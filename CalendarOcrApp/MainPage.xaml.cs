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
                var photo = await MediaPicker.PickPhotoAsync(
                    new MediaPickerOptions
                    {
                        Title = "Selecciona una imagen del calendario"
                    });

                if (photo == null)
                    return;

                await using var imageStream = await photo.OpenReadAsync();

                string detectedText = await _ocrService.RecognizeTextAsync(imageStream);
                
                if(string.IsNullOrWhiteSpace(detectedText))
                {
                    await DisplayAlert("Sin Texto",
                        "No se ha detectado texto en la imagen.",
                        "Aceptar");
                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    "===== TEXTO OCR =====");
                System.Diagnostics.Debug.WriteLine(detectedText);
                System.Diagnostics.Debug.WriteLine("====================");

                OcrResultEditor.Text = detectedText;

                int year = DateTime.Now.Year;
                int month = DateTime.Now.Month;

                var events = _eventParser.Parse(
                    detectedText,
                    year,
                    month);

                _detectedEvents.Clear();

                foreach (var calendarEvent in events)
                {
                    _detectedEvents.Add(calendarEvent);
                }

                //if (photo == null)
                //    return;
                //// Mostrar imagen seleccionada
                //SelectedImage.Source = ImageSource.FromFile(photo.FullPath);
                //SelectedImage.IsVisible = true;

                //// Abrir Stream y pasar al OCR
                //using var stream = await photo.OpenReadAsync();
                //var text = await _ocrService.RecognizeTextAsync(stream);

                //OcrResultEditor.Text = text;

                //_detectedEvents = _eventParser.Parse(text, 2026, 5);
                //EventsCollection.ItemsSource = _detectedEvents;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");

                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private async void OnSaveEventsClicked(object sender, EventArgs e)
        {
            if(_detectedEvents.Count == 0)
            {
                await DisplayAlert("Sin Eventos", "No hay eventos detectados para guardar.", "OK");
                return;
            }
            var calendars = await _calendarService.GetCalendarsAsync();

            if(calendars.Count == 0)
            {
                try
                {
                    await _calendarService.OpenEventEditorAsync(_detectedEvents[0]);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "Aceptar");
                }
                return;
            }
            
            var options = calendars
                .Select(c => $"{c.Name} - {c.AccountName} ({c.AccountType})")
                .ToArray();

            var selected = await DisplayActionSheet(
                "Elige calendario",
                "Cancelar",
                null,
                options);

            if (selected == "Cancelar" || selected == null)
                return;

            var index = Array.IndexOf(options, selected);
            var calendar = calendars[index];

            await _calendarService.AddEventAsync(_detectedEvents, calendar.Id);
            await DisplayAlert("Guardar", $"Se guardarian {_detectedEvents.Count} eventos.", "OK");
            await DisplayAlert("Calendario", $"Eventos guardados en: {calendar.Name}", "OK");
        }
    }
}
