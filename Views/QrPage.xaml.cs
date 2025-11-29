using YessGoFront.ViewModels;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;
using YessGoFront.Models;
using Microsoft.Extensions.Logging;

namespace YessGoFront.Views;

public partial class QrPage : ContentPage
{
    private readonly IPartnersApiService _partnersService;
    private readonly ILogger<QrPage>? _logger;
    private bool _isProcessing = false;

    public QrPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<QrViewModel>();
        _partnersService = MauiProgram.Services.GetRequiredService<IPartnersApiService>();
        _logger = MauiProgram.Services.GetService<ILogger<QrPage>>();
    }

    private async void BarCodeReader_BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result == null || _isProcessing)
            return;

        _isProcessing = true;

        try
        {
            var scannedQrCode = result.Value;
            _logger?.LogInformation("QR код отсканирован: {QrCode}", scannedQrCode);

            await Dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    // Получаем список всех партнёров
                    var partners = await _partnersService.GetAllAsync();
                    
                    // Ищем партнёра с совпадающим QrCodeUrl
                    var matchedPartner = partners.FirstOrDefault(p => 
                        !string.IsNullOrWhiteSpace(p.QrCodeUrl) && 
                        p.QrCodeUrl.Equals(scannedQrCode, StringComparison.OrdinalIgnoreCase));

                    if (matchedPartner != null)
                    {
                        _logger?.LogInformation("Найден партнёр: {PartnerId} - {PartnerName}", matchedPartner.Id, matchedPartner.Name);
                        
                        // Переходим на страницу оплаты с параметрами партнёра
                        var partnerName = Uri.EscapeDataString(matchedPartner.Name ?? "");
                        var qrCode = Uri.EscapeDataString(scannedQrCode);
                        
                        await Shell.Current.GoToAsync($"payment?partnerId={matchedPartner.Id}&partnerName={partnerName}&qrCode={qrCode}");
                    }
                    else
                    {
                        _logger?.LogWarning("Партнёр с QR кодом {QrCode} не найден", scannedQrCode);
                        await DisplayAlert("QR код не найден", 
                            "Данный QR код не соответствует ни одному партнёру.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка обработки QR кода");
                    await DisplayAlert("Ошибка", 
                        "Не удалось обработать QR код. Попробуйте ещё раз.", "OK");
                }
                finally
                {
                    _isProcessing = false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при сканировании QR");
            _isProcessing = false;
        }
    }
}
