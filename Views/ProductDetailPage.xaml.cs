using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using YessGoFront.Models;
using YessGoFront.Services.Domain;

namespace YessGoFront.Views;

[QueryProperty(nameof(ProductId), "productId")]
[QueryProperty(nameof(PartnerId), "partnerId")]
public partial class ProductDetailPage : ContentPage
{
    private string? productId;
    private string? partnerId;
    private ProductDto? _product;
    private PartnerDetailDto? _partner;
    private readonly IPartnersService _partnersService;
    private readonly ICartService _cartService;
    private readonly ILogger<ProductDetailPage>? _logger;
    private bool _isAppearing = false; // Защита от повторных вызовов OnAppearing
    private readonly SemaphoreSlim _actionLock = new(1, 1); // Защита от повторных нажатий

    public string? ProductId
    {
        get => productId;
        set
        {
            productId = value;
            System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] ProductId set to: '{productId}'");
        }
    }

    public string? PartnerId
    {
        get => partnerId;
        set
        {
            partnerId = value;
            System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] PartnerId set to: '{partnerId}'");
        }
    }

    public ProductDetailPage()
    {
        InitializeComponent();

        // Получаем сервисы через DI
        _partnersService = MauiProgram.Services.GetRequiredService<IPartnersService>();
        _cartService = MauiProgram.Services.GetRequiredService<ICartService>();
        _logger = MauiProgram.Services.GetService<ILogger<ProductDetailPage>>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isAppearing)
            return; // Уже выполняется

        _isAppearing = true;
        try
        {
            await OnAppearingAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] Error in OnAppearing: {ex.Message}");
        }
        finally
        {
            _isAppearing = false;
        }
    }

    protected virtual async Task OnAppearingAsync()
    {
        // Загружаем данные после того, как все QueryProperty установлены
        if (!string.IsNullOrWhiteSpace(productId) && !string.IsNullOrWhiteSpace(partnerId))
        {
            await LoadProductAsync();
        }
        else
        {
            _logger?.LogWarning("ProductId or PartnerId is empty. ProductId: {ProductId}, PartnerId: {PartnerId}", productId, partnerId);
        }
    }

    private async Task LoadProductAsync()
    {
        if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(partnerId))
        {
            _logger?.LogWarning("ProductId or PartnerId is empty. ProductId: {ProductId}, PartnerId: {PartnerId}", productId, partnerId);
            return;
        }

        if (!int.TryParse(productId, out var productIdInt))
        {
            _logger?.LogWarning("Failed to parse ProductId: {ProductId}", productId);
            return;
        }

        try
        {
            // Загружаем информацию о партнёре
            _partner = await _partnersService.GetPartnerByIdAsync(partnerId);

            // Загружаем все товары партнёра
            var products = await _partnersService.GetPartnerProductsAsync(partnerId);

            // Находим нужный товар по ID
            _product = products?.FirstOrDefault(p => p.Id == productIdInt);

            if (_product != null)
            {
                // Устанавливаем BindingContext на товар на главном потоке
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BindingContext = _product;
                });
                
                _logger?.LogInformation("Product {ProductId} loaded successfully. Name: {ProductName}", productIdInt, _product.Name);
                System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] Product loaded: Id={_product.Id}, Name={_product.Name}, Price={_product.Price}");
            }
            else
            {
                _logger?.LogWarning("Product {ProductId} not found for partner {PartnerId}", productIdInt, partnerId);
                System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] Product {productIdInt} not found for partner {partnerId}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", "Товар не найден", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading product {ProductId} for partner {PartnerId}", productIdInt, partnerId);
            System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] Error loading product: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] StackTrace: {ex.StackTrace}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Ошибка", "Не удалось загрузить информацию о товаре", "OK");
            });
        }
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
            System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] Error in OnBackButtonClicked: {ex.Message}");
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
        // Возвращаемся назад в стеке навигации
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..", animate: true);
        }
    }

    private async void OnAddToCartClicked(object sender, EventArgs e)
    {
        // Защита от повторных нажатий
        if (!await _actionLock.WaitAsync(0))
            return; // Уже обрабатывается

        try
        {
            // Отключаем кнопку визуально
            if (sender is VisualElement element)
                element.IsEnabled = false;

            await OnAddToCartClickedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] Error in OnAddToCartClicked: {ex.Message}");
        }
        finally
        {
            if (sender is VisualElement element)
                element.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnAddToCartClickedAsync()
    {
        if (_product == null)
        {
            _logger?.LogWarning("Cannot add product to cart: product is null");
            return;
        }

        if (_partner == null)
        {
            _logger?.LogWarning("Cannot add product to cart: partner is null");
            await DisplayAlert("Ошибка", "Информация о партнёре не загружена", "OK");
            return;
        }

        try
        {
            await _cartService.AddToCartAsync(
                _product,
                _partner.Id,
                _partner.Name,
                _partner.LogoUrl);
            
            _logger?.LogInformation("Product {ProductId} added to cart", _product.Id);
            await DisplayAlert("Добавлено", $"Продукт {_product.Name} добавлен в корзину", "OK");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding product {ProductId} to cart", _product.Id);
            await DisplayAlert("Ошибка", "Не удалось добавить товар в корзину", "OK");
        }
    }
}

