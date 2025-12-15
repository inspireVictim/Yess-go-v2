using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YessGoFront.Views;

public partial class PayPage : ContentPage
{
    private bool _isNavigating = false;
    private readonly SemaphoreSlim _actionLock = new(1, 1); // Защита от повторных нажатий

    public PayPage()
    {
        InitializeComponent();
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        // Защита от повторных нажатий
        if (!await _actionLock.WaitAsync(0))
            return; // Уже обрабатывается

        try
        {
            // Отключаем кнопку визуально
            if (sender is VisualElement element)
                element.IsEnabled = false;

            await OnBackButtonClickedAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PayPage] Error in OnBackButtonClicked: {ex.Message}");
        }
        finally
        {
            if (sender is VisualElement element)
                element.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnBackButtonClickedAsync()
    {
        if (_isNavigating) return;
        _isNavigating = true;

        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("..", animate: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PayPage] Navigation error: {ex.Message}");
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async void OnQrPaymentClicked(object? sender, EventArgs e)
    {
        // Защита от повторных нажатий
        if (!await _actionLock.WaitAsync(0))
            return; // Уже обрабатывается

        try
        {
            // Отключаем кнопку визуально
            if (sender is VisualElement element)
                element.IsEnabled = false;

            await OnQrPaymentClickedAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PayPage] Error in OnQrPaymentClicked: {ex.Message}");
        }
        finally
        {
            if (sender is VisualElement element)
                element.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnQrPaymentClickedAsync()
    {
        if (_isNavigating) return;
        _isNavigating = true;

        try
        {
            Debug.WriteLine("[PayPage] Navigating to QrPage");
            if (Shell.Current != null)
            {
                // Используем единый и стабильный маршрут таба QR
                await Shell.Current.GoToAsync("//main/qr", animate: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PayPage] Error navigating to QrPage: {ex.Message}");
            try
            {
                await Shell.Current.GoToAsync("//qr", animate: true);
            }
            catch
            {
                // Ignore secondary error
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async void OnSearchPartnerClicked(object? sender, EventArgs e)
    {
        // Защита от повторных нажатий
        if (!await _actionLock.WaitAsync(0))
            return; // Уже обрабатывается

        try
        {
            // Отключаем кнопку визуально
            if (sender is VisualElement element)
                element.IsEnabled = false;

            await OnSearchPartnerClickedAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PayPage] Error in OnSearchPartnerClicked: {ex.Message}");
        }
        finally
        {
            if (sender is VisualElement element)
                element.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnSearchPartnerClickedAsync()
    {
        if (_isNavigating) return;
        _isNavigating = true;

        try
        {
            Debug.WriteLine("[PayPage] Navigating to SearchPartnersPay");
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("SearchPartnersPay", animate: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PayPage] Error navigating to SearchPartnersPay: {ex.Message}");
        }
        finally
        {
            _isNavigating = false;
        }
    }
}