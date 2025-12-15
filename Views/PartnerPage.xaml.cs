using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;
using YessGoFront.Models;

namespace YessGoFront.Views
{
    public partial class PartnerPage : ContentPage
    {
        private readonly ILogger<PartnerPage>? _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public ObservableCollection<CategoryItem> Categories { get; set; }
        private string _searchQuery = string.Empty;
        private bool _categoriesLoaded = false;
        private bool _isAppearing = false;
        private readonly SemaphoreSlim _actionLock = new(1, 1);

        public PartnerPage()
        {
            InitializeComponent();
            _httpClientFactory = MauiProgram.Services.GetRequiredService<IHttpClientFactory>();
            _logger = MauiProgram.Services.GetService<ILogger<PartnerPage>>();
            Categories = new ObservableCollection<CategoryItem>();
            CategoriesCollection.ItemsSource = Categories;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isAppearing)
                return;

            _isAppearing = true;
            try
            {
                await OnAppearingAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerPage] Error in OnAppearing: {ex.Message}");
            }
            finally
            {
                _isAppearing = false;
            }
        }

        protected virtual async Task OnAppearingAsync()
        {
            // Показываем дефолтные категории сразу
            if (!_categoriesLoaded)
            {
                LoadDefaultCategories();
            }

            // Загружаем категории из API в фоне
            _ = LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var endpoint = ApiEndpoints.PartnersEndpoints.Categories;
                
                var response = await httpClient.GetAsync(endpoint, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning($"Failed to load categories: {response.StatusCode}");
                    // Используем дефолтные категории при ошибке
                    LoadDefaultCategories();
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var categories = JsonSerializer.Deserialize<List<CategoryDto>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (categories == null || categories.Count == 0)
                {
                    LoadDefaultCategories();
                    return;
                }

                // Добавляем категорию "Все компании" в начало
                Categories.Clear();
                Categories.Add(new CategoryItem("Все компании", "cat_all.png", null));

                // Добавляем категории из API
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Categories.Clear();
                    Categories.Add(new CategoryItem("Все компании", "cat_all.png", null));
                    foreach (var category in categories.OrderBy(c => c.Name))
                    {
                        var icon = GetCategoryIcon(category.Slug ?? category.Name.ToLower());
                        Categories.Add(new CategoryItem(category.Name, icon, category.Slug));
                    }
                    _categoriesLoaded = true;
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading categories");
                // Оставляем дефолтные категории
            }
        }

        private void LoadDefaultCategories()
        {
            Categories.Clear();
            Categories.Add(new CategoryItem("Все компании", "cat_all.png", null));
            Categories.Add(new CategoryItem("Еда и напитки", "cat_food.png", "food-drinks"));
            Categories.Add(new CategoryItem("Одежда и обувь", "cat_clothes.png", "clothing-shoes"));
            Categories.Add(new CategoryItem("Красота", "cat_beauty.png", "beauty"));
            Categories.Add(new CategoryItem("Все для дома", "cat_home.png", "home"));
            Categories.Add(new CategoryItem("Продукты", "cat_electronics.png", "groceries"));
            Categories.Add(new CategoryItem("Электроника", "cat_electronics.png", "electronics"));
            Categories.Add(new CategoryItem("Детское", "cat_kids.png", "kids"));
            Categories.Add(new CategoryItem("Спорт и отдых", "cat_sport.png", "sport"));
            Categories.Add(new CategoryItem("Кафе и рестораны", "category_cafe.png", "cafe-restaurant"));
            Categories.Add(new CategoryItem("Транспорт", "category_transport.png", "transport"));
            Categories.Add(new CategoryItem("Образование", "category_education.png", "education"));
        }

        private string GetCategoryIcon(string slug)
        {
            return slug switch
            {
                "food-drinks" or "cafe-restaurant" => "cat_food.png",
                "clothing-shoes" => "cat_clothes.png",
                "beauty" => "cat_beauty.png",
                "home" => "cat_home.png",
                "groceries" => "cat_electronics.png",
                "electronics" => "cat_electronics.png",
                "kids" => "cat_kids.png",
                "sport" => "cat_sport.png",
                "transport" => "category_transport.png",
                "education" => "category_education.png",
                _ => "cat_all.png"
            };
        }


        private async void OnBackTapped(object? sender, EventArgs e)
        {
            if (!await _actionLock.WaitAsync(0))
                return;

            try
            {
                await OnBackTappedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerPage] Error in OnBackTapped: {ex.Message}");
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private async Task OnBackTappedAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private async void OnMapButtonClicked(object sender, EventArgs e)
        {
            if (!await _actionLock.WaitAsync(0))
                return;

            try
            {
                if (sender is Button btn)
                    btn.IsEnabled = false;

                await OnMapButtonClickedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerPage] Error in OnMapButtonClicked: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось перейти: {ex.Message}", "ОК");
            }
            finally
            {
                if (sender is Button btn)
                    btn.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnMapButtonClickedAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("///MapPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось перейти: {ex.Message}", "ОК");
            }
        }

        private async void Category_Tapped(object sender, TappedEventArgs e)
        {
            if (!await _actionLock.WaitAsync(0))
                return;

            try
            {
                await Category_TappedAsync(sender, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerPage] Error in Category_Tapped: {ex.Message}");
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private async Task Category_TappedAsync(object sender, TappedEventArgs e)
        {
            try
            {
                CategoryItem? category = null;
                
                // Пробуем получить из BindingContext
                if (sender is BindableObject bindable && bindable.BindingContext is CategoryItem cat)
                {
                    category = cat;
                }
                // Или из параметра события
                else if (e.Parameter is CategoryItem catParam)
                {
                    category = catParam;
                }
                
                if (category != null)
                {
                    // Передаём параметры через Query параметры
                    var categorySlug = category.Slug ?? (category.Name == "Все компании" ? "all" : string.Empty);
                    var categoryName = Uri.EscapeDataString(category.Name);
                    var searchQuery = Uri.EscapeDataString(_searchQuery);
                    
                    // Используем относительный путь (БЕЗ ///) для сохранения стека навигации
                    var route = $"PartnersListPage?categorySlug={Uri.EscapeDataString(categorySlug)}&categoryName={categoryName}&searchQuery={searchQuery}";
                    await Shell.Current.GoToAsync(route);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось перейти: {ex.Message}", "ОК");
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            _searchQuery = e.NewTextValue ?? string.Empty;
        }

        // адаптация под размер
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (CategoriesCollection.ItemsLayout is GridItemsLayout gridLayout)
            {
                if (width < 400)
                    gridLayout.Span = 2;
                else if (width < 700)
                    gridLayout.Span = 3;
                else
                    gridLayout.Span = 4;
            }
        }
    }

    public class CategoryItem
    {
        public string Name { get; set; }
        public string Image { get; set; }
        public string? Slug { get; set; }

        public CategoryItem(string name, string image, string? slug = null)
        {
            Name = name;
            Image = image;
            Slug = slug;
        }
    }
}
