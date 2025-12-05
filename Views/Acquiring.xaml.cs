using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using YessGoFront.Services;
using YessGoFront.Config;
using System.Diagnostics;

namespace YessGoFront.Views;

[QueryProperty(nameof(AmountString), "amount")]
public partial class Acquiring : ContentPage
{
    private string? _amountString;
    private decimal _amount;
    private readonly IFinikPaymentService? _finikPaymentService;
    private bool _isProcessingPayment = false;

    public string? AmountString
    {
        get => _amountString;
        set
        {
            _amountString = value;
            // Парсим строку в decimal
            if (!string.IsNullOrWhiteSpace(_amountString) && decimal.TryParse(_amountString, out var parsedAmount))
            {
                _amount = parsedAmount;
                // Обновляем Label с суммой после инициализации
                UpdateAmountLabel();
            }
        }
    }

    public decimal Amount => _amount;

    public Acquiring()
    {
        InitializeComponent();
        
        // Получаем сервис из DI
        try
        {
            _finikPaymentService = MauiProgram.Services?.GetService<IFinikPaymentService>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Acquiring] Error getting FinikPaymentService: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Обновляем Label при появлении страницы, если сумма уже установлена
        UpdateAmountLabel();
        UpdatePaymentButtonState();
    }

    private void UpdateAmountLabel()
    {
        // Используем NameScopeExtensions для поиска элемента
        var amountLabel = NameScopeExtensions.FindByName<Label>(this, "AmountLabel");
        if (amountLabel != null)
        {
            amountLabel.Text = $"{_amount:0.##} KGS";
        }
    }

    private void UpdatePaymentButtonState()
    {
        var payButton = NameScopeExtensions.FindByName<Button>(this, "PayButton");
        var loadingIndicator = NameScopeExtensions.FindByName<ActivityIndicator>(this, "LoadingIndicator");
        
        if (payButton != null)
        {
            payButton.IsEnabled = !_isProcessingPayment && _amount > 0 && _finikPaymentService != null;
        }
        
        if (loadingIndicator != null)
        {
            loadingIndicator.IsRunning = _isProcessingPayment;
            loadingIndicator.IsVisible = _isProcessingPayment;
        }
    }

    private async void OnPayButtonClicked(object? sender, EventArgs e)
    {
        if (_isProcessingPayment || _amount <= 0 || _finikPaymentService == null)
        {
            return;
        }

        try
        {
            _isProcessingPayment = true;
            UpdatePaymentButtonState();

            Debug.WriteLine($"[Acquiring] Starting payment for amount: {_amount} KGS");

            // Создаем запрос на оплату
            var paymentRequest = new PaymentRequest
            {
                Amount = _amount,
                NameEn = "Balance Replenishment",
                Description = $"Пополнение баланса YessGo на сумму {_amount:0.##} KGS",
                RequestId = Guid.NewGuid().ToString(),
                MaxAvailableQuantity = 1,
                RequiredFields = new Dictionary<string, string>
                {
                    { "amount", _amount.ToString("F2") },
                    { "requestId", Guid.NewGuid().ToString() }
                }
            };

            // Вызываем Finik SDK
            var result = await _finikPaymentService.ProcessPaymentAsync(paymentRequest);

            _isProcessingPayment = false;
            UpdatePaymentButtonState();

            // Обрабатываем результат
            if (result.IsCancelled)
            {
                Debug.WriteLine("[Acquiring] Payment was cancelled by user");
                await DisplayAlert("Отмена", "Оплата отменена", "OK");
            }
            else if (result.IsSuccess)
            {
                Debug.WriteLine($"[Acquiring] Payment successful. Transaction ID: {result.TransactionId}");
                await DisplayAlert(
                    "Успешно",
                    $"Оплата выполнена успешно!\nТранзакция: {result.TransactionId}",
                    "OK");
                
                // Можно здесь обновить баланс или вернуться назад
                // await RefreshBalance();
                await OnBackButtonClicked(null, EventArgs.Empty);
            }
            else
            {
                Debug.WriteLine($"[Acquiring] Payment failed: {result.ErrorMessage}");
                await DisplayAlert(
                    "Ошибка",
                    $"Не удалось выполнить оплату: {result.ErrorMessage ?? "Неизвестная ошибка"}",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _isProcessingPayment = false;
            UpdatePaymentButtonState();
            
            Debug.WriteLine($"[Acquiring] Error processing payment: {ex.Message}");
            await DisplayAlert("Ошибка", $"Произошла ошибка при обработке платежа: {ex.Message}", "OK");
        }
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null)
            {
                return;
            }

            // Возвращаемся назад
            if (Shell.Current.Navigation != null && Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync(animated: true);
            }
            else
            {
                await Shell.Current.GoToAsync("..", animate: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Acquiring] Navigation error: {ex.Message}");
        }
    }
}