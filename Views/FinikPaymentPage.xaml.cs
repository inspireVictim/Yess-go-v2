using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using YessGoFront.Services.Api;
using YessGoFront.Services;

namespace YessGoFront.Views;

[QueryProperty(nameof(PaymentUrlString), "paymentUrl")]
[QueryProperty(nameof(RedirectUrlString), "redirectUrl")]
public partial class FinikPaymentPage : ContentPage
{
    private string? _paymentUrlString;
    private string? _redirectUrlString;
    private bool _isPaymentCompleted = false;
    private readonly SemaphoreSlim _actionLock = new(1, 1);

    public string? PaymentUrlString
    {
        get => _paymentUrlString;
        set
        {
            _paymentUrlString = value;
            if (!string.IsNullOrWhiteSpace(_paymentUrlString))
            {
                var decodedUrl = Uri.UnescapeDataString(_paymentUrlString);
                if (PaymentWebView != null)
                {
                    PaymentWebView.Source = decodedUrl;
                }
            }
        }
    }

    public string? RedirectUrlString
    {
        get => _redirectUrlString;
        set
        {
            _redirectUrlString = value;
            if (!string.IsNullOrWhiteSpace(_redirectUrlString))
            {
                _redirectUrlString = Uri.UnescapeDataString(_redirectUrlString);
            }
        }
    }

    public FinikPaymentPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        if (!string.IsNullOrWhiteSpace(_paymentUrlString) && PaymentWebView != null)
        {
            var decodedUrl = Uri.UnescapeDataString(_paymentUrlString);
            PaymentWebView.Source = decodedUrl;
            PaymentWebView.IsVisible = true;
            LoadingIndicator.IsVisible = false;
        }
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        Debug.WriteLine($"[FinikPaymentPage] Navigating to: {e.Url}");
        
        // Показываем индикатор загрузки
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        
        // Проверяем, не является ли это редиректом после успешной оплаты
        if (!string.IsNullOrWhiteSpace(_redirectUrlString) && 
            e.Url.Contains(_redirectUrlString, StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine("[FinikPaymentPage] Payment completed, redirect detected");
            e.Cancel = true;
            _isPaymentCompleted = true;
            _ = HandlePaymentSuccessAsync();
        }
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        Debug.WriteLine($"[FinikPaymentPage] Navigated to: {e.Url}, Result: {e.Result}");
        
        // Скрываем индикатор загрузки
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
        PaymentWebView.IsVisible = true;
        
        // Проверяем результат навигации
        if (e.Result != WebNavigationResult.Success)
        {
            Debug.WriteLine($"[FinikPaymentPage] Navigation failed: {e.Result}");
        }
    }

    private async Task HandlePaymentSuccessAsync()
    {
        try
        {
            Debug.WriteLine("[FinikPaymentPage] Handling payment success");
            
            // Обновляем баланс
            var balanceRefreshService = MauiProgram.Services?.GetService<BalanceRefreshService>();
            if (balanceRefreshService != null)
            {
                await balanceRefreshService.RefreshBalanceAsync();
                Debug.WriteLine("[FinikPaymentPage] Balance refreshed");
            }
            
            // Показываем сообщение об успехе
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Успешно", "Платеж успешно выполнен. Баланс обновлен.", "OK");
                
                // Возвращаемся назад
                await GoBackAsync();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FinikPaymentPage] Error handling payment success: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Ошибка", "Произошла ошибка при обработке платежа.", "OK");
            });
        }
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        if (!await _actionLock.WaitAsync(0))
            return;

        try
        {
            if (sender is Button btn)
                btn.IsEnabled = false;

            await GoBackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinikPaymentPage] Error in OnBackButtonClicked: {ex.Message}");
        }
        finally
        {
            if (sender is Button btn)
                btn.IsEnabled = true;
            _actionLock.Release();
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
            Debug.WriteLine($"[FinikPaymentPage] Navigation error: {ex.Message}");
        }
    }
}

