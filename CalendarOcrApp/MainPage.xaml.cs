using System.Threading.Tasks;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Models;

namespace CalendarOcrApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IOcrService _ocrService;
        private readonly IEventParser _eventParser;
        private readonly ICalendarService _calendarService;
        private List<CalendarEventModel> _detectedEvents = new();
      
        public MainPage(IOcrService ocrService,IEventParser eventParser, ICalendarService calendarService)
        {
            InitializeComponent();

            _ocrService = ocrService;
            _eventParser = eventParser;
            _calendarService = calendarService;
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync();

                if (result == null)
                    return;
                // Mostrar imagen seleccionada
                SelectedImage.Source = ImageSource.FromFile(result.FullPath);
                SelectedImage.IsVisible = true;

                // Abrir Stream y pasar al OCR
                using var stream = await result.OpenReadAsync();
                var text = await _ocrService.RecognizeTextAsync(stream);

                OcrResultEditor.Text = text;

                _detectedEvents = _eventParser.Parse(text, 2026, 5);
                EventsCollection.ItemsSource = _detectedEvents;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnSaveEventsClicked(object sender, EventArgs e)
        {
            if(_detectedEvents.Count == 0)
            {
                await DisplayAlert("Sin Eventos", "No hay eventos detectados para guardar.", "OK");
                return;
            }
            await _calendarService.AddEventAsync(_detectedEvents);
            await DisplayAlert("Guardar", $"Se guardarian {_detectedEvents.Count} eventos.", "OK");
        }
    }
}
