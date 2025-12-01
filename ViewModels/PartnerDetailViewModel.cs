using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using YessGoFront.Converters;
using YessGoFront.Models;
using YessGoFront.Services.Domain;

namespace YessGoFront.ViewModels;

/// <summary>
/// ViewModel для страницы детальной информации о партнёре
/// </summary>
public partial class PartnerDetailViewModel : ObservableObject
{
    private readonly IPartnersService _partnersService;
    private readonly ICartService _cartService;
    private readonly ILogger<PartnerDetailViewModel>? _logger;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private PartnerDetailDto? partner;

    [ObservableProperty]
    private string partnerName = string.Empty;

    [ObservableProperty]
    private string? partnerDescription;

    [ObservableProperty]
    private ImageSource? coverImageSource;

    private ImageSource? _logoImageSource;

    private string _logoText = string.Empty;

    private bool _isLogoTextVisible = true;

    private bool _isLogoImageVisible = false;

    // Явные свойства для логотипа
    public ImageSource? LogoImageSource
    {
        get => _logoImageSource;
        set
        {
            if (SetProperty(ref _logoImageSource, value))
            {
                OnPropertyChanged();
            }
        }
    }

    public string LogoText
    {
        get => _logoText;
        set
        {
            if (SetProperty(ref _logoText, value))
            {
                OnPropertyChanged();
            }
        }
    }

    public bool IsLogoTextVisible
    {
        get => _isLogoTextVisible;
        set
        {
            if (SetProperty(ref _isLogoTextVisible, value))
            {
                OnPropertyChanged();
            }
        }
    }

    public bool IsLogoImageVisible
    {
        get => _isLogoImageVisible;
        set
        {
            if (SetProperty(ref _isLogoImageVisible, value))
            {
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    private string promoText = "скидки на все";

    [ObservableProperty]
    private string promoPercent = "-30%";

    [ObservableProperty]
    private bool isPromoVisible = true;

    [ObservableProperty]
    private string selectedTab = "Products"; // "Products" или "Reviews"

    public ObservableCollection<ProductDto> Products { get; } = new();

    public IAsyncRelayCommand<int> LoadPartnerCommand { get; }
    public IAsyncRelayCommand GoBackCommand { get; }
    public IAsyncRelayCommand<ProductDto> AddToCartCommand { get; }
    public IRelayCommand<string> SelectTabCommand { get; }

    public PartnerDetailViewModel(
        IPartnersService partnersService,
        ICartService cartService,
        ILogger<PartnerDetailViewModel>? logger = null)
    {
        _partnersService = partnersService ?? throw new ArgumentNullException(nameof(partnersService));
        _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
        _logger = logger;

        LoadPartnerCommand = new AsyncRelayCommand<int>(LoadPartnerAsync);
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        AddToCartCommand = new AsyncRelayCommand<ProductDto>(AddToCartAsync);
        SelectTabCommand = new RelayCommand<string>(SelectTab);
    }

    private void SelectTab(string? tab)
    {
        if (!string.IsNullOrWhiteSpace(tab))
        {
            SelectedTab = tab;
        }
    }

    private async Task LoadPartnerAsync(int partnerId)
    {
        if (IsBusy || partnerId <= 0)
        {
            _logger?.LogWarning("LoadPartnerAsync: Invalid partnerId {PartnerId} or already busy", partnerId);
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = null;

            _logger?.LogDebug("Loading partner with id: {PartnerId} (type: int)", partnerId);

            // Загружаем информацию о партнёре
            var partnerIdString = partnerId.ToString();
            var partnerData = await _partnersService.GetPartnerByIdAsync(partnerIdString);

            if (partnerData == null)
            {
                _logger?.LogWarning("Partner {PartnerId} not found", partnerId);
                HasError = true;
                ErrorMessage = $"Партнёр №{partnerId} не найден";
                PartnerName = $"Партнёр №{partnerId}";
                return;
            }

            Partner = partnerData;
            PartnerName = partnerData.Name;
            PartnerDescription = partnerData.Description;

            // Загружаем обложку
            if (!string.IsNullOrWhiteSpace(partnerData.CoverImageUrl))
            {
                var converter = new StringToImageSourceConverter();
                var imageSource = converter.Convert(
                    partnerData.CoverImageUrl,
                    typeof(ImageSource),
                    null,
                    System.Globalization.CultureInfo.CurrentCulture) as ImageSource;
                CoverImageSource = imageSource;
            }
            else
            {
                CoverImageSource = null;
            }

            // Устанавливаем логотип или текст
            await SetLogoAsync(partnerData);

            // Устанавливаем промо-информацию
            SetPromoInfo(partnerData);

            // Загружаем продукты
            await LoadProductsAsync(partnerId);

            _logger?.LogDebug("Partner {PartnerId} loaded successfully. Products count: {Count}", 
                partnerId, Products.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading partner {PartnerId}", partnerId);
            HasError = true;
            ErrorMessage = "Не удалось загрузить информацию о партнёре";
            PartnerName = partnerId > 0 ? $"Партнёр №{partnerId}" : "Партнёр";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SetLogoAsync(PartnerDetailDto partner)
    {
        System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] ===== SetLogoAsync =====");
        System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] LogoUrl: '{partner.LogoUrl}'");
        System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] LogoUrl is null: {partner.LogoUrl == null}");
        System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] LogoUrl is empty: {string.IsNullOrEmpty(partner.LogoUrl)}");
        System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] LogoUrl is whitespace: {string.IsNullOrWhiteSpace(partner.LogoUrl)}");
        
        if (string.IsNullOrWhiteSpace(partner.LogoUrl))
        {
            // Используем текст по умолчанию
            LogoText = partner.Name.Length > 10 
                ? partner.Name.Substring(0, 10).ToUpper() 
                : partner.Name.ToUpper();
            IsLogoTextVisible = true;
            IsLogoImageVisible = false;
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] LogoUrl пустой, показываем текст: '{LogoText}'");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] Вызываем конвертер для: '{partner.LogoUrl}'");
            var converter = new StringToImageSourceConverter();
            var logoSource = converter.Convert(
                partner.LogoUrl,
                typeof(ImageSource),
                null,
                System.Globalization.CultureInfo.CurrentCulture) as ImageSource;

            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] Конвертер вернул: {(logoSource != null ? "ImageSource" : "null")}");

            if (logoSource != null)
            {
                LogoImageSource = logoSource;
                IsLogoImageVisible = true;
                IsLogoTextVisible = false;
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] ✅ Логотип установлен: IsLogoImageVisible={IsLogoImageVisible}, IsLogoTextVisible={IsLogoTextVisible}");
            }
            else
            {
                // Используем текст, если изображение не загрузилось
                LogoText = partner.Name.Length > 10 
                    ? partner.Name.Substring(0, 10).ToUpper() 
                    : partner.Name.ToUpper();
                IsLogoTextVisible = true;
                IsLogoImageVisible = false;
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] ❌ Конвертер вернул null, показываем текст: '{LogoText}'");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading logo for partner {PartnerId}", partner.Id);
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] ❌ Ошибка загрузки логотипа: {ex.Message}");
            // Используем текст в случае ошибки
            LogoText = partner.Name.Length > 10 
                ? partner.Name.Substring(0, 10).ToUpper() 
                : partner.Name.ToUpper();
            IsLogoTextVisible = true;
            IsLogoImageVisible = false;
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] Показываем текст после ошибки: '{LogoText}'");
        }
        
        System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] ===== SetLogoAsync ЗАВЕРШЕНО =====");
    }

    private void SetPromoInfo(PartnerDetailDto partner)
    {
        // Устанавливаем промо-текст
        if (partner.CurrentPromotions != null && partner.CurrentPromotions.Count > 0)
        {
            PromoText = partner.CurrentPromotions.FirstOrDefault() ?? "скидки на все";
        }
        else
        {
            PromoText = "скидки на все";
        }

        // Устанавливаем максимальную скидку
        if (partner.MaxDiscountPercent.HasValue && partner.MaxDiscountPercent.Value > 0)
        {
            PromoPercent = $"-{partner.MaxDiscountPercent.Value:F0}%";
            IsPromoVisible = true;
        }
        else
        {
            IsPromoVisible = false;
        }
    }

    private async Task LoadProductsAsync(int partnerId)
    {
        try
        {
            Products.Clear();
            
            _logger?.LogInformation("🛒 [LoadProductsAsync] Starting to load products for partnerId: {PartnerId} (type: int)", partnerId);
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] 🛒 Starting to load products for partnerId: {partnerId} (type: int)");
            
            // Преобразуем int в string для совместимости с сервисом
            var partnerIdString = partnerId.ToString();
            
            _logger?.LogInformation("🛒 [LoadProductsAsync] Calling GetPartnerProductsAsync with partnerId: '{PartnerIdString}'", 
                partnerIdString);
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] 🛒 Calling GetPartnerProductsAsync with partnerId: '{partnerIdString}'");
            
            var products = await _partnersService.GetPartnerProductsAsync(partnerIdString);
            
            _logger?.LogInformation("🛒 [LoadProductsAsync] Received {Count} products from service for partner {PartnerId}", 
                products?.Count ?? 0, partnerId);
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] 🛒 Received {products?.Count ?? 0} products from service for partner {partnerId}");
            
            if (products != null && products.Any())
            {
                foreach (var product in products)
                {
                    Products.Add(product);
                    _logger?.LogInformation("  ➕ Added product to collection: Id={ProductId}, Name={ProductName}, PartnerId={ProductPartnerId}, Price={Price}", 
                        product.Id, product.Name, product.PartnerId, product.Price);
                }

                _logger?.LogInformation("✅ [LoadProductsAsync] Successfully loaded {Count} products for partner {PartnerId}. " +
                    "Total in ObservableCollection: {Total}", products.Count, partnerId, Products.Count);
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] ✅ Successfully loaded {products.Count} products for partner {partnerId}. Total in ObservableCollection: {Products.Count}");
            }
            else
            {
                _logger?.LogWarning("⚠️ [LoadProductsAsync] No products found for partner {PartnerId}. " +
                    "Products collection is empty. Check database for partner_id = {PartnerId}", partnerId, partnerId);
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewModel] ⚠️ No products found for partner {partnerId}. Products collection is empty. Check database for partner_id = {partnerId}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ [LoadProductsAsync] Error loading products for partner {PartnerId}. " +
                "Exception: {ExceptionMessage}", partnerId, ex.Message);
            // Не устанавливаем ошибку, так как партнёр может быть загружен, а продукты - нет
            Products.Clear();
        }
    }

    private async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..", animate: true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error navigating back");
            try
            {
                await Shell.Current.GoToAsync("///main/home", animate: true);
            }
            catch
            {
                // Игнорируем ошибку навигации
            }
        }
    }

    private async Task AddToCartAsync(ProductDto? product)
    {
        if (product == null || Partner == null)
            return;

        try
        {
            _logger?.LogDebug("Adding product {ProductId} ({ProductName}) to cart", product.Id, product.Name);
            
            await _cartService.AddToCartAsync(
                product,
                Partner.Id,
                Partner.Name,
                Partner.LogoUrl);
            
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Добавлено", 
                    $"Продукт {product.Name} добавлен в корзину", 
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding product {ProductId} to cart", product.Id);
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка", 
                    "Не удалось добавить продукт в корзину", 
                    "OK");
            }
        }
    }
}

