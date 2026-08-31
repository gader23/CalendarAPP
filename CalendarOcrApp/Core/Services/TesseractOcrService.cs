using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarOcrApp.Core.Interfaces;
using TesseractOcrMaui;
using TesseractOcrMaui.Results;

namespace CalendarOcrApp.Core.Services
{
    public class TesseractOcrService : IOcrService
    {
        private readonly ITesseract _tesseract;

        public TesseractOcrService(ITesseract tesseract)
        {
            _tesseract = tesseract;
        }

        public async Task<string> RecognizeTextAsync(Stream image)
        {
            ArgumentNullException.ThrowIfNull(image);

            byte[] imageBytes = await ReadStreamAsync(image);

            if(imageBytes.Length == 0)
            {
                throw new InvalidOperationException("La imagen seleccionada esta vacia.");
            }

            var result = await _tesseract.RecognizeTextAsync(imageBytes);

            if(result.NotSuccess())
            {
                throw new InvalidOperationException(
                    $"Tesseract no pudo reconocer la imagen." +
                    $"Estado: {result.Status}");
            }
            return result.RecognisedText?.Trim() ?? string.Empty;
        }
        private static async Task<byte[]> ReadStreamAsync(Stream image)
        {
            if(image.CanSeek)
            {
                image.Position = 0;
            }
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
