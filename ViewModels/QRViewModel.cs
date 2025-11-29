using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YessGoFront.Services.QRService;

namespace YessGoFront.ViewModels;

public partial class QrViewModel : ObservableObject
{
    private readonly YessGoFront.Services.QRService.IQRService _qrService;

    public QrViewModel(YessGoFront.Services.QRService.IQRService qrService)
    {
        _qrService = qrService;
    }

    // -----------------------------
    // �������� ���������
    // -----------------------------

    [ObservableProperty]
    private bool isTorchOn;

    [ObservableProperty]
    private bool isQrOverlayOpen;

    [ObservableProperty]
    private string myQrImagePath;

    // -----------------------------
    // �������
    // -----------------------------

    [RelayCommand]
    private void ToggleFlash()
    {
        IsTorchOn = !IsTorchOn;
    }

    [RelayCommand]
    private async Task ShowMyQr()
    {
        MyQrImagePath = await _qrService.GenerateMyQrAsync();
        IsQrOverlayOpen = true;
    }

    [RelayCommand]
    private void CloseQr()
    {
        IsQrOverlayOpen = false;
    }

    [RelayCommand]
    private async Task Exit()
    {
        await Shell.Current.GoToAsync("///main/partner");
    }
}
