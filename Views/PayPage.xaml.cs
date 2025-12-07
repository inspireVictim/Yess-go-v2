using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace YessGoFront.Views;

public partial class PayPage : ContentPage
{
    private bool _isNavigating = false;

    public PayPage()
    {
        InitializeComponent();
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
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
        if (_isNavigating) return;
        _isNavigating = true;

        try
        {
            Debug.WriteLine("[PayPage] Navigating to QrPage");
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("///qr", animate: true);
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