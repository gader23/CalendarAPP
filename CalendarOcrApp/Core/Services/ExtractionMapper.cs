using System.Globalization;
using CalendarOcrApp.Core.Models;

namespace CalendarOcrApp.Core.Services
{
    /// <summary>
    /// Convierte los eventos extraídos (día + texto) en eventos de calendario reales
    /// una vez que el usuario ha confirmado mes y año. Descarta lo que no sea válido
    /// en vez de fallar con todo.
    /// </summary>
    public static class ExtractionMapper
    {
        public static List<CalendarEventModel> ToCalendarEvents(
            IEnumerable<ExtractedEvent> events,
            int year,
            int month,
            out int skipped)
        {
            var result = new List<CalendarEventModel>();
            skipped = 0;

            if (year < 1 || year > 9999 || month < 1 || month > 12)
            {
                skipped = events.Count();
                return result;
            }

            int daysInMonth = DateTime.DaysInMonth(year, month);

            foreach (var e in events)
            {
                var title = e.Title?.Trim();

                if (e.Day < 1 || e.Day > daysInMonth || string.IsNullOrEmpty(title))
                {
                    skipped++;
                    continue;
                }

                var date = new DateTime(year, month, e.Day, 0, 0, 0, DateTimeKind.Unspecified);

                if (e.AllDay || !TryParseTime(e.StartTime, out var start))
                {
                    result.Add(new CalendarEventModel
                    {
                        Title = title,
                        Start = date,
                        End = date.AddDays(1),
                        AllDay = true
                    });
                    continue;
                }

                var startDateTime = date + start;
                var endDateTime = TryParseTime(e.EndTime, out var end) && end > start
                    ? date + end
                    : startDateTime.AddHours(1);

                result.Add(new CalendarEventModel
                {
                    Title = title,
                    Start = startDateTime,
                    End = endDateTime,
                    AllDay = false
                });
            }

            return result;
        }

        private static bool TryParseTime(string? value, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out time);
        }
    }
}
