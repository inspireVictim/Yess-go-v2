using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using YessGoFront.Models;
using YessGoFront.Services.Domain;

namespace YessGoFront.ViewModels;

/// <summary>
/// ViewModel для страницы корзины
/// </summary>
public partial class BasketViewModel : ObservableObject
{
    private readonly ICartService _cartService;
    private readonly ILogger<BasketViewModel>? _logger;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private ObservableCollection<PartnerCartGroup> partnerGroups = new();

    public BasketViewModel(ICartService cartService, ILogger<BasketViewModel>? logger = null)
    {
        _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadCartAsync()
    {
        try
        {
            IsBusy = true;
            IsEmpty = false;

            var itemsByPartner = await _cartService.GetCartItemsByPartnerAsync();
            
            PartnerGroups.Clear();

            if (!itemsByPartner.Any())
            {
                IsEmpty = true;
                return;
            }

            foreach (var (partnerId, items) in itemsByPartner)
            {
                var partnerName = items.FirstOrDefault()?.PartnerName ?? "Неизвестный партнёр";
                var partnerLogoUrl = items.FirstOrDefault()?.PartnerLogoUrl;
                
                var group = new PartnerCartGroup
                {
                    PartnerId = partnerId,
                    PartnerName = partnerName,
                    PartnerLogoUrl = partnerLogoUrl,
                    Items = new ObservableCollection<CartItem>(items)
                };

                // Вычисляем итоговые суммы для группы
                group.TotalPrice = items.Sum(item => item.TotalPrice);
                group.TotalYessCoins = items.Sum(item => item.TotalYessCoins);
                group.TotalDiscount = items
                    .Where(item => item.OriginalPrice.HasValue)
                    .Sum(item => (item.OriginalPrice.Value - item.Price) * item.Quantity);
                
                // Вычисляем процент скидки
                var totalOriginalPrice = items
                    .Where(item => item.OriginalPrice.HasValue)
                    .Sum(item => item.OriginalPrice.Value * item.Quantity);
                
                if (totalOriginalPrice > 0)
                {
                    group.DiscountPercent = (group.TotalDiscount / totalOriginalPrice) * 100;
                }

                PartnerGroups.Add(group);
            }

            IsEmpty = !PartnerGroups.Any();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading cart");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка",
                    "Не удалось загрузить корзину",
                    "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveItemAsync(CartItem? item)
    {
        if (item == null)
            return;

        try
        {
            await _cartService.RemoveFromCartAsync(item.ProductId);
            await LoadCartAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing item from cart");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка",
                    "Не удалось удалить товар из корзины",
                    "OK");
            }
        }
    }

    [RelayCommand]
    private async Task IncreaseQuantityAsync(CartItem? item)
    {
        if (item == null)
            return;

        try
        {
            await _cartService.UpdateQuantityAsync(item.ProductId, item.Quantity + 1);
            await LoadCartAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error increasing quantity");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка",
                    "Не удалось изменить количество",
                    "OK");
            }
        }
    }

    [RelayCommand]
    private async Task DecreaseQuantityAsync(CartItem? item)
    {
        if (item == null)
            return;

        try
        {
            var newQuantity = Math.Max(0, item.Quantity - 1);
            if (newQuantity == 0)
            {
                await RemoveItemAsync(item);
            }
            else
            {
                await _cartService.UpdateQuantityAsync(item.ProductId, newQuantity);
                await LoadCartAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error decreasing quantity");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка",
                    "Не удалось изменить количество",
                    "OK");
            }
        }
    }

    [RelayCommand]
    private async Task ProceedToOrderAsync(int partnerId)
    {
        // TODO: Реализовать переход на страницу оформления заказа
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Информация",
                "Функция оформления заказа будет реализована позже",
                "OK");
        }
    }
}

/// <summary>
/// Группа товаров корзины по партнёру
/// </summary>
public partial class PartnerCartGroup : ObservableObject
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? PartnerLogoUrl { get; set; }
    public ObservableCollection<CartItem> Items { get; set; } = new();
    
    public decimal TotalPrice { get; set; }
    public decimal TotalYessCoins { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal DiscountPercent { get; set; }
}

