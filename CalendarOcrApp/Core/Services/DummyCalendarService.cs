using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Models;

namespace CalendarOcrApp.Core.Services
{
    internal class DummyCalendarService : ICalendarService
    {
        public Task<List<DeviceCalendarModel>> GetCalendarsAsync()
        {
            return Task.FromResult(new List<DeviceCalendarModel>());
        }
        public Task AddEventAsync(IEnumerable<CalendarEventModel> events, long calendarId)
        {
            return Task.CompletedTask;
        }
        public Task OpenEventEditorAsync(CalendarEventModel calendarEvent)
        {
            return Task.CompletedTask;
        }
    }
}
