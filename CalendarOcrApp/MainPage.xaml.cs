using System.Collections.ObjectModel;
using System.Globalization;
using CalendarOcrApp.Core.Interfaces;
using CalendarOcrApp.Core.Models;
using CalendarOcrApp.Core.Services;

namespace CalendarOcrApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IEventExtractor _eventExtractor;
        private readonly ICalendarService _calendarService;
        private readonly ObservableCollection<ExtractedEvent> _events = new();

        public MainPage(IEventExtractor eventExtractor, ICalendarService calendarService)
        {
            InitializeComponent();

            _eventExtractor = eventExtractor;
            _calendarService = calendarService;

            BindableLayout.SetItemsSource(EventsLayout, _events);

            var culture = new CultureInfo("es-ES");
            MonthPicker.ItemsSource = culture.DateTimeFormat.MonthNames
                .Take(12)
                .Select(name => char.ToUpper(name[0], culture) + name[1..])
                .ToList();

            MonthPicker.SelectedIndex = DateTime.Now.Month - 1;
            YearEntry.Text = DateTime.Now.Year.ToString();
        }

        private async void OnPickImageClicked(object? sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Selecciona una foto del calendario"
                });

                if (photo == null)
                    return;

                SelectedImage.Source = ImageSource.FromFile(photo.FullPath);
                SelectedImage.IsVisible = true;

                SetBusy(true, "Leyendo el calendario...");

                await using var imageStream = await photo.OpenReadAsync();
                var result = await _eventExtractor.ExtractAsync(imageStream);

                _events.Clear();
                foreach (var extracted in result.Events)
                    _events.Add(extracted);

                bool periodDetected = result.Month is >= 1 and <= 12 && result.Year is > 0;

                // Si no se detectó, se dejan los valores actuales y se avisa al usuario.
                if (result.Month is >= 1 and <= 12)
                    MonthPicker.SelectedIndex = result.Month.Value - 1;

                if (result.Year is > 0)
                    YearEntry.Text = result.Year.Value.ToString();

                PeriodHintLabel.IsVisible = !periodDetected;

                if (_events.Count == 0)
                {
                    await DisplayAlert("Sin eventos",
                        "No se ha detectado ningún evento en la foto.",
                        "Aceptar");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void OnDeleteEventClicked(object? sender, EventArgs e)
        {
            if (sender is Button { BindingContext: ExtractedEvent extracted })
                _events.Remove(extracted);
        }

        private async void OnSaveEventsClicked(object? sender, EventArgs e)
        {
            try
            {
                if (_events.Count == 0)
                {
                    await DisplayAlert("Sin eventos", "No hay eventos para guardar.", "Aceptar");
                    return;
                }

                if (MonthPicker.SelectedIndex < 0 ||
                    !int.TryParse(YearEntry.Text, out int year) || year < 2000 || year > 2100)
                {
                    await DisplayAlert("Mes y año", "Indica un mes y un año válidos.", "Aceptar");
                    return;
                }

                int month = MonthPicker.SelectedIndex + 1;

                var calendarEvents = ExtractionMapper.ToCalendarEvents(_events, year, month, out int skipped);

                if (calendarEvents.Count == 0)
                {
                    await DisplayAlert("Sin eventos válidos",
                        "Ningún evento tiene un día válido para ese mes. Revisa los días.",
                        "Aceptar");
                    return;
                }

                var calendars = await _calendarService.GetCalendarsAsync();

                if (calendars.Count == 0)
                {
                    // Sin lista de calendarios (p. ej. iOS por ahora): abrir el editor del sistema
                    // con el primer evento.
                    await _calendarService.OpenEventEditorAsync(calendarEvents[0]);
                    return;
                }

                var options = calendars
                    .Select(c => $"{c.Name} - {c.AccountName}")
                    .ToArray();

                var selected = await DisplayActionSheet("Elige calendario", "Cancelar", null, options);

                if (selected == null || selected == "Cancelar")
                    return;

                int index = Array.IndexOf(options, selected);
                if (index < 0)
                    return;

                var calendar = calendars[index];

                await _calendarService.AddEventAsync(calendarEvents, calendar.Id);

                string message = $"Se han guardado {calendarEvents.Count} eventos en \"{calendar.Name}\".";
                if (skipped > 0)
                    message += $" {skipped} se han omitido por tener un día o título no válido.";

                await DisplayAlert("Guardado", message, "Aceptar");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }

        private void SetBusy(bool busy, string? status = null)
        {
            BusyIndicator.IsVisible = busy;
            BusyIndicator.IsRunning = busy;
            PickButton.IsEnabled = !busy;
            SaveButton.IsEnabled = !busy;

            StatusLabel.Text = status ?? string.Empty;
            StatusLabel.IsVisible = busy && !string.IsNullOrEmpty(status);
        }
    }
}
