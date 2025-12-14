using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using YessGoFront.Models;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;

namespace YessGoFront.Views;

[QueryProperty(nameof(PartnerId), "partnerId")]
public partial class PartnerDetailViewPage : ContentPage
{
    private string? partnerId;
    private readonly PartnerDetailViewModel _viewModel;

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        // Возвращаемся назад в стеке навигации
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..", animate: true);
        }
    }

    private async void OnBasketButtonClicked(object? sender, EventArgs e)
    {
        // Переход на страницу корзины
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("BasketPage", animate: true);
        }
    }

    public string? PartnerId
    {
        get => partnerId;
        set
        {
            partnerId = value;
            if (!string.IsNullOrWhiteSpace(partnerId) && _viewModel != null)
            {
                // Парсим string в int для корректной работы с базой данных
                if (int.TryParse(partnerId, out var partnerIdInt))
                {
                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewPage] Parsed partnerId: '{partnerId}' -> {partnerIdInt}");
                    _ = _viewModel.LoadPartnerCommand.ExecuteAsync(partnerIdInt);
                }
                else
                {   
                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewPage] Failed to parse partnerId: '{partnerId}'");
                }
            }
        }
    }

    public PartnerDetailViewPage()
    {
        InitializeComponent();

        // Получаем сервисы через DI
        var partnersService = MauiProgram.Services.GetRequiredService<IPartnersService>();
        var cartService = MauiProgram.Services.GetRequiredService<ICartService>();
        var logger = MauiProgram.Services.GetService<ILogger<PartnerDetailViewModel>>();

        _viewModel = new PartnerDetailViewModel(partnersService, cartService, logger);
        BindingContext = _viewModel;

        // Подписываемся на изменения коллекции продуктов
        _viewModel.Products.CollectionChanged += OnProductsCollectionChanged;
        
        // Подписываемся на изменения выбранной категории для обновления стилей кнопок
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }
    
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PartnerDetailViewModel.SelectedCategory))
        {
            UpdateCategoryButtonsStyle();
        }
    }
    
    private void UpdateCategoryButtonsStyle()
    {
        if (_viewModel == null || CategoriesScrollView == null)
            return;
            
        // Находим все кнопки категорий и обновляем их стиль
        var horizontalStackLayout = CategoriesScrollView.Content as HorizontalStackLayout;
        if (horizontalStackLayout != null)
        {
            foreach (var child in horizontalStackLayout.Children)
            {
                if (child is Button button)
                {
                    // Используем CommandParameter или Text для определения категории
                    var categoryName = button.CommandParameter as string ?? button.Text;
                    
                    if (categoryName == _viewModel.SelectedCategory)
                    {
                        button.BackgroundColor = Color.FromArgb("#0B4A3B");
                        button.TextColor = Colors.White;
                    }
                    else
                    {
                        button.BackgroundColor = Color.FromArgb("#E0E0E0");
                        button.TextColor = Color.FromArgb("#666");
                    }
                }
            }
        }
    }

    private void OnProductsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyProductsMessage();
        UpdateCategoryButtonsStyle();
    }

    private void UpdateEmptyProductsMessage()
    {
        var emptyProductsLabel = this.FindByName<Label>("EmptyProductsLabel");
        if (emptyProductsLabel != null && _viewModel != null)
        {
            emptyProductsLabel.IsVisible = _viewModel.Products.Count == 0;
        }
    }


    private async void OnAddToCartClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ProductDto product && _viewModel != null)
        {
            await _viewModel.AddToCartCommand.ExecuteAsync(product);
        }
    }
    
    private void OnCategoryButtonClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && _viewModel != null)
        {
            // Используем CommandParameter, если доступен, иначе Text
            var categoryName = button.CommandParameter as string ?? button.Text;
            
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                _viewModel.SelectCategoryCommand.Execute(categoryName);
                UpdateCategoryButtonsStyle();
            }
        }
    }

    private async void OnProductTapped(object? sender, EventArgs e)
    {
        try
        {
            ProductDto? product = null;

            // Пытаемся получить товар из BindingContext элемента
            if (sender is BindableObject bindable && bindable.BindingContext is ProductDto productFromContext)
            {
                product = productFromContext;
            }
            // Или из TapGestureRecognizer через IGestureRecognizers
            else if (sender is IGestureRecognizers gestureContainer)
            {
                var tapGesture = gestureContainer.GestureRecognizers
                    .OfType<TapGestureRecognizer>()
                    .FirstOrDefault();
                
                if (tapGesture?.CommandParameter is ProductDto productFromGesture)
                {
                    product = productFromGesture;
                }
            }
            // Или из BindingContext визуального элемента
            else if (sender is VisualElement element && element.BindingContext is ProductDto productFromElement)
            {
                product = productFromElement;
            }

            if (product != null && Shell.Current != null)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewPage] Navigating to ProductDetailPage with productId={product.Id}, partnerId={product.PartnerId}");
                await Shell.Current.GoToAsync($"ProductDetailPage?productId={product.Id}&partnerId={product.PartnerId}", animate: true);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewPage] OnProductTapped: Could not get product from sender. Sender type: {sender?.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewPage] OnProductTapped error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[PartnerDetailViewPage] StackTrace: {ex.StackTrace}");
        }
    }
}

