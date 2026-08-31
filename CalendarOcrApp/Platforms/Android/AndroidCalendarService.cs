using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Icu.Number;
using Android.Provider;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Models;
 

namespace CalendarOcrApp.Platforms.Android
{
    

    

    public class  AndroidCalendarService : ICalendarService
    {
        public async Task<List<DeviceCalendarModel>> GetCalendarsAsync()
        {
            var result = new List<DeviceCalendarModel>();

            var readStatus = await Permissions.RequestAsync<Permissions.CalendarRead>();

            if(readStatus != PermissionStatus.Granted)
            {
                throw new Exception(
                    "Se necesita permiso de lectura para mostrar los calendarios.");
            }

            var context = global::Android.App.Application.Context;

            var projection = new[]
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.AccountName,
                CalendarContract.Calendars.InterfaceConsts.AccountType
            };

            using var cursor = context.ContentResolver.Query(
                CalendarContract.Calendars.ContentUri,
                projection,
                null,
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
                    Id = cursor.GetLong(0),
                    Name = cursor.GetString(1) ?? "",
                    AccountName = cursor.GetString(2) ?? "",
                    AccountType = cursor.GetString(3) ?? ""
                });
            }
            return result;
        }
        public async Task AddEventAsync(IEnumerable<CalendarEventModel> events, long calendarId)
        {
            var status = await Permissions.RequestAsync<Permissions.CalendarWrite>();
            if(status != PermissionStatus.Granted)
            {
                throw new Exception("Permiso de calendario denegado.");
            }

            var context = global::Android.App.Application.Context;

            foreach(var calendarEvent in events)
            {
                var values = new ContentValues();

                values.Put(CalendarContract.Events.InterfaceConsts.CalendarId, calendarId);
                values.Put(CalendarContract.Events.InterfaceConsts.Title, calendarEvent.Title);
                values.Put(CalendarContract.Events.InterfaceConsts.Description, calendarEvent.Notes ?? "");
                values.Put(CalendarContract.Events.InterfaceConsts.Dtstart, ToUnixMillis(calendarEvent.Start));
                values.Put(CalendarContract.Events.InterfaceConsts.Dtend, ToUnixMillis(calendarEvent.End));
                values.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, TimeZoneInfo.Local.Id);

                context.ContentResolver.Insert(CalendarContract.Events.ContentUri, values);
            }
        }

        public async Task OpenEventEditorAsync(CalendarEventModel calendarEvent)
        {
            var activity = await Platform.WaitForActivityAsync();

            var intent = new Intent(Intent.ActionInsert);

            intent.SetData(CalendarContract.Events.ContentUri);

            intent.PutExtra(CalendarContract.ExtraEventBeginTime,
                ToUnixMillis(calendarEvent.Start));

            intent.PutExtra(CalendarContract.ExtraEventEndTime,
                ToUnixMillis(calendarEvent.End));

            intent.PutExtra(CalendarContract.Events.InterfaceConsts.Title,
                calendarEvent.Title);

            intent.PutExtra(CalendarContract.Events.InterfaceConsts.Description,
                calendarEvent.Notes ?? string.Empty);

            var chooser = Intent.CreateChooser(intent, "Selecciona una aplicacion de calendario");

            activity.StartActivity(chooser);

        }

        private static long ToUnixMillis(DateTime date)
        {
            return new DateTimeOffset(date).ToUnixTimeMilliseconds();
        }
    }
}
