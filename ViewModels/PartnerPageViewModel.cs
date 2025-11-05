using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Infrastructure.Ui;
using YessGoFront.Models;
using YessGoFront.Services.Domain;

namespace YessGoFront.ViewModels;

public partial class PartnerPageViewModel : ObservableObject
{
    private readonly IPartnersService _service;
    private readonly ILogger<PartnerPageViewModel>? _logger;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<PartnerDto> Partners { get; } = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? selectedCategoryTitle;
    [ObservableProperty] private string? errorMessage;

    // 👉 ЯВНАЯ КОМАНДА (без source generator)
    public IAsyncRelayCommand<string?> LoadByCategoryAsyncCommand { get; }

    public PartnerPageViewModel(IPartnersService service, ILogger<PartnerPageViewModel>? logger = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger;

        // инициализируем команду
        LoadByCategoryAsyncCommand = new AsyncRelayCommand<string?>(LoadByCategoryAsync);

        // плитки
        Categories.Add(new Category { Title = "Все компании", Image = "cat_all" });
        Categories.Add(new Category { Title = "Еда и напитки", Image = "cat_food" });
        Categories.Add(new Category { Title = "Одежда и обувь", Image = "cat_clothes" });
        Categories.Add(new Category { Title = "Красота", Image = "cat_beauty" });
        Categories.Add(new Category { Title = "Все для дома", Image = "cat_home" });
        Categories.Add(new Category { Title = "Продукты", Image = "cat_grocery" });
        Categories.Add(new Category { Title = "Электроника", Image = "cat_electronics" });
        Categories.Add(new Category { Title = "Детское", Image = "cat_kids" });
        Categories.Add(new Category { Title = "Спорт и отдых", Image = "cat_sport" });
        Categories.Add(new Category { Title = "Кафе и рестораны", Image = "cat_coffee" });
        Categories.Add(new Category { Title = "Транспорт", Image = "cat_transport" });
        Categories.Add(new Category { Title = "Образование", Image = "cat_education" });

        // стартовый запрос
        LoadByCategoryAsyncCommand.Execute("для дома");
    }

    [RelayCommand]
    private async Task OpenPartnerAsync(PartnerDto partner)
    {
        if (partner == null)
            return;

        // Если используешь Shell навигацию:
        await Shell.Current.GoToAsync($"partnerdetails?partnerId={partner.Id}");
    }

    // ❌ БЕЗ [RelayCommand] — метод вызывается из явной команды
    private async Task LoadByCategoryAsync(string? categoryTitle)
    {
        if (string.IsNullOrWhiteSpace(categoryTitle)) return;

        SelectedCategoryTitle = categoryTitle;
        ErrorMessage = null;

        var backendKey = categoryTitle.Trim().ToLowerInvariant();
        if (backendKey == "все для дома") backendKey = "для дома";

        IsBusy = true;
        try
        {
            Partners.Clear();
            var items = await _service.GetPartnersByCategoryAsync(backendKey);
            foreach (var p in items)
                Partners.Add(p);
        }
        catch (NetworkException ex)
        {
            ErrorMessage = "Нет подключения к интернету";
            _logger?.LogError(ex, "Network error loading partners");
            var page = AppUiHelper.TryGetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlert("Ошибка", ErrorMessage, "OK");
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = "Не удалось загрузить партнёров";
            _logger?.LogError(ex, "API error loading partners");
            var page = AppUiHelper.TryGetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlert("Ошибка", ErrorMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Произошла непредвиденная ошибка";
            _logger?.LogError(ex, "Unexpected error loading partners");
            var page = AppUiHelper.TryGetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlert("Ошибка", ErrorMessage, "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
