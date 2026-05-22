using System.Threading.Tasks;
using CalendarOcrApp.Core.Interfaces;

namespace CalendarOcrApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IOcrService _ocrService;
      
        public MainPage(IOcrService ocrService)
        {
            InitializeComponent();
            _ocrService = ocrService;
        }

        private async Task OnPickImageClicked(object sender, EventArgs e)
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
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
