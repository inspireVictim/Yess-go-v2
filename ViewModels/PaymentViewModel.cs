using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YessGoFront.Services.Domain;
using YessGoFront.Services.Api;
using Microsoft.Extensions.Logging;

namespace YessGoFront.ViewModels;

public partial class PaymentViewModel : ObservableObject
{
    private readonly YessGoFront.Services.Domain.IQRService _qrService;
    private readonly IWalletService _walletService;
    private readonly ILogger<PaymentViewModel>? _logger;

    [ObservableProperty]
    private int partnerId;

    [ObservableProperty]
    private string partnerName = string.Empty;

    [ObservableProperty]
    private string qrCode = string.Empty;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool hasError;

    public PaymentViewModel(
        YessGoFront.Services.Domain.IQRService qrService,
        IWalletService walletService,
        ILogger<PaymentViewModel>? logger = null)
    {
        _qrService = qrService ?? throw new ArgumentNullException(nameof(qrService));
        _walletService = walletService ?? throw new ArgumentNullException(nameof(walletService));
        _logger = logger;
    }

    [RelayCommand]
    private async Task ProcessPayment()
    {
        if (IsBusy || Amount <= 0)
            return;

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = null;

            _logger?.LogInformation("Обработка платежа: PartnerId={PartnerId}, Amount={Amount}, QrCode={QrCode}", 
                PartnerId, Amount, QrCode);

            // Создаём транзакцию через QR сервис
            var result = await _qrService.ScanQRAsync(QrCode, Amount);

            if (result.Success)
            {
                _logger?.LogInformation("Платёж успешно обработан: TransactionId={TransactionId}, Cashback={Cashback}", 
                    result.TransactionId, result.CashbackAmount);

                await Shell.Current.DisplayAlert("Успешно", 
                    $"Платёж на сумму {Amount:0.##} KGS выполнен успешно!\n" +
                    $"Начислено кэшбэка: {result.CashbackAmount:0.##} Yess!Coin", 
                    "OK");

                // Возвращаемся на главную страницу
                await Shell.Current.GoToAsync("///main/partner");
            }
            else
            {
                HasError = true;
                ErrorMessage = result.Message ?? "Не удалось обработать платёж";
                await Shell.Current.DisplayAlert("Ошибка", ErrorMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при обработке платежа");
            HasError = true;
            ErrorMessage = ex.Message;
            await Shell.Current.DisplayAlert("Ошибка", 
                $"Не удалось обработать платёж: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetAmount(string amountStr)
    {
        if (decimal.TryParse(amountStr, out var amount))
        {
            Amount = amount;
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}

