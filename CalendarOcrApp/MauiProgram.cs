using Microsoft.Extensions.Logging;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Services;
using TesseractOcrMaui;

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
            builder.Services.AddTesseractOcr(files =>
            {
                files.AddFile("spa.traineddata");
            });
#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<IOcrService, TesseractOcrService>();
#if ANDROID
            builder.Services.AddSingleton<ICalendarService, CalendarOcrApp.Platforms.Android.AndroidCalendarService>();
#else
            builder.Services.AddSingleton<ICalendarService, DummyCalendarService>();
#endif
            builder.Services.AddSingleton<IEventParser, SimpleEventParser>();
            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }
}
