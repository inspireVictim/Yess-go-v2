using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;

namespace YessGoFront.Views;

public partial class BasketPage : ContentPage
{
    private readonly BasketViewModel _viewModel;

    public BasketPage()
    {
        InitializeComponent();

        // Получаем сервисы через DI
        var cartService = MauiProgram.Services.GetRequiredService<ICartService>();
        var logger = MauiProgram.Services.GetService<ILogger<BasketViewModel>>();

        _viewModel = new BasketViewModel(cartService, logger);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Загружаем данные корзины при появлении страницы
        if (_viewModel != null)
        {
            await _viewModel.LoadCartCommand.ExecuteAsync(null);
        }
    }

    public async void OnBackButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null)
            {
                return;
            }

            // Сначала пытаемся использовать Navigation.PopAsync (более надежный способ)
            if (Shell.Current.Navigation != null && Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync(animated: true);
                return;
            }

            // Если Navigation.PopAsync не сработал, используем Shell навигацию
            await Shell.Current.GoToAsync("..", animate: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BasketPage] Navigation error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BasketPage] StackTrace: {ex.StackTrace}");
            
            // Fallback: попытка вернуться назад через Shell навигацию
            try
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("..", animate: true);
                }
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"[BasketPage] Fallback navigation error: {fallbackEx.Message}");
            }
        }
    }
}
