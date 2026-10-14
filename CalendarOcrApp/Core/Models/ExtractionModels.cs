using System.Text.Json.Serialization;

namespace CalendarOcrApp.Core.Models
{
    /// <summary>
    /// Evento tal como lo devuelve el backend. Es editable en la pantalla de revisión,
    /// por eso son clases con setters y no records.
    /// </summary>
    public class ExtractedEvent
    {
        public int Day { get; set; }
        public string Title { get; set; } = string.Empty;

        /// <summary>"HH:mm" en 24 h, o null si el evento no tiene hora.</summary>
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public bool AllDay { get; set; }

        [JsonIgnore]
        public string TimeDescription =>
            AllDay || string.IsNullOrEmpty(StartTime)
                ? "Todo el día"
                : string.IsNullOrEmpty(EndTime) ? StartTime : $"{StartTime} - {EndTime}";
    }

    public class ExtractionResult
    {
        /// <summary>1-12, o null si la foto no muestra el mes con claridad.</summary>
        public int? Month { get; set; }

        /// <summary>Año, o null si la foto no lo muestra con claridad.</summary>
        public int? Year { get; set; }

        public List<ExtractedEvent> Events { get; set; } = new();
    }
}
