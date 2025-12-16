using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using ZXing;
using ZXing.Common;
using SkiaSharp;

namespace YessGoFront.Views;

[QueryProperty(nameof(PaymentUrl), "paymentUrl")]
public partial class FinikQrPage : ContentPage
{
    private string? _paymentUrl;

    public string? PaymentUrl
    {
        get => _paymentUrl;
        set
        {
            _paymentUrl = Uri.UnescapeDataString(value ?? string.Empty);
            _ = LoadQrCodeAsync();
        }
    }

    public FinikQrPage()
    {
        InitializeComponent();
    }

    private async Task LoadQrCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(_paymentUrl))
        {
            ShowError("Не получен URL для оплаты");
            return;
        }

        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            QrCodeImage.IsVisible = false;
            ErrorLabel.IsVisible = false;

            // Генерируем QR-код из URL
            var qrImageSource = GenerateQrCode(_paymentUrl);
            
            QrCodeImage.Source = qrImageSource;
            QrCodeImage.IsVisible = true;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinikQrPage] Ошибка генерации QR-кода: {ex.Message}");
            ShowError($"Ошибка генерации QR-кода: {ex.Message}");
        }
    }

    private ImageSource GenerateQrCode(string data)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = 300,
                Width = 300,
                Margin = 2
            }
        };

        var pixelData = writer.Write(data);

        using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height);
        var pixels = bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(
            pixelData.Pixels, 0, pixels, pixelData.Pixels.Length);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var imageBytes = encoded.ToArray();

        return ImageSource.FromStream(() => new MemoryStream(imageBytes));
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", animate: true);
    }
}

