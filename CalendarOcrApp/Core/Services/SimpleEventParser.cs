using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Models;
using System.Text.RegularExpressions;

namespace CalendarOcrApp.Core.Services
{
    public class SimpleEventParser : IEventParser
    {
        public List<CalendarEventModel> Parse(string ocrText, int year, int month)
        {
            var events = new List<CalendarEventModel>();
            
            var lines = ocrText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^(\d{1,2})\s+(.+)\s+(\d{1,2}:\d{2})$");

                if (!match.Success)
                    continue;

                var day = int.Parse(match.Groups[1].Value);
                var title = match.Groups[2].Value;
                var time = TimeSpan.Parse(match.Groups[3].Value);

                var start = new DateTime(year, month, day)
                    .Add(time);

                events.Add(new CalendarEventModel
                {
                    Title = title,
                    Start = start,
                    End = start.AddHours(1)
                });
            }
            return events;
        }
    }
}
