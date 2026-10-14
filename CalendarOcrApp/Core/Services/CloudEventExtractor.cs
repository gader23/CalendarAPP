using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Models;
#if ANDROID || IOS || MACCATALYST
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
#endif

namespace CalendarOcrApp.Core.Services
{
    public class CloudEventExtractor : IEventExtractor
    {
        // Lado mayor de la imagen que se envía. Más píxeles no mejoran la lectura
        // y solo aumentan el coste y la latencia.
        private const int MaxImageEdge = 1800;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;

        public CloudEventExtractor(HttpClient http)
        {
            _http = http;
        }

        public async Task<ExtractionResult> ExtractAsync(Stream image, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(image);

            var (bytes, mediaType) = await PrepareImageAsync(image);

            if (bytes.Length == 0)
                throw new InvalidOperationException("La imagen seleccionada está vacía.");

            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync("extract", content, cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException(
                    "No se pudo conectar con el servidor. Comprueba tu conexión a internet.");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("El servidor ha tardado demasiado en responder.");
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => "La app no está autorizada en el servidor.",
                        HttpStatusCode.RequestEntityTooLarge => "La imagen es demasiado grande.",
                        HttpStatusCode.UnsupportedMediaType => "Formato de imagen no soportado.",
                        HttpStatusCode.TooManyRequests => "Demasiadas peticiones seguidas. Espera un minuto.",
                        _ => $"El servidor no pudo leer el calendario (código {(int)response.StatusCode})."
                    });
                }

                var result = await response.Content
                    .ReadFromJsonAsync<ExtractionResult>(JsonOptions, cancellationToken);

                return result ?? new ExtractionResult();
            }
        }

        /// <summary>
        /// Reduce y recomprime la foto a JPEG. Si no se puede (p. ej. en Windows),
        /// se envía tal cual.
        /// </summary>
        private static async Task<(byte[] Data, string MediaType)> PrepareImageAsync(Stream image)
        {
            if (image.CanSeek)
                image.Position = 0;

            using var original = new MemoryStream();
            await image.CopyToAsync(original);
            var originalBytes = original.ToArray();

#if ANDROID || IOS || MACCATALYST
            try
            {
                using var source = new MemoryStream(originalBytes);

                var platformImage = PlatformImage.FromStream(source);
                var resized = platformImage.Downsize(MaxImageEdge);

                using var output = new MemoryStream();
                await resized.SaveAsync(output, ImageFormat.Jpeg, 0.8f);

                if (!ReferenceEquals(resized, platformImage))
                    resized.Dispose();
                platformImage.Dispose();

                // OJO: en Android, PlatformImage puede ignorar la orientación EXIF y
                // devolver la foto girada. El modelo lee texto girado razonablemente,
                // pero conviene probarlo con fotos hechas en vertical.
                return (output.ToArray(), "image/jpeg");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo reducir la imagen: {ex}");
            }
#endif
            return (originalBytes, DetectMediaType(originalBytes));
        }

        private static string DetectMediaType(byte[] data)
        {
            if (data.Length > 3 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";

            if (data.Length > 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
                return "image/webp";

            return "image/jpeg";
        }
    }
}
