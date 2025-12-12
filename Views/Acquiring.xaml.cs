using System;
using System.Threading;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using YessGoFront.Services;
using YessGoFront.Services.Api;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Exceptions;
using System.Diagnostics;

namespace YessGoFront.Views;

[QueryProperty(nameof(AmountString), "amount")]
public partial class Acquiring : ContentPage
{
    private string? _amountString;
    private decimal _amount;
    private readonly IPaymentApiService? _paymentApiService;
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
            _paymentApiService = MauiProgram.Services?.GetService<IPaymentApiService>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Acquiring] Error getting PaymentApiService: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Обновляем Label при появлении страницы, если сумма уже установлена
        UpdateAmountLabel();
        UpdateYessCoinLabel();
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
        
        // Обновляем также YessCoin
        UpdateYessCoinLabel();
    }

    private void UpdateYessCoinLabel()
    {
        // Рассчитываем YessCoin: сумма пополнения * 2
        decimal yessCoinAmount = _amount * 2;
        
        // Используем NameScopeExtensions для поиска элемента
        var yessCoinLabel = NameScopeExtensions.FindByName<Label>(this, "YessCoinLabel");
        if (yessCoinLabel != null)
        {
            yessCoinLabel.Text = $"{yessCoinAmount:0.##} YessCoin";
        }
    }

    private void UpdatePaymentButtonState()
    {
        var payButton = NameScopeExtensions.FindByName<Button>(this, "PayButton");
        var loadingIndicator = NameScopeExtensions.FindByName<ActivityIndicator>(this, "LoadingIndicator");
        
        if (payButton != null)
        {
            payButton.IsEnabled = !_isProcessingPayment && _amount > 0 && _paymentApiService != null;
        }
        
        if (loadingIndicator != null)
        {
            loadingIndicator.IsRunning = _isProcessingPayment;
            loadingIndicator.IsVisible = _isProcessingPayment;
        }
    }

    private async void OnPayButtonClicked(object? sender, EventArgs e)
    {
        if (_isProcessingPayment || _amount <= 0 || _paymentApiService == null)
        {
            return;
        }

        try
        {
            _isProcessingPayment = true;
            UpdatePaymentButtonState();

            Debug.WriteLine($"[Acquiring] Starting payment for amount: {_amount} KGS");

            // Используем таймаут для создания платежа (15 секунд)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            // Вызываем backend для создания платежа
            var response = await _paymentApiService.CreatePaymentAsync(_amount, cts.Token);

            _isProcessingPayment = false;
            UpdatePaymentButtonState();

            // Проверяем, что получили paymentUrl
            if (string.IsNullOrWhiteSpace(response?.PaymentUrl))
            {
                Debug.WriteLine("[Acquiring] PaymentUrl is empty in response");
                await DisplayAlert("Ошибка", "Не удалось получить ссылку на оплату. Попробуйте снова.", "OK");
                return;
            }

            Debug.WriteLine($"[Acquiring] Payment created, opening WebView with URL: {response.PaymentUrl}");

            // Открываем FinikPaymentPage с paymentUrl и redirectUrl
            var paymentUrlEncoded = Uri.EscapeDataString(response.PaymentUrl);
            var redirectUrlEncoded = !string.IsNullOrWhiteSpace(response.RedirectUrl) 
                ? Uri.EscapeDataString(response.RedirectUrl) 
                : string.Empty;
            
            // Используем полное имя маршрута для навигации
            var navigationPath = !string.IsNullOrWhiteSpace(redirectUrlEncoded)
                ? $"{nameof(Views.FinikPaymentPage)}?paymentUrl={paymentUrlEncoded}&redirectUrl={redirectUrlEncoded}"
                : $"{nameof(Views.FinikPaymentPage)}?paymentUrl={paymentUrlEncoded}";
            
            Debug.WriteLine($"[Acquiring] Navigating to: {navigationPath}");
            await Shell.Current.GoToAsync(navigationPath, animate: true);
        }
        catch (OperationCanceledException)
        {
            _isProcessingPayment = false;
            UpdatePaymentButtonState();
            
            Debug.WriteLine("[Acquiring] Payment creation timed out");
            await DisplayAlert("Ошибка", "Операция создания платежа заняла слишком много времени. Проверьте подключение к интернету и попробуйте снова.", "OK");
        }
        catch (BadRequestException badRequestEx)
        {
            _isProcessingPayment = false;
            UpdatePaymentButtonState();
            
            Debug.WriteLine($"[Acquiring] BadRequest error: {badRequestEx.Message}");
            Debug.WriteLine($"[Acquiring] Full exception: {badRequestEx}");
            
            // Показываем понятное сообщение пользователю
            var userMessage = badRequestEx.Message.Contains("подпись") || badRequestEx.Message.Contains("signature")
                ? "Ошибка на сервере при создании платежа. Пожалуйста, попробуйте позже или обратитесь в поддержку."
                : badRequestEx.Message;
            
            await DisplayAlert("Ошибка создания платежа", userMessage, "OK");
        }
        catch (Exception ex)
        {
            _isProcessingPayment = false;
            UpdatePaymentButtonState();
            
            Debug.WriteLine($"[Acquiring] Error processing payment: {ex.Message}");
            Debug.WriteLine($"[Acquiring] Exception type: {ex.GetType().Name}");
            Debug.WriteLine($"[Acquiring] StackTrace: {ex.StackTrace}");
            
            // Показываем общее сообщение об ошибке
            var userMessage = "Произошла ошибка при создании платежа. Пожалуйста, проверьте подключение к интернету и попробуйте снова.";
            await DisplayAlert("Ошибка", userMessage, "OK");
        }
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        await GoBackAsync();
    }

    private async Task RefreshBalanceAsync()
    {
        try
        {
            var balanceRefreshService = MauiProgram.Services?.GetService<YessGoFront.Services.BalanceRefreshService>();
            if (balanceRefreshService != null)
            {
                await balanceRefreshService.RefreshBalanceAsync();
                Debug.WriteLine("[Acquiring] Balance refreshed after successful payment");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Acquiring] Error refreshing balance: {ex.Message}");
        }
    }

    private async Task GoBackAsync()
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