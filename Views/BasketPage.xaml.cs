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
                System.Diagnostics.Debug.WriteLine("[BasketPage] Shell.Current is null");
                return;
            }

            // Если есть товары в корзине, переходим к первому партнёру
            if (_viewModel != null && _viewModel.PartnerGroups.Count > 0)
            {
                var firstPartner = _viewModel.PartnerGroups.FirstOrDefault();
                if (firstPartner != null)
                {
                    var partnerId = firstPartner.PartnerId.ToString();
                    // Используем абсолютный путь для гарантированного перехода
                    var route = $"///PartnerDetailViewPage?partnerId={Uri.EscapeDataString(partnerId)}";
                    
                    System.Diagnostics.Debug.WriteLine($"[BasketPage] Navigating to PartnerDetailViewPage with partnerId: {partnerId}");
                    System.Diagnostics.Debug.WriteLine($"[BasketPage] Route: {route}");
                    
                    await Shell.Current.GoToAsync(route, animate: true);
                    return;
                }
            }
            
            // Если корзина пуста, пытаемся вернуться назад стандартным способом
            System.Diagnostics.Debug.WriteLine("[BasketPage] Cart is empty, trying to go back");
            await Shell.Current.GoToAsync("..", animate: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BasketPage] Navigation error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BasketPage] StackTrace: {ex.StackTrace}");
            
            // Fallback: попытка вернуться назад
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
