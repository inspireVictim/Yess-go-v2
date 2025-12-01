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
    }

    private void OnProductsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyProductsMessage();
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
}

