using Microsoft.Extensions.Logging;
using CalendarOcrApp.Core;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Services;

namespace CalendarOcrApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Un único HttpClient para toda la app (lo recomendado para HttpClient).
            builder.Services.AddSingleton<IEventExtractor>(_ =>
            {
                var http = new HttpClient
                {
                    BaseAddress = new Uri(AppConfig.ApiBaseUrl),
                    Timeout = TimeSpan.FromSeconds(90)
                };
                http.DefaultRequestHeaders.Add("X-App-Token", AppConfig.AppToken);
                return new CloudEventExtractor(http);
            });

#if ANDROID
            builder.Services.AddSingleton<ICalendarService, CalendarOcrApp.Platforms.Android.AndroidCalendarService>();
#elif IOS
            builder.Services.AddSingleton<ICalendarService, CalendarOcrApp.Platforms.iOS.IosCalendarService>();
#else
            // TODO iOS: sustituir por una implementación con EventKit.
            builder.Services.AddSingleton<ICalendarService, DummyCalendarService>();
#endif

            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }
}
