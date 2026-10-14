using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;

// ---------------------------------------------------------------------------
// Configuración (variables de entorno en el servidor, nunca en el repositorio):
//   Anthropic__ApiKey  -> clave de la API
//   App__Token         -> token compartido con la app (cabecera X-App-Token)
//   Anthropic__Model   -> opcional; por defecto claude-haiku-5-5 (ver appsettings.json)
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

var apiKey = builder.Configuration["Anthropic:ApiKey"];
var model = builder.Configuration["Anthropic:Model"] ?? "claude-haiku-5-5";
var appToken = builder.Configuration["App:Token"];

if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(appToken))
{
    throw new InvalidOperationException(
        "Faltan las variables de entorno Anthropic__ApiKey y/o App__Token.");
}

const int MaxImageBytes = 5 * 1024 * 1024; // límite de la API por imagen

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = MaxImageBytes + 1024);

builder.Services.AddHttpClient("anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Máximo 10 peticiones por minuto y por IP. Si el servidor está detrás de un proxy,
// configura UseForwardedHeaders para que RemoteIpAddress sea la del cliente real.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("extract", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok("ok"));

app.MapPost("/extract", async (
    HttpContext ctx,
    IHttpClientFactory httpFactory,
    ILogger<Program> log,
    CancellationToken ct) =>
{
    // 1) Autenticación básica de la app
    var provided = ctx.Request.Headers["X-App-Token"].ToString();
    if (!Helpers.FixedTimeEquals(provided, appToken))
        return Results.Unauthorized();

    // 2) Tipo de imagen
    var mediaType = (ctx.Request.ContentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
    if (!Helpers.AllowedMediaTypes.Contains(mediaType))
        return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

    // 3) Leer la imagen (Kestrel ya corta peticiones demasiado grandes)
    using var ms = new MemoryStream();
    await ctx.Request.Body.CopyToAsync(ms, ct);

    if (ms.Length == 0)
        return Results.BadRequest("Imagen vacía.");
    if (ms.Length > MaxImageBytes)
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

    // 4) Llamada al modelo de visión
    var payload = new
    {
        model,
        max_tokens = 8000,
        system = Prompts.System,
        messages = new[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "image",
                        source = new
                        {
                            type = "base64",
                            media_type = mediaType,
                            data = Convert.ToBase64String(ms.ToArray())
                        }
                    },
                    new { type = "text", text = Prompts.User }
                }
            }
        }
    };

    using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
    {
        Content = JsonContent.Create(payload)
    };
    request.Headers.Add("x-api-key", apiKey);
    request.Headers.Add("anthropic-version", "2023-06-01");

    string responseBody;
    try
    {
        using var upstream = await httpFactory.CreateClient("anthropic").SendAsync(request, ct);
        responseBody = await upstream.Content.ReadAsStringAsync(ct);

        if (!upstream.IsSuccessStatusCode)
        {
            log.LogError("La API devolvió {Status}: {Body}", (int)upstream.StatusCode, responseBody);
            return Results.Problem("El servicio de lectura no está disponible.", statusCode: 502);
        }
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        log.LogError(ex, "Fallo al llamar a la API");
        return Results.Problem("El servicio de lectura no está disponible.", statusCode: 502);
    }

    // 5) Interpretar y sanear la respuesta
    var text = Helpers.GetResponseText(responseBody);
    var parsed = Helpers.ParseModelJson(text);

    if (parsed is null)
    {
        log.LogWarning("Respuesta del modelo no interpretable: {Text}", text);
        return Results.Problem("No se pudo interpretar la lectura del calendario.", statusCode: 502);
    }

    return Results.Ok(Helpers.Sanitize(parsed));
})
.RequireRateLimiting("extract");

app.Run();

// ---------------------------------------------------------------------------
// Tipos y utilidades
// ---------------------------------------------------------------------------

record ExtractedEvent(int Day, string Title, string? StartTime, string? EndTime, bool AllDay);

record ExtractionResponse(int? Month, int? Year, List<ExtractedEvent>? Events);

static class Prompts
{
    public const string System =
        "Eres un extractor de eventos a partir de fotos de calendarios físicos de papel. " +
        "Respondes SIEMPRE y ÚNICAMENTE con un objeto JSON válido, sin texto adicional " +
        "ni bloques de código.";

    public const string User = """
        Analiza la foto de este calendario físico y extrae los eventos que el usuario ha anotado.

        Devuelve exactamente este JSON:
        {
          "month": número de 1 a 12, o null,
          "year": número de cuatro cifras, o null,
          "events": [
            {
              "day": número de 1 a 31,
              "title": "texto",
              "startTime": "HH:mm" o null,
              "endTime": "HH:mm" o null,
              "allDay": true o false
            }
          ]
        }

        Reglas:
        1. Cada casilla corresponde a un día. El número del día puede estar en cualquier esquina
           o en la parte inferior de la casilla. El texto escrito a mano puede estar inclinado o
           salirse del borde de la casilla: asígnalo a la casilla a la que pertenece visualmente.
        2. Los números que forman parte de un texto (edades, cantidades, horas) NO son el día.
           Ejemplo: "Cena con 8 amigos" escrito en la casilla del 14 es un evento del día 14.
        3. Extrae solo las anotaciones añadidas por el usuario (normalmente a mano). Ignora el
           texto impreso del calendario: santoral, festivos, fases lunares, etc.
        4. Transcribe cada título tal como está escrito, sin traducir ni corregir.
        5. Si hay una hora escrita, ponla en startTime en formato 24 h (y en endTime si hay hora
           de fin) y allDay=false. Si no hay hora, startTime y endTime son null y allDay=true.
        6. Si ves con claridad el mes y el año (normalmente en el encabezado), devuélvelos.
           Si no se ven con claridad, devuelve null. No los deduzcas.
        7. Si una casilla contiene varias anotaciones claramente separadas, devuelve un evento
           por cada una.
        8. No inventes eventos. Si algo es ilegible, omítelo.
        """;
}

static partial class Helpers
{
    public static readonly HashSet<string> AllowedMediaTypes =
        new() { "image/jpeg", "image/png", "image/webp", "image/gif" };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimeRegex();

    public static bool FixedTimeEquals(string a, string b)
    {
        var x = Encoding.UTF8.GetBytes(a);
        var y = Encoding.UTF8.GetBytes(b);
        return x.Length == y.Length && CryptographicOperations.FixedTimeEquals(x, y);
    }

    /// <summary>Extrae el primer bloque de texto de la respuesta de la API.</summary>
    public static string? GetResponseText(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);

            if (doc.RootElement.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var type) &&
                        type.GetString() == "text" &&
                        block.TryGetProperty("text", out var text))
                    {
                        return text.GetString();
                    }
                }
            }
        }
        catch (JsonException)
        {
            // se trata abajo como "sin texto"
        }

        return null;
    }

    /// <summary>Tolera que el modelo envuelva el JSON en ```json ... ```.</summary>
    public static ExtractionResponse? ParseModelJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        try
        {
            return JsonSerializer.Deserialize<ExtractionResponse>(text[start..(end + 1)], JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Nunca se fía de lo que devuelve el modelo: valida rangos y formatos.</summary>
    public static ExtractionResponse Sanitize(ExtractionResponse raw)
    {
        int? month = raw.Month is >= 1 and <= 12 ? raw.Month : null;
        int? year = raw.Year is >= 2000 and <= 2100 ? raw.Year : null;

        var events = new List<ExtractedEvent>();

        foreach (var e in raw.Events ?? [])
        {
            var title = e.Title?.Trim();

            if (e.Day is < 1 or > 31 || string.IsNullOrEmpty(title))
                continue;

            if (title.Length > 200)
                title = title[..200];

            var start = e.AllDay ? null : ValidTime(e.StartTime);
            var end = start is null ? null : ValidTime(e.EndTime);

            events.Add(new ExtractedEvent(e.Day, title, start, end, AllDay: start is null));
        }

        return new ExtractionResponse(month, year, events);
    }

    private static string? ValidTime(string? value) =>
        value is not null && TimeRegex().IsMatch(value) ? value : null;
}
