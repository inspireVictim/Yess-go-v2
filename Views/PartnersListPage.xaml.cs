using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;
using YessGoFront.Models;

namespace YessGoFront.Views
{
    [QueryProperty(nameof(CategorySlug), "categorySlug")]
    [QueryProperty(nameof(CategoryName), "categoryName")]
    [QueryProperty(nameof(SearchQuery), "searchQuery")]
    public partial class PartnersListPage : ContentPage
    {
        private readonly ILogger<PartnersListPage>? _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public ObservableCollection<PartnerListItem> Partners { get; } = new();
        private string _categorySlug = string.Empty;
        private string _categoryName = string.Empty;
        private string _searchQuery = string.Empty;
        private List<PartnerDto> _allPartners = new();

        public string CategorySlug { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SearchQuery { get; set; } = string.Empty;

        public PartnersListPage()
        {
            InitializeComponent();
            _httpClientFactory = MauiProgram.Services.GetRequiredService<IHttpClientFactory>();
            _logger = MauiProgram.Services.GetService<ILogger<PartnersListPage>>();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Получаем параметры навигации через QueryProperty
            _categorySlug = CategorySlug ?? string.Empty;
            _categoryName = CategoryName ?? string.Empty;
            _searchQuery = SearchQuery ?? string.Empty;
            
            // Устанавливаем поисковый запрос, если есть
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                SearchEntry.Text = _searchQuery;
            }

            // Загружаем партнёров
            await LoadPartnersAsync();
        }

        private async Task LoadPartnersAsync()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                string endpoint;

                // Если выбрана категория "Все компании" или slug пустой, загружаем всех партнёров
                if (string.IsNullOrEmpty(_categorySlug) || _categorySlug == "all")
                {
                    endpoint = ApiEndpoints.PartnersEndpoints.List;
                }
                else
                {
                    endpoint = ApiEndpoints.PartnersEndpoints.ByCategory(_categorySlug);
                }

                var response = await httpClient.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning($"Failed to load partners: {response.StatusCode}");
                    ShowError("Не удалось загрузить партнёров");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                _allPartners = JsonSerializer.Deserialize<List<PartnerDto>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PartnerDto>();

                // Применяем фильтр поиска
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading partners");
                ShowError("Произошла ошибка при загрузке данных");
            }
        }

        private void ApplyFilters()
        {
            Partners.Clear();

            var filtered = _allPartners.AsEnumerable();

            // Фильтр по поисковому запросу
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                var searchLower = _searchQuery.ToLowerInvariant();
                filtered = filtered.Where(p => 
                    (p.Name?.ToLowerInvariant().Contains(searchLower) == true) ||
                    (p.Description?.ToLowerInvariant().Contains(searchLower) == true)
                );
            }

            // Преобразуем в PartnerListItem
            foreach (var partner in filtered)
            {
                var categoryName = partner.Categories?.FirstOrDefault()?.Name ?? 
                                  partner.Category ?? 
                                  "Без категории";
                
                var cashbackText = partner.CashbackPercent > 0 
                    ? $"до {partner.CashbackPercent}%" 
                    : "—";

                Partners.Add(new PartnerListItem
                {
                    Id = partner.Id,
                    Logo = partner.LogoUrl ?? "partner_default.png",
                    Name = partner.Name ?? "Без названия",
                    Category = categoryName,
                    CashbackText = cashbackText
                });
            }

            // Отмечаем последний элемент
            if (Partners.Count > 0)
            {
                Partners[Partners.Count - 1].IsLast = true;
            }
        }

        private void ShowError(string message)
        {
            DisplayAlert("Ошибка", message, "ОК");
        }

        private async void OnBackTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(PartnerPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            _searchQuery = e.NewTextValue ?? string.Empty;
            ApplyFilters();
        }

        private async void OnPartnerTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                if (sender is BindableObject bindable && bindable.BindingContext is PartnerListItem item)
                {
                    await Shell.Current.GoToAsync($"///PartnerDetailPage?partnerId={item.Id}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось открыть партнёра: {ex.Message}", "ОК");
            }
        }
    }

    // модель для строки списка
    public class PartnerListItem
    {
        public int Id { get; set; }
        public string Logo { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string CashbackText { get; set; } = "";
        public bool IsLast { get; set; } = false;
    }
}
