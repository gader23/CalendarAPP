using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarOcrApp.Core.Models;

public class CalendarEventModel
{
    public string Title { get; set; }
    public DateTime Start { get; set;  }
    public DateTime End { get; set; }
    public string Notes { get; set; }
}