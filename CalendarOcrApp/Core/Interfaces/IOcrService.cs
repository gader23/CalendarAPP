using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarOcrApp.Core.Interfaces
{

    public interface IOcrService
    {
        Task<string> RecognizeTextAsync(Stream image);
    }
}