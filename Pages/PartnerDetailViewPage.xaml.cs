using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YessGoFront.Models;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;

namespace YessGoFront.Pages;

[QueryProperty(nameof(PartnerId), "partnerId")]
public partial class PartnerDetailViewPage : ContentPage
{
    private string? partnerId;
    private readonly PartnerDetailViewModel _viewModel;

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
        var logger = MauiProgram.Services.GetService<ILogger<PartnerDetailViewModel>>();

        _viewModel = new PartnerDetailViewModel(partnersService, logger);
        BindingContext = _viewModel;

        // Подписываемся на изменения свойств ViewModel для обновления логотипа
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Подписываемся на изменения коллекции продуктов
        _viewModel.Products.CollectionChanged += OnProductsCollectionChanged;
    }

    private void OnProductsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyProductsMessage();
    }

    private void UpdateEmptyProductsMessage()
    {
        if (EmptyProductsLabel != null && _viewModel != null)
        {
            EmptyProductsLabel.IsVisible = _viewModel.Products.Count == 0;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_viewModel == null)
            return;

        // Обновляем логотип при изменении соответствующих свойств
        if (e.PropertyName == nameof(PartnerDetailViewModel.LogoImageSource) ||
            e.PropertyName == nameof(PartnerDetailViewModel.LogoText) ||
            e.PropertyName == nameof(PartnerDetailViewModel.IsLogoImageVisible) ||
            e.PropertyName == nameof(PartnerDetailViewModel.IsLogoTextVisible))
        {
            UpdateLogo();
        }
        
        // Обновляем сообщение о пустой коллекции продуктов
        if (e.PropertyName == nameof(PartnerDetailViewModel.Products))
        {
            UpdateEmptyProductsMessage();
        }
    }

    private void UpdateLogo()
    {
        if (_viewModel == null || LogoFrame == null)
            return;

        // Если есть изображение логотипа, показываем его
        if (_viewModel.IsLogoImageVisible && _viewModel.LogoImageSource != null)
        {
            var logoImage = new Image
            {
                Source = _viewModel.LogoImageSource,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            LogoFrame.Content = logoImage;
            if (LogoText != null)
            {
                LogoText.IsVisible = false;
            }
        }
        else if (_viewModel.IsLogoTextVisible && !string.IsNullOrWhiteSpace(_viewModel.LogoText))
        {
            // Показываем текст логотипа
            if (LogoText != null)
            {
                LogoText.Text = _viewModel.LogoText;
                LogoText.IsVisible = true;
            }
            // Убираем изображение, если оно было
            if (LogoFrame.Content is Image)
            {
                LogoFrame.Content = LogoText;
            }
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