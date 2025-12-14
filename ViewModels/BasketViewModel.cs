using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using YessGoFront.Models;
using YessGoFront.Services.Domain;

namespace YessGoFront.ViewModels;

/// <summary>
/// ViewModel для страницы корзины
/// </summary>
public partial class BasketViewModel : ObservableObject
{
    private readonly ICartService _cartService;
    private readonly IPartnersService _partnersService;
    private readonly ILogger<BasketViewModel>? _logger;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private ObservableCollection<PartnerCartGroup> partnerGroups = new();

    public BasketViewModel(ICartService cartService, IPartnersService partnersService, ILogger<BasketViewModel>? logger = null)
    {
        _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
        _partnersService = partnersService ?? throw new ArgumentNullException(nameof(partnersService));
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

                // Загружаем координаты партнёра
                try
                {
                    var partner = await _partnersService.GetPartnerByIdAsync(partnerId.ToString());
                    if (partner != null)
                    {
                        group.PartnerLatitude = partner.Latitude;
                        group.PartnerLongitude = partner.Longitude;
                        group.PartnerAddress = partner.Address;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load partner coordinates for partner {PartnerId}", partnerId);
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
        try
        {
            IsBusy = true;

            // Получаем информацию о партнёре
            PartnerDetailDto? partner = null;
            try
            {
                partner = await _partnersService.GetPartnerByIdAsync(partnerId.ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading partner {PartnerId}", partnerId);
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "Не удалось загрузить информацию о партнёре",
                        "OK");
                }
                return;
            }

            if (partner == null)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "Партнёр не найден",
                        "OK");
                }
                return;
            }

            // Проверяем наличие номера телефона
            if (string.IsNullOrWhiteSpace(partner.Phone))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "У партнёра не указан номер телефона",
                        "OK");
                }
                return;
            }

            // Получаем товары партнёра из корзины
            var partnerGroup = PartnerGroups.FirstOrDefault(g => g.PartnerId == partnerId);
            if (partnerGroup == null || !partnerGroup.Items.Any())
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "Корзина пуста для данного партнёра",
                        "OK");
                }
                return;
            }

            // Проверяем, что местоположение выбрано
            if (!partnerGroup.IsLocationSelected || string.IsNullOrWhiteSpace(partnerGroup.SelectedAddress))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Внимание",
                        "Пожалуйста, выберите адрес доставки на карте",
                        "OK");
                }
                return;
            }

            // Формируем сообщение заказа
            var orderMessage = FormatOrderMessage(partnerGroup);

            // Создаём WhatsApp URL
            var whatsappUrl = CreateWhatsAppUrl(partner.Phone, orderMessage);

            // Открываем WhatsApp
            try
            {
                await Launcher.OpenAsync(new Uri(whatsappUrl));
                _logger?.LogInformation("Successfully opened WhatsApp for partner {PartnerId}", partnerId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening WhatsApp for partner {PartnerId}", partnerId);
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        $"Не удалось открыть WhatsApp: {ex.Message}",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error proceeding to order for partner {PartnerId}", partnerId);
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка",
                    "Не удалось оформить заказ",
                    "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Форматирует сообщение заказа с деталями товаров
    /// </summary>
    private string FormatOrderMessage(PartnerCartGroup partnerGroup)
    {
        var message = new StringBuilder();
        
        // Заголовок
        message.AppendLine($"Заказ для: {partnerGroup.PartnerName}");
        message.AppendLine();

        // Список товаров
        message.AppendLine("Товары:");
        int itemNumber = 1;
        foreach (var item in partnerGroup.Items)
        {
            var itemLine = $"{itemNumber}. {item.ProductName} (x{item.Quantity}) - {item.TotalPrice:F0} сом";
            
            // Добавляем Yess!Coins для товара, если есть
            var itemYessCoins = item.EffectiveYessCoins * item.Quantity;
            if (itemYessCoins > 0)
            {
                itemLine += $" + {itemYessCoins:F0} Yess!Coins";
            }
            
            message.AppendLine(itemLine);
            
            if (!string.IsNullOrWhiteSpace(item.ProductDescription))
            {
                message.AppendLine($"   {item.ProductDescription}");
            }
            
            message.AppendLine();
            itemNumber++;
        }

        // Итоговая информация
        message.AppendLine($"Итого: {partnerGroup.TotalPrice:F0} сом");
        
        if (partnerGroup.TotalYessCoins > 0)
        {
            message.AppendLine($"+ {partnerGroup.TotalYessCoins:F0} Yess!Coins");
        }
        
        if (partnerGroup.DiscountPercent > 0)
        {
            message.AppendLine($"Скидка: {partnerGroup.DiscountPercent:F0}%");
        }

        // Адрес доставки
        message.AppendLine();
        message.AppendLine("Адрес доставки:");
        if (!string.IsNullOrWhiteSpace(partnerGroup.SelectedAddress))
        {
            message.AppendLine(partnerGroup.SelectedAddress);
        }
        else
        {
            message.AppendLine("Не указан");
        }

        return message.ToString();
    }

    /// <summary>
    /// Создаёт WhatsApp URL с предзаполненным сообщением
    /// </summary>
    private string CreateWhatsAppUrl(string phoneNumber, string message)
    {
        // Очищаем номер телефона от пробелов, дефисов, скобок и других символов
        var cleanPhone = new StringBuilder();
        foreach (char c in phoneNumber)
        {
            if (char.IsDigit(c))
            {
                cleanPhone.Append(c);
            }
        }

        var phone = cleanPhone.ToString();
        
        // Кодируем сообщение для URL
        var encodedMessage = Uri.EscapeDataString(message);
        
        // Формируем WhatsApp URL
        return $"https://wa.me/{phone}?text={encodedMessage}";
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
    
    // Координаты партнёра
    [ObservableProperty]
    private double? partnerLatitude;
    
    [ObservableProperty]
    private double? partnerLongitude;
    
    [ObservableProperty]
    private string? partnerAddress;
    
    // Выбранное местоположение заказчика
    [ObservableProperty]
    private double? selectedLatitude;
    
    [ObservableProperty]
    private double? selectedLongitude;
    
    [ObservableProperty]
    private string? selectedAddress;
    
    [ObservableProperty]
    private bool isLocationSelected;
}

