using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarOcrApp.Core.Models
{
    public class DeviceCalendarModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string AccountType { get; set; } = "";
    }
}
