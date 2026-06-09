using Microsoft.Extensions.Logging;
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
            builder.Services.AddSingleton<IOcrService, DummyOcrService>();
            builder.Services.AddSingleton<ICalendarService, DummyCalendarService>();
            builder.Services.AddSingleton<IEventParser, SimpleEventParser>();
            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }
}
