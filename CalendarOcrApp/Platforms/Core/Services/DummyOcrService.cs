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
            return Task.FromResult("Texto OCR de prueba");
        }
    }
}
