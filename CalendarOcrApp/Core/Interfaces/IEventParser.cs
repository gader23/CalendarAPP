using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarOcrApp.Core.Models;

namespace CalendarOcrApp.Core.Interfaces
{
    public interface IEventParser
    {
        List<CalendarEventModel> Parse(string ocrText, int year, int month);
    }
}
