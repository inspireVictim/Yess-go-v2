using ZXing;
using ZXing.Common;
using ZXing.Net.Maui;
using SkiaSharp;

namespace YessGoFront.Services.QRService;

public interface IQRService
{
    Task<string> GenerateMyQrAsync();
}

public class QRService : IQRService
{
    private readonly string _qrFilePath =
        Path.Combine(FileSystem.AppDataDirectory, "my_qr.png");

    public async Task<string> GenerateMyQrAsync()
    {
        if (File.Exists(_qrFilePath))
            return _qrFilePath;

        string qrData = "YESSGO-" + Guid.NewGuid();

        var writer = new ZXing.BarcodeWriterPixelData
        {
            Format = ZXing.BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = 300,
                Width = 300,
                Margin = 1
            }
        };

        var pixelData = writer.Write(qrData);

        using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height);
        var pixels = bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, pixels, pixelData.Pixels.Length);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(_qrFilePath);
        data.SaveTo(stream);

        return _qrFilePath;
    }
}
