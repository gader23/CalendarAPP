using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Models;
using EventKit;
using Foundation;

namespace CalendarOcrApp.Platforms.iOS
{
    internal class IosCalendarService : ICalendarService
    {
        private readonly EKEventStore _store = new();

        private async Task EnsureAccessAsync()
        {
            bool granted;

            if(OperatingSystem.IsIOSVersionAtLeast(17))
            {
                var result = await _store.RequestFullAccessToEventsAsync();
                granted = result.Item1;
            }
            else
            {
                var result = await _store.RequestAccessAsync(EKEntityType.Event);
                granted = result.Item1;
            }

            if(!granted)
            {
                throw new Exception("Sin permiso de calendario. Actívalo en Ajustes > CalendarioOcrApp > Calendarios.");
            }
        }

        public async Task<List<DeviceCalendarModel>> GetCalendarsAsync()
        {
            await EnsureAccessAsync();

            return _store.GetCalendars(EKEntityType.Event)
                .Where(c => c.AllowsContentModifications)
                .Select(c => new DeviceCalendarModel
                {
                    Id = c.CalendarIdentifier,
                    Name = c.Title ?? "",
                    AccountName = c.Source?.Title ?? "",
                    AccountType = c.Source?.SourceType.ToString() ?? ""
                })
                .ToList();
        }

        public async Task AddEventAsync(IEnumerable<CalendarEventModel> events, string calendarId)
        {
            await EnsureAccessAsync();

            var calendar = _store.GetCalendar(calendarId)
                ?? throw new InvalidOperationException("No se encontró el calendario seleccionado.");

            var list = events.ToList();

            int failed = 0;

            foreach (var calendarEvent in list)
            {
                var ev = EKEvent.FromStore(_store);
                ev.Calendar = calendar;
                ev.Title = calendarEvent.Title;
                ev.Notes = calendarEvent.Notes;

                if (calendarEvent.AllDay)
                {
                    ev.AllDay = true;
                    ev.StartDate = ToNSDate(calendarEvent.Start);
                    ev.EndDate = ToNSDate(calendarEvent.End);
                }
                else
                {
                    ev.StartDate = ToNSDate(calendarEvent.Start);
                    ev.EndDate = ToNSDate(calendarEvent.End);
                }

                if (!_store.SaveEvent(ev,EKSpan.ThisEvent,true,out NSError? error))
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo guardar '{calendarEvent.Title}':{error}");
                    failed++;
                }
            }

            if (failed > 0)
            {
                throw new InvalidOperationException(
                    $"No se pudieron guardar {failed} de {list.Count} eventos.");
            }
        }

        private static NSDate ToNSDate(DateTime date) =>
            (NSDate)DateTime.SpecifyKind(date, DateTimeKind.Local);

        public Task OpenEventEditorAsync(CalendarEventModel calendarEvent)
        {
            throw new NotSupportedException(
                "No se encontró ningún calendario donde guardar los eventos.");
        }
    }
}
