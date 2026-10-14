namespace CalendarOcrApp.Core.Models;

public class CalendarEventModel
{
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Si AllDay es true, solo importa la fecha: Start es el día (00:00) y
    /// End es el día siguiente (00:00, exclusivo).
    /// </summary>
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool AllDay { get; set; }
    public string Notes { get; set; } = string.Empty;
}
