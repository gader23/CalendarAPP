using CalendarOcrApp.Core.Models;

namespace CalendarOcrApp.Core.Interfaces
{
    /// <summary>
    /// Recibe la foto de un calendario físico y devuelve los eventos detectados.
    /// Sustituye a IOcrService + IEventParser.
    /// </summary>
    public interface IEventExtractor
    {
        Task<ExtractionResult> ExtractAsync(Stream image, CancellationToken cancellationToken = default);
    }
}
