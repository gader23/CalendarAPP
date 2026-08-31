using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarOcrApp.Core.Models;


namespace CalendarOcrApp.Core.Interfaces
{

    public interface ICalendarService
    {
        Task<List<DeviceCalendarModel>> GetCalendarsAsync();
        Task AddEventAsync(IEnumerable<CalendarEventModel> events,long calendarId);

        Task OpenEventEditorAsync(CalendarEventModel calendarEvent);
    }
}