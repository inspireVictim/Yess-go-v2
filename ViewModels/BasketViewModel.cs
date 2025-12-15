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
    private readonly SemaphoreSlim _updateQuantityLock = new(1, 1);

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
            if (_cartService == null)
            {
                _logger?.LogError("CartService is null");
                IsEmpty = true;
                return;
            }

            IsBusy = true;
            IsEmpty = false;

            var itemsByPartner = await _cartService.GetCartItemsByPartnerAsync();
            
            if (PartnerGroups == null)
            {
                _logger?.LogError("PartnerGroups is null");
                IsEmpty = true;
                return;
            }

            PartnerGroups.Clear();

            if (itemsByPartner == null || !itemsByPartner.Any())
            {
                IsEmpty = true;
                return;
            }

            foreach (var (partnerId, items) in itemsByPartner)
            {
                if (items == null || !items.Any())
                    continue;

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
                group.TotalPrice = items.Sum(item => item?.TotalPrice ?? 0);
                group.TotalYessCoins = items.Sum(item => item?.TotalYessCoins ?? 0);
                group.TotalDiscount = items
                    .Where(item => item != null && item.OriginalPrice.HasValue)
                    .Sum(item => (item.OriginalPrice.Value - item.Price) * item.Quantity);
                
                // Вычисляем процент скидки
                var totalOriginalPrice = items
                    .Where(item => item != null && item.OriginalPrice.HasValue)
                    .Sum(item => item.OriginalPrice.Value * item.Quantity);
                
                if (totalOriginalPrice > 0)
                {
                    group.DiscountPercent = (group.TotalDiscount / totalOriginalPrice) * 100;
                }

                // Загружаем координаты партнёра
                if (_partnersService != null)
                {
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
                }

                PartnerGroups.Add(group);
            }

            IsEmpty = !PartnerGroups.Any();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading cart");
            IsEmpty = true;
            try
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "Не удалось загрузить корзину",
                        "OK");
                }
            }
            catch (Exception alertEx)
            {
                _logger?.LogError(alertEx, "Error showing alert");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Обновляет количество товара в коллекции без перезагрузки всей корзины
    /// Выполняется на главном потоке для безопасности
    /// </summary>
    private async void UpdateCartItemInCollection(int productId, int newQuantity)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                foreach (var group in PartnerGroups)
                {
                    var cartItem = group.Items.FirstOrDefault(i => i.ProductId == productId);
                    if (cartItem != null)
                    {
                        // Обновляем количество (это автоматически вызовет OnPropertyChanged от CartItem)
                        cartItem.Quantity = newQuantity;
                        
                        // Пересчитываем итоги группы
                        group.RecalculateTotals();
                        
                        // НЕ заменяем элемент в коллекции - это нарушает привязки
                        // Вместо этого полагаемся на OnPropertyChanged от CartItem
                        
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка обновления количества в коллекции");
            }
        });
    }

    [RelayCommand]
    private async Task RemoveItemAsync(CartItem? item)
    {
        if (item == null)
            return;

        if (_cartService == null)
        {
            _logger?.LogError("CartService is null");
            return;
        }

        try
        {
            await _cartService.RemoveFromCartAsync(item.ProductId);
            await LoadCartAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing item from cart");
            try
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "Не удалось удалить товар из корзины",
                        "OK");
                }
            }
            catch (Exception alertEx)
            {
                _logger?.LogError(alertEx, "Error showing alert");
            }
        }
    }

    [RelayCommand]
    private async Task IncreaseQuantityAsync(CartItem? item)
    {
        if (item == null)
            return;

        if (_cartService == null)
        {
            _logger?.LogError("CartService is null");
            return;
        }

        if (!await _updateQuantityLock.WaitAsync(0))
            return; // Уже обрабатывается

        try
        {
            var newQuantity = item.Quantity + 1;
            await _cartService.UpdateQuantityAsync(item.ProductId, newQuantity);
            
            // Обновляем на главном потоке
            UpdateCartItemInCollection(item.ProductId, newQuantity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error increasing quantity");
            try
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "Не удалось изменить количество",
                        "OK");
                }
            }
            catch (Exception alertEx)
            {
                _logger?.LogError(alertEx, "Error showing alert");
            }
        }
        finally
        {
            _updateQuantityLock.Release();
        }
    }

    [RelayCommand]
    private async Task DecreaseQuantityAsync(CartItem? item)
    {
        if (item == null)
            return;

        if (_cartService == null)
        {
            _logger?.LogError("CartService is null");
            return;
        }

        if (!await _updateQuantityLock.WaitAsync(0))
            return; // Уже обрабатывается

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
                
                // Обновляем на главном потоке
                UpdateCartItemInCollection(item.ProductId, newQuantity);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error decreasing quantity");
            try
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Ошибка",
                        "Не удалось изменить количество",
                        "OK");
                }
            }
            catch (Exception alertEx)
            {
                _logger?.LogError(alertEx, "Error showing alert");
            }
        }
        finally
        {
            _updateQuantityLock.Release();
        }
    }

    [RelayCommand]
    private async Task ProceedToOrderAsync(int partnerId)
    {
        try
        {
            if (_partnersService == null)
            {
                _logger?.LogError("PartnersService is null");
                try
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Ошибка",
                            "Сервис партнёров недоступен",
                            "OK");
                    }
                }
                catch (Exception alertEx)
                {
                    _logger?.LogError(alertEx, "Error showing alert");
                }
                return;
            }

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
                try
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Ошибка",
                            "Не удалось загрузить информацию о партнёре",
                            "OK");
                    }
                }
                catch (Exception alertEx)
                {
                    _logger?.LogError(alertEx, "Error showing alert");
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
            if (PartnerGroups == null)
            {
                _logger?.LogError("PartnerGroups is null");
                return;
            }
            var partnerGroup = PartnerGroups.FirstOrDefault(g => g?.PartnerId == partnerId);
            if (partnerGroup == null || partnerGroup.Items == null || !partnerGroup.Items.Any())
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

    /// <summary>
    /// Пересчитывает итоговые суммы группы на основе текущих элементов
    /// </summary>
    public void RecalculateTotals()
    {
        TotalPrice = Items.Sum(item => item.TotalPrice);
        TotalYessCoins = Items.Sum(item => item.TotalYessCoins);
        TotalDiscount = Items
            .Where(item => item.OriginalPrice.HasValue)
            .Sum(item => (item.OriginalPrice.Value - item.Price) * item.Quantity);
        
        // Вычисляем процент скидки
        var totalOriginalPrice = Items
            .Where(item => item.OriginalPrice.HasValue)
            .Sum(item => item.OriginalPrice.Value * item.Quantity);
        
        if (totalOriginalPrice > 0)
        {
            DiscountPercent = (TotalDiscount / totalOriginalPrice) * 100;
        }
        else
        {
            DiscountPercent = 0;
        }
        
        // Уведомляем об изменении свойств
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(TotalYessCoins));
        OnPropertyChanged(nameof(TotalDiscount));
        OnPropertyChanged(nameof(DiscountPercent));
    }
}

