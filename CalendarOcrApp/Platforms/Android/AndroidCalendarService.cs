using Android.Content;
using Android.Provider;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Models;

namespace CalendarOcrApp.Platforms.Android
{
    public class AndroidCalendarService : ICalendarService
    {
        // CalendarContract.Calendars.CAL_ACCESS_CONTRIBUTOR = 500: a partir de ahí se puede escribir.
        // Así no se ofrecen calendarios de solo lectura (festivos, cumpleaños de contactos...).
        private const int MinWritableAccessLevel = 500;

        public async Task<List<DeviceCalendarModel>> GetCalendarsAsync()
        {
            var result = new List<DeviceCalendarModel>();

            var readStatus = await Permissions.RequestAsync<Permissions.CalendarRead>();

            if (readStatus != PermissionStatus.Granted)
            {
                throw new Exception("Se necesita permiso de lectura para mostrar los calendarios.");
            }

            var context = global::Android.App.Application.Context;

            var projection = new[]
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.AccountName,
                CalendarContract.Calendars.InterfaceConsts.AccountType
            };

            string selection =
                $"{CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel} >= {MinWritableAccessLevel}";

            using var cursor = context.ContentResolver!.Query(
                CalendarContract.Calendars.ContentUri!,
                projection,
                selection,
                null,
                null);

            if (cursor == null)
            {
                return result;
            }

            while (cursor.MoveToNext())
            {
                result.Add(new DeviceCalendarModel
                {
                    Id = cursor.GetLong(0).ToString(),
                    Name = cursor.GetString(1) ?? "",
                    AccountName = cursor.GetString(2) ?? "",
                    AccountType = cursor.GetString(3) ?? ""
                });
            }

            return result;
        }

        public async Task AddEventAsync(IEnumerable<CalendarEventModel> events, string calendarId)
        {
            var status = await Permissions.RequestAsync<Permissions.CalendarWrite>();
            if (status != PermissionStatus.Granted)
            {
                throw new Exception("Permiso de calendario denegado.");
            }

            if(!long.TryParse(calendarId, out long androidCalendarId))
            {
                throw new ArgumentException("Identificador de calendario no válido.", nameof(calendarId));
            }

            var context = global::Android.App.Application.Context;
            var list = events.ToList();

            int failed = await Task.Run(() =>
            {
                int failures = 0;

                foreach (var calendarEvent in list)
                {
                    var values = BuildValues(calendarEvent, androidCalendarId);
                    var uri = context.ContentResolver!.Insert(CalendarContract.Events.ContentUri!, values);

                    if (uri == null)
                        failures++;
                }

                return failures;
            });

            if (failed > 0)
            {
                throw new InvalidOperationException(
                    $"No se pudieron guardar {failed} de {list.Count} eventos.");
            }
        }

        public async Task OpenEventEditorAsync(CalendarEventModel calendarEvent)
        {
            var activity = await Platform.WaitForActivityAsync();

            var intent = new Intent(Intent.ActionInsert);
            intent.SetData(CalendarContract.Events.ContentUri);

            if (calendarEvent.AllDay)
            {
                intent.PutExtra(CalendarContract.ExtraEventAllDay, true);
                intent.PutExtra(CalendarContract.ExtraEventBeginTime, ToUtcMidnightMillis(calendarEvent.Start));
                intent.PutExtra(CalendarContract.ExtraEventEndTime, ToUtcMidnightMillis(calendarEvent.End));
            }
            else
            {
                intent.PutExtra(CalendarContract.ExtraEventBeginTime, ToLocalMillis(calendarEvent.Start));
                intent.PutExtra(CalendarContract.ExtraEventEndTime, ToLocalMillis(calendarEvent.End));
            }

            intent.PutExtra(CalendarContract.Events.InterfaceConsts.Title, calendarEvent.Title);
            intent.PutExtra(CalendarContract.Events.InterfaceConsts.Description, calendarEvent.Notes ?? string.Empty);

            var chooser = Intent.CreateChooser(intent, "Selecciona una aplicación de calendario");
            activity.StartActivity(chooser);
        }

        private static ContentValues BuildValues(CalendarEventModel calendarEvent, long calendarId)
        {
            var values = new ContentValues();

            values.Put(CalendarContract.Events.InterfaceConsts.CalendarId, calendarId);
            values.Put(CalendarContract.Events.InterfaceConsts.Title, calendarEvent.Title);
            values.Put(CalendarContract.Events.InterfaceConsts.Description, calendarEvent.Notes ?? "");

            if (calendarEvent.AllDay)
            {
                // Android exige para eventos de día completo: medianoche UTC, zona "UTC"
                // y DTEND = día siguiente (exclusivo).
                values.Put(CalendarContract.Events.InterfaceConsts.AllDay, 1);
                values.Put(CalendarContract.Events.InterfaceConsts.Dtstart, ToUtcMidnightMillis(calendarEvent.Start));
                values.Put(CalendarContract.Events.InterfaceConsts.Dtend, ToUtcMidnightMillis(calendarEvent.End));
                values.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, "UTC");
            }
            else
            {
                values.Put(CalendarContract.Events.InterfaceConsts.Dtstart, ToLocalMillis(calendarEvent.Start));
                values.Put(CalendarContract.Events.InterfaceConsts.Dtend, ToLocalMillis(calendarEvent.End));
                values.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, TimeZoneInfo.Local.Id);
            }

            return values;
        }

        private static long ToLocalMillis(DateTime date)
        {
            var local = DateTime.SpecifyKind(date, DateTimeKind.Local);
            return new DateTimeOffset(local).ToUnixTimeMilliseconds();
        }

        private static long ToUtcMidnightMillis(DateTime date)
        {
            return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero)
                .ToUnixTimeMilliseconds();
        }
    }
}
