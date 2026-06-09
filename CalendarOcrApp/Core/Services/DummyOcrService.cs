using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarOcrApp.Core.Interfaces;

namespace CalendarOcrApp.Core.Services
{
    internal class DummyOcrService : IOcrService
    {
        public Task<string> RecognizeTextAsync(Stream image)
        {
            var fakeText = """
            1 Reunión 10:00
            2 Dentista 17:30
            5 Cumpleaños Lucas 20:00
            8 Gym 19:00
            12 Cena 21:30
            """;
            return Task.FromResult(fakeText);
        }
    }
}
