using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Models;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
#if ANDROID
using Android.Util;
#endif

namespace YessGoFront.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly IPartnersService? _partnersService;
        private readonly ILogger<MapPage>? _logger;
        private readonly ObservableCollection<CategoryFilter> _categories = new();
        private readonly Dictionary<int, PartnerLocationDto> _partnerLocations = new();
        private string? _selectedCategory;
        private string? _searchQuery;
        private System.Threading.Timer? _searchDebounceTimer;
        private bool _isLoading;

        private Mapsui.UI.Maui.MapView? MapView { get; set; }

        private bool _isInitialized = false;

        public MapPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] === НАЧАЛО КОНСТРУКТОРА ===");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("[MapPage] InitializeComponent выполнен");
                
                // Получаем сервисы из DI
                _partnersService = MauiProgram.Services.GetService<IPartnersService>();
                _logger = MauiProgram.Services.GetService<ILogger<MapPage>>();
                
                System.Diagnostics.Debug.WriteLine("[MapPage] Сервисы получены");
                
                // НЕ создаём MapView в конструкторе - отложим до OnAppearing
                // Это предотвратит краш при создании страницы
                
                System.Diagnostics.Debug.WriteLine("[MapPage] === КОНЕЦ КОНСТРУКТОРА ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] КРИТИЧЕСКАЯ ОШИБКА В КОНСТРУКТОРЕ: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MapPage] Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[MapPage] Inner exception: {ex.InnerException?.Message}");
                _logger?.LogError(ex, "[MapPage] Ошибка при инициализации MapPage: {Message}", ex.Message);
                // НЕ пробрасываем исключение, чтобы страница могла загрузиться
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] === OnAppearing НАЧАЛО ===");
                
                // Инициализируем MapView только при первом появлении страницы
                if (!_isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("[MapPage] Инициализация MapView...");
                    await InitializeMapView();
                    _isInitialized = true;
                    System.Diagnostics.Debug.WriteLine("[MapPage] MapView инициализирован");
                }
                
                // Загружаем партнёров на карту
                await LoadPartnersOnMap();
                
                // Запрашиваем разрешение на геолокацию и центрируем карту
                await RequestLocationAndCenterMap();
                
                System.Diagnostics.Debug.WriteLine("[MapPage] === OnAppearing ЗАВЕРШЕНО ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] ОШИБКА В OnAppearing: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MapPage] Stack trace: {ex.StackTrace}");
                _logger?.LogError(ex, "[MapPage] Ошибка в OnAppearing: {Message}", ex.Message);
                
                // Показываем сообщение об ошибке пользователю
                await DisplayAlert("Ошибка", "Не удалось загрузить карту. Попробуйте позже.", "OK");
            }
        }

        private async Task InitializeMapView()
        {
            // Используем TaskCompletionSource для безопасной инициализации
            var tcs = new TaskCompletionSource<bool>();
            
            try
            {
#if ANDROID
                Log.Info("MapPage", "=== InitializeMapView НАЧАЛО ===");
#endif
                System.Diagnostics.Debug.WriteLine("[MapPage] InitializeMapView начат");
                
                // Проверяем, что MapContainer существует
                if (MapContainer == null)
                {
#if ANDROID
                    Log.Error("MapPage", "MapContainer == null");
#endif
                    System.Diagnostics.Debug.WriteLine("[MapPage] ОШИБКА: MapContainer == null");
                    throw new InvalidOperationException("MapContainer не найден в XAML");
                }
                
                // Создаём MapView в главном потоке UI (обязательно!)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
#if ANDROID
                        Log.Info("MapPage", "Создание MapView в главном потоке...");
#endif
                        System.Diagnostics.Debug.WriteLine("[MapPage] Создание MapView в главном потоке...");
                        
                        // Создаём MapView программно
                        MapView = new Mapsui.UI.Maui.MapView
                        {
                            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E0E0E0"),
                            VerticalOptions = LayoutOptions.FillAndExpand,
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        };
                        
#if ANDROID
                        Log.Info("MapPage", "MapView создан, добавление в контейнер...");
#endif
                        System.Diagnostics.Debug.WriteLine("[MapPage] MapView создан, добавление в контейнер...");
                        
                        // Добавляем MapView в контейнер
                        MapContainer.Children.Add(MapView);
                        
#if ANDROID
                        Log.Info("MapPage", "MapView добавлен в контейнер");
#endif
                        System.Diagnostics.Debug.WriteLine("[MapPage] MapView добавлен в контейнер");
                        
                        // Небольшая задержка перед инициализацией карты
                        Task.Delay(200).ContinueWith(_ =>
                        {
                            try
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    try
                                    {
#if ANDROID
                                        Log.Info("MapPage", "Инициализация карты...");
#endif
                                        System.Diagnostics.Debug.WriteLine("[MapPage] Инициализация карты...");
                                        InitializeMap();
#if ANDROID
                                        Log.Info("MapPage", "Карта инициализирована");
#endif
                                        System.Diagnostics.Debug.WriteLine("[MapPage] Карта инициализирована");
                                        
                                        // Инициализируем категории (только визуально, без функционала)
                                        InitializeCategories();
                                        
                                        tcs.SetResult(true);
                                    }
                                    catch (Exception ex)
                                    {
#if ANDROID
                                        Log.Error("MapPage", $"ОШИБКА при инициализации карты: {ex.Message}");
#endif
                                        System.Diagnostics.Debug.WriteLine($"[MapPage] ОШИБКА при инициализации карты: {ex.Message}");
                                        tcs.SetException(ex);
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
#if ANDROID
                                Log.Error("MapPage", $"ОШИБКА в задержке: {ex.Message}");
#endif
                                tcs.SetException(ex);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
#if ANDROID
                        Log.Error("MapPage", $"ОШИБКА при создании MapView: {ex.Message}\n{ex.StackTrace}");
#endif
                        System.Diagnostics.Debug.WriteLine($"[MapPage] ОШИБКА при создании MapView: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[MapPage] Stack trace: {ex.StackTrace}");
                        tcs.SetException(ex);
                    }
                });
                
                // Ждём завершения инициализации (с таймаутом)
                await Task.WhenAny(tcs.Task, Task.Delay(5000));
                
                if (!tcs.Task.IsCompleted)
                {
#if ANDROID
                    Log.Warn("MapPage", "Таймаут инициализации MapView");
#endif
                    throw new TimeoutException("Таймаут инициализации MapView");
                }
                
                if (tcs.Task.IsFaulted)
                {
                    throw tcs.Task.Exception?.InnerException ?? tcs.Task.Exception ?? new Exception("Неизвестная ошибка");
                }
                
#if ANDROID
                Log.Info("MapPage", "=== InitializeMapView ЗАВЕРШЕНО УСПЕШНО ===");
#endif
                System.Diagnostics.Debug.WriteLine("[MapPage] InitializeMapView завершён успешно");
            }
            catch (Exception ex)
            {
#if ANDROID
                Log.Error("MapPage", $"КРИТИЧЕСКАЯ ОШИБКА В InitializeMapView: {ex.Message}\n{ex.StackTrace}");
#endif
                System.Diagnostics.Debug.WriteLine($"[MapPage] ОШИБКА В InitializeMapView: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MapPage] Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[MapPage] Inner exception: {ex.InnerException?.Message}");
                _logger?.LogError(ex, "[MapPage] Ошибка в InitializeMapView: {Message}", ex.Message);
                throw;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _searchDebounceTimer?.Dispose();
        }

        private void InitializeMap()
        {
            if (MapView == null)
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] MapView is null, cannot initialize map");
                _logger?.LogError("[MapPage] MapView is null, cannot initialize map");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] InitializeMap начат");
                
                // Создаём карту (используем Mapsui.Map явно)
                var map = new Mapsui.Map();
                System.Diagnostics.Debug.WriteLine("[MapPage] Mapsui.Map создан");

                // Добавляем слой OpenStreetMap с уникальным User-Agent
                // OpenStreetMap требует уникальный User-Agent для избежания блокировок
                System.Diagnostics.Debug.WriteLine("[MapPage] Создание слоя OpenStreetMap...");
                var userAgent = "YessGoApp/1.0 (com.yessgo.front)";
                var osmLayer = OpenStreetMap.CreateTileLayer(userAgent);
                map.Layers.Add(osmLayer);
                System.Diagnostics.Debug.WriteLine("[MapPage] Слой OpenStreetMap добавлен с User-Agent");

                // Устанавливаем карту в MapView
                System.Diagnostics.Debug.WriteLine("[MapPage] Установка карты в MapView...");
                MapView.Map = map;
                System.Diagnostics.Debug.WriteLine("[MapPage] Карта установлена в MapView");

                // Подписываемся на события карты
                MapView.Info += OnMapInfo;
                System.Diagnostics.Debug.WriteLine("[MapPage] Подписка на события карты выполнена");

                // Центрируем на Бишкеке по умолчанию
                double bishkekLon = 74.5698;
                double bishkekLat = 42.8746;
                Mapsui.MPoint bishkek = new Mapsui.MPoint(bishkekLon, bishkekLat); // longitude, latitude
                (double x, double y) mercatorCoords = SphericalMercator.FromLonLat(bishkek.X, bishkek.Y);
                Mapsui.MPoint sphericalMercatorCoordinate = new Mapsui.MPoint(mercatorCoords.x, mercatorCoords.y);
                
                // Устанавливаем начальный зум (zoom level 13)
                // Resolution для zoom level 13 примерно равен 19.1 метра на пиксель
                var resolution = 19.1;
                if (MapView?.Map != null)
                {
                    MapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, resolution);
                }

                _logger?.LogInformation("Map initialized with OpenStreetMap");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing map: {Message}", ex.Message);
                System.Diagnostics.Debug.WriteLine($"[MapPage] Error initializing map: {ex}");
                ShowError("Не удалось инициализировать карту");
            }
        }

        private void InitializeCategories()
        {
            // Категории для фильтрации
            var categoryList = new List<CategoryFilter>
            {
                new CategoryFilter { Name = "Все", IsSelected = true },
                new CategoryFilter { Name = "Красота", IsSelected = false },
                new CategoryFilter { Name = "Еда и напитки", IsSelected = false },
                new CategoryFilter { Name = "Продукты", IsSelected = false },
                new CategoryFilter { Name = "Одежда", IsSelected = false },
                new CategoryFilter { Name = "Электроника", IsSelected = false },
                new CategoryFilter { Name = "Спорт", IsSelected = false }
            };

            foreach (var category in categoryList)
            {
                _categories.Add(category);
                
                // Создаём кнопку категории
                var button = new Button
                {
                    Text = category.Name,
                    BackgroundColor = category.IsSelected ? Microsoft.Maui.Graphics.Color.FromArgb("#0F6B53") : Microsoft.Maui.Graphics.Color.FromArgb("#E5E7EB"),
                    TextColor = category.IsSelected ? Microsoft.Maui.Graphics.Colors.White : Microsoft.Maui.Graphics.Color.FromArgb("#6B7280"),
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    CornerRadius = 20,
                    Padding = new Thickness(20, 10),
                    Margin = new Thickness(0, 0, 0, 0)
                };

                button.Clicked += (s, e) => OnCategoryClicked(category);
                
                CategoriesContainer.Children.Add(button);
            }
        }

        private async void OnCategoryClicked(CategoryFilter category)
        {
            // Снимаем выделение с других категорий
            foreach (var cat in _categories)
            {
                cat.IsSelected = cat.Name == category.Name;
            }

            // Обновляем визуальное состояние кнопок
            UpdateCategoryButtons();

            // Фильтруем партнёров на карте
            _selectedCategory = category.IsSelected && category.Name != "Все" ? category.Name : null;
            await LoadPartnersOnMap();
        }

        private void UpdateCategoryButtons()
        {
            int index = 0;
            foreach (var category in _categories)
            {
                if (index < CategoriesContainer.Children.Count)
                {
                    if (CategoriesContainer.Children[index] is Button button)
                    {
                        button.BackgroundColor = category.IsSelected 
                            ? Microsoft.Maui.Graphics.Color.FromArgb("#0F6B53") 
                            : Microsoft.Maui.Graphics.Color.FromArgb("#E5E7EB");
                        button.TextColor = category.IsSelected 
                            ? Microsoft.Maui.Graphics.Colors.White 
                            : Microsoft.Maui.Graphics.Color.FromArgb("#6B7280");
                        button.Opacity = category.IsSelected ? 1.0 : 0.8;
                    }
                }
                index++;
            }
        }

        private async Task LoadPartnersOnMap()
        {
            if (_isLoading) return;
            
            _isLoading = true;
            ShowLoading(true);

            try
            {
                // Получаем локации партнёров через API
                var httpClientFactory = MauiProgram.Services.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                
                var endpoint = ApiEndpoints.PartnersEndpoints.Locations;
                
                // Добавляем фильтры, если есть
                if (!string.IsNullOrWhiteSpace(_selectedCategory) || !string.IsNullOrWhiteSpace(_searchQuery))
                {
                    var queryParams = new List<string>();
                    if (!string.IsNullOrWhiteSpace(_selectedCategory))
                    {
                        queryParams.Add($"category={Uri.EscapeDataString(_selectedCategory)}");
                    }
                    if (!string.IsNullOrWhiteSpace(_searchQuery))
                    {
                        queryParams.Add($"query={Uri.EscapeDataString(_searchQuery)}");
                    }
                    if (queryParams.Count > 0)
                    {
                        endpoint += "?" + string.Join("&", queryParams);
                    }
                }
                
                var response = await httpClient.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning($"Failed to get partner locations: {response.StatusCode}");
                    ShowError("Не удалось загрузить партнёров");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var locations = JsonSerializer.Deserialize<List<PartnerLocationDto>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (locations == null)
                {
                    _logger?.LogInformation("No partner locations found");
                    return;
                }

                // Сохраняем локации для обработки кликов
                _partnerLocations.Clear();
                foreach (var location in locations)
                {
                    _partnerLocations[location.PartnerId] = location;
                }

                // Фильтруем по поисковому запросу, если есть
                if (!string.IsNullOrWhiteSpace(_searchQuery))
                {
                    var searchLower = _searchQuery.ToLowerInvariant();
                    locations = locations.Where(l => 
                        l.PartnerName.ToLowerInvariant().Contains(searchLower) ||
                        (l.Address?.ToLowerInvariant().Contains(searchLower) == true)
                    ).ToList();
                }

                // Проверяем, что MapView инициализирован
                if (MapView?.Map == null)
                {
                    _logger?.LogError("[MapPage] MapView or Map is null, cannot load partners");
                    return;
                }

                // Удаляем старый слой партнёров, если есть
                var existingLayer = MapView.Map.Layers.FirstOrDefault(l => l.Name == "PartnersLayer");
                if (existingLayer != null)
                {
                    MapView.Map.Layers.Remove(existingLayer);
                }

                // Создаём новый слой для партнёров
                var features = new List<IFeature>();

                foreach (var location in locations)
                {
                    // Используем координаты локации
                    if (location.Latitude.HasValue && location.Longitude.HasValue)
                    {
                        double lon = location.Longitude.Value;
                        double lat = location.Latitude.Value;
                        Mapsui.MPoint point = new Mapsui.MPoint(lon, lat);
                        (double x, double y) mercatorCoords = SphericalMercator.FromLonLat(point.X, point.Y);
                        Mapsui.MPoint sphericalMercatorCoordinate = new Mapsui.MPoint(mercatorCoords.x, mercatorCoords.y);
                        
                        // Создаём PointFeature с явным указанием типа координаты
                        Mapsui.Layers.PointFeature feature = new Mapsui.Layers.PointFeature(sphericalMercatorCoordinate);
                        feature["Name"] = location.PartnerName;
                        feature["PartnerId"] = location.PartnerId;
                        feature["Address"] = location.Address ?? string.Empty;
                        feature["LocationId"] = location.Id;

                        // Стиль маркера (зелёный круг с белой обводкой)
                        // Используем Mapsui.Styles.Color и Mapsui.Styles.Brush
                        byte r = 15;
                        byte g = 107;
                        byte b = 83;
                        Mapsui.Styles.Color fillColor = new Mapsui.Styles.Color(r, g, b); // #0F6B53
                        byte whiteR = 255;
                        byte whiteG = 255;
                        byte whiteB = 255;
                        Mapsui.Styles.Color outlineColor = new Mapsui.Styles.Color(whiteR, whiteG, whiteB);
                        Mapsui.Styles.Brush fillBrush = new Mapsui.Styles.Brush(fillColor);
                        Mapsui.Styles.Pen outlinePen = new Mapsui.Styles.Pen(outlineColor, 2);
                        
                        feature.Styles.Add(new SymbolStyle
                        {
                            SymbolType = SymbolType.Ellipse,
                            Fill = fillBrush,
                            Outline = outlinePen,
                            SymbolScale = 1.2f,
                            Opacity = 0.9f
                        });

                        features.Add(feature);
                    }
                }

                // Создаём MemoryLayer с MemoryProvider
                var memoryProvider = new MemoryProvider(features);
                var partnersLayer = new MemoryLayer("PartnersLayer");
                
                // В Mapsui 4.1.9 MemoryLayer может использовать DataSource или Features
                // Пробуем установить DataSource напрямую
                try
                {
                    // Используем dynamic для обхода проверки типов на этапе компиляции
                    dynamic dynamicLayer = partnersLayer;
                    dynamicLayer.DataSource = memoryProvider;
                }
                catch
                {
                    // Если DataSource недоступен, логируем предупреждение
                    _logger?.LogWarning("Could not set DataSource on MemoryLayer, features may not display");
                }
                
                MapView.Map.Layers.Add(partnersLayer);

                _logger?.LogInformation($"Loaded {features.Count} partner locations on map");
            }
            catch (NetworkException ex)
            {
                _logger?.LogError(ex, "Network error loading partners");
                ShowError("Нет подключения к интернету");
            }
            catch (ApiException ex)
            {
                _logger?.LogError(ex, "API error loading partners");
                ShowError("Не удалось загрузить партнёров");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading partners on map: {Message}", ex.Message);
                System.Diagnostics.Debug.WriteLine($"[MapPage] Error loading partners: {ex}");
                ShowError("Произошла ошибка при загрузке данных");
            }
            finally
            {
                _isLoading = false;
                ShowLoading(false);
            }
        }

        private async void OnMapInfo(object? sender, MapInfoEventArgs e)
        {
            try
            {
                if (e.MapInfo?.Feature == null) return;

                var feature = e.MapInfo.Feature;
                
                // Проверяем наличие поля PartnerId
                if (feature == null)
                    return;

                // Проверяем наличие поля через индексатор
                if (!feature.Fields.Contains("PartnerId"))
                    return;

                var partnerIdValue = feature["PartnerId"];
                if (partnerIdValue == null)
                    return;

                var partnerId = Convert.ToInt32(partnerIdValue);
                if (!_partnerLocations.TryGetValue(partnerId, out var location)) return;

                // Показываем диалог с информацией о партнёре
                var result = await DisplayActionSheet(
                    location.PartnerName,
                    "Отмена",
                    null,
                    new[] { "Открыть страницу партнёра", "Показать адрес" }
                );

                if (result == "Открыть страницу партнёра")
                {
                    await Shell.Current.GoToAsync($"///partnerdetails?partnerId={partnerId}");
                }
                else if (result == "Показать адрес" && !string.IsNullOrWhiteSpace(location.Address))
                {
                    await DisplayAlert("Адрес", location.Address, "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling map info: {Message}", ex.Message);
            }
        }

        private async Task RequestLocationAndCenterMap()
        {
            try
            {
                // Проверяем разрешение на геолокацию
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status == PermissionStatus.Granted)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    var location = await Geolocation.Default.GetLocationAsync(request);

                    if (location != null)
                    {
                        double userLon = location.Longitude;
                        double userLat = location.Latitude;
                        Mapsui.MPoint point = new Mapsui.MPoint(userLon, userLat);
                        (double x, double y) mercatorCoords = SphericalMercator.FromLonLat(point.X, point.Y);
                        Mapsui.MPoint sphericalMercatorCoordinate = new Mapsui.MPoint(mercatorCoords.x, mercatorCoords.y);
                        // Resolution для zoom level 14 примерно равен 9.5 метра на пиксель
                        var resolution = 9.5;
                        if (MapView?.Map != null)
                        {
                            MapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, resolution);
                            _logger?.LogInformation($"Centered map on user location: {location.Latitude}, {location.Longitude}");
                        }
                    }
                }
            }
            catch (PermissionException)
            {
                _logger?.LogWarning("Location permission denied");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not get user location, using default (Bishkek)");
                // Используем координаты Бишкека по умолчанию
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue;
            
            // Показываем/скрываем кнопку очистки
            ClearSearchButton.IsVisible = !string.IsNullOrWhiteSpace(searchText);

            // Debounce поиска (500ms)
            _searchDebounceTimer?.Dispose();
            _searchDebounceTimer = new System.Threading.Timer(async _ =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _searchQuery = searchText;
                    await LoadPartnersOnMap();
                });
            }, null, 500, Timeout.Infinite);
        }

        private async void OnSearchCompleted(object? sender, EventArgs e)
        {
            _searchQuery = SearchEntry.Text;
            await LoadPartnersOnMap();
        }

        private async void OnClearSearchClicked(object? sender, EventArgs e)
        {
            SearchEntry.Text = string.Empty;
            _searchQuery = null;
            ClearSearchButton.IsVisible = false;
            await LoadPartnersOnMap();
        }

        private async void OnBackTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("///main/partner");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating back: {Message}", ex.Message);
            }
        }

        private void ShowLoading(bool show)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsRunning = show;
                LoadingIndicator.IsVisible = show;
            });
        }

        private void ShowError(string message)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Ошибка", message, "OK");
            });
        }
    }

    public class CategoryFilter
    {
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class PartnerLocationDto
    {
        public int Id { get; set; }
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WorkingHours { get; set; }
        public double MaxDiscountPercent { get; set; }
    }
}
