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
        public Task AddEventAsync(IEnumerable<CalendarEventModel> events)
        {
            return Task.CompletedTask;
        }
    }
}
