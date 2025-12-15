using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using YessGoFront.Services.Domain;
using YessGoFront.Services.Api;
using YessGoFront.ViewModels;

namespace YessGoFront.Views;

public partial class BasketPage : ContentPage
{
    private readonly BasketViewModel _viewModel;
    private readonly IPaymentApiService? _paymentApiService;
    private readonly ILogger<BasketPage>? _logger;
    private readonly HttpClient _httpClient;
    private bool _isProcessingPayment = false;
    private readonly Dictionary<int, MapView> _mapViews = new(); // Хранилище MapView по PartnerId
    private readonly Dictionary<int, System.Threading.Timer> _geocodingTimers = new(); // Таймеры для debounce reverse geocoding

    public BasketPage()
    {
        InitializeComponent();

        // Получаем сервисы через DI
        var cartService = MauiProgram.Services.GetRequiredService<ICartService>();
        var partnersService = MauiProgram.Services.GetRequiredService<IPartnersService>();
        var logger = MauiProgram.Services.GetService<ILogger<BasketViewModel>>();

        _viewModel = new BasketViewModel(cartService, partnersService, logger);
        BindingContext = _viewModel;

        // Получаем сервис платежей
        try
        {
            _paymentApiService = MauiProgram.Services?.GetService<IPaymentApiService>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BasketPage] Error getting PaymentApiService: {ex.Message}");
        }

        // Получаем логгер
        _logger = MauiProgram.Services?.GetService<ILogger<BasketPage>>();
        
        // Создаём HttpClient для reverse geocoding
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "YessGoApp/1.0 (com.yessgo.front)");

        // Подписываемся на изменения коллекции партнёров для инициализации карт
        _viewModel.PartnerGroups.CollectionChanged += OnPartnerGroupsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Загружаем данные корзины при появлении страницы
        if (_viewModel != null)
        {
            await _viewModel.LoadCartCommand.ExecuteAsync(null);
            // Инициализируем карты после загрузки
            await Task.Delay(1000); // Задержка для рендеринга UI
            InitializeMaps();
        }
    }

    private void OnPartnerGroupsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Инициализируем карты при изменении коллекции
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add || 
            e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(1000); // Задержка для рендеринга UI
                InitializeMaps();
            });
        }
    }

    public async void OnBackButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null)
            {
                return;
            }

            // Сначала пытаемся использовать Navigation.PopAsync (более надежный способ)
            if (Shell.Current.Navigation != null && Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync(animated: true);
                return;
            }

            // Если Navigation.PopAsync не сработал, используем Shell навигацию
            await Shell.Current.GoToAsync("..", animate: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BasketPage] Navigation error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BasketPage] StackTrace: {ex.StackTrace}");
            
            // Fallback: попытка вернуться назад через Shell навигацию
            try
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("..", animate: true);
                }
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"[BasketPage] Fallback navigation error: {fallbackEx.Message}");
            }
        }
    }

    private async void OnTopUpButtonClicked(object sender, EventArgs e)
    {
        if (_isProcessingPayment || _paymentApiService == null)
        {
            return;
        }

        try
        {
            _isProcessingPayment = true;

            // Используем минимальную сумму пополнения (100 сом) или можно сделать выбор суммы
            decimal amount = 100m;

            Debug.WriteLine($"[BasketPage] Starting payment for amount: {amount} KGS");

            // Используем таймаут для создания платежа (15 секунд)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            // Вызываем backend для создания платежа
            var response = await _paymentApiService.CreatePaymentAsync(amount, cts.Token);

            _isProcessingPayment = false;

            // Проверяем, что получили paymentUrl
            if (string.IsNullOrWhiteSpace(response?.PaymentUrl))
            {
                Debug.WriteLine("[BasketPage] PaymentUrl is empty in response");
                await DisplayAlert("Ошибка", "Не удалось получить ссылку на оплату. Попробуйте снова.", "OK");
                return;
            }

            Debug.WriteLine($"[BasketPage] Payment created, opening WebView with URL: {response.PaymentUrl}");

            // Открываем FinikPaymentPage с paymentUrl и redirectUrl
            var paymentUrlEncoded = Uri.EscapeDataString(response.PaymentUrl);
            var redirectUrlEncoded = !string.IsNullOrWhiteSpace(response.RedirectUrl) 
                ? Uri.EscapeDataString(response.RedirectUrl) 
                : string.Empty;
            
            // Используем полное имя маршрута для навигации
            var navigationPath = !string.IsNullOrWhiteSpace(redirectUrlEncoded)
                ? $"FinikPaymentPage?paymentUrl={paymentUrlEncoded}&redirectUrl={redirectUrlEncoded}"
                : $"FinikPaymentPage?paymentUrl={paymentUrlEncoded}";
            
            Debug.WriteLine($"[BasketPage] Navigating to: {navigationPath}");
            await Shell.Current.GoToAsync(navigationPath, animate: true);
        }
        catch (OperationCanceledException)
        {
            _isProcessingPayment = false;
            
            Debug.WriteLine("[BasketPage] Payment creation timed out");
            await DisplayAlert("Ошибка", "Операция создания платежа заняла слишком много времени. Проверьте подключение к интернету и попробуйте снова.", "OK");
        }
        catch (Exception ex)
        {
            _isProcessingPayment = false;
            
            Debug.WriteLine($"[BasketPage] Error processing payment: {ex.Message}");
            Debug.WriteLine($"[BasketPage] Exception type: {ex.GetType().Name}");
            Debug.WriteLine($"[BasketPage] StackTrace: {ex.StackTrace}");
            
            // Показываем общее сообщение об ошибке
            var userMessage = "Произошла ошибка при создании платежа. Пожалуйста, проверьте подключение к интернету и попробуйте снова.";
            await DisplayAlert("Ошибка", userMessage, "OK");
        }
    }

    private void InitializeMaps()
    {
        try
        {
            if (_viewModel == null)
            {
                _logger?.LogWarning("ViewModel is null, cannot initialize maps");
                return;
            }

            if (_mapViews == null)
            {
                _logger?.LogWarning("MapViews dictionary is null");
                return;
            }

            // Находим CollectionView
            var collectionView = this.FindByName<CollectionView>("PartnerGroupsCollectionView");
            if (collectionView == null)
            {
                collectionView = FindVisualElement<CollectionView>(this);
            }

            if (collectionView == null)
            {
                _logger?.LogWarning("CollectionView not found for map initialization");
                return;
            }

            // Инициализируем карты для каждой группы партнёров
            // Ищем все Grid контейнеры карт через визуальное дерево
            if (_viewModel.PartnerGroups != null)
            {
                foreach (var group in _viewModel.PartnerGroups)
                {
                    if (group != null && !_mapViews.ContainsKey(group.PartnerId))
                    {
                        FindAndInitializeMapForPartner(collectionView, group);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing maps");
            Debug.WriteLine($"[BasketPage] Error initializing maps: {ex.Message}");
        }
    }

    private T? FindVisualElement<T>(VisualElement parent) where T : VisualElement
    {
        if (parent is T result)
            return result;

        if (parent is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is VisualElement visualChild)
                {
                    var found = FindVisualElement<T>(visualChild);
                    if (found != null)
                        return found;
                }
            }
        }

        return null;
    }

    private void FindAndInitializeMapForPartner(VisualElement parent, PartnerCartGroup group)
    {
        try
        {
            if (group == null || _mapViews == null)
            {
                _logger?.LogWarning("Group or MapViews is null");
                return;
            }

            // Ищем Grid с именем MapContainerGrid, у которого BindingContext равен нашей группе
            var grids = FindAllVisualElements<Grid>(parent);
            foreach (var grid in grids)
            {
                if (grid?.BindingContext is PartnerCartGroup gridGroup && 
                    gridGroup.PartnerId == group.PartnerId)
                {
                    // Проверяем, есть ли внутри Grid с именем MapContainer
                    var mapContainer = grid.Children?.OfType<Grid>().FirstOrDefault();
                    if (mapContainer != null && !_mapViews.ContainsKey(group.PartnerId))
                    {
                        InitializeMapView(mapContainer, group);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error finding map container for partner {PartnerId}", group?.PartnerId ?? 0);
        }
    }

    private List<T> FindAllVisualElements<T>(VisualElement parent) where T : VisualElement
    {
        var results = new List<T>();
        FindAllVisualElementsRecursive(parent, results);
        return results;
    }

    private void FindAllVisualElementsRecursive<T>(VisualElement parent, List<T> results) where T : VisualElement
    {
        if (parent is T result)
        {
            results.Add(result);
        }

        if (parent is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                if (child is VisualElement visualChild)
                {
                    FindAllVisualElementsRecursive(visualChild, results);
                }
            }
        }
    }

    private async void InitializeMapView(Grid mapContainer, PartnerCartGroup group)
    {
        try
        {
            if (_mapViews.ContainsKey(group.PartnerId))
            {
                // Карта уже инициализирована
                return;
            }

            // Создаём MapView в главном потоке
            MapView? mapView = null;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    // Создаём MapView
                    mapView = new MapView
                    {
                        BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E0E0E0"),
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    // Добавляем MapView в контейнер СНАЧАЛА
                    mapContainer.Children.Add(mapView);
                    _mapViews[group.PartnerId] = mapView;
                    
                    Debug.WriteLine($"[BasketPage] MapView created and added to container for partner {group.PartnerId}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error creating map view for partner {PartnerId}", group.PartnerId);
                    Debug.WriteLine($"[BasketPage] Error creating map view: {ex.Message}");
                }
            });

            if (mapView == null)
            {
                _logger?.LogError("MapView is null after creation for partner {PartnerId}", group.PartnerId);
                Debug.WriteLine($"[BasketPage] MapView is null after creation for partner {group.PartnerId}");
                return;
            }

            // Небольшая задержка перед инициализацией карты (как в MapPage)
            // Это нужно для того, чтобы MapView успел полностью отрендериться
            await Task.Delay(300);

            // Инициализируем карту в главном потоке
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    Debug.WriteLine($"[BasketPage] Initializing map for partner {group.PartnerId}");

                    // Создаём карту
                    var map = new Mapsui.Map();
                    var userAgent = "YessGoApp/1.0 (com.yessgo.front)";
                    var osmLayer = OpenStreetMap.CreateTileLayer(userAgent);
                    map.Layers.Add(osmLayer);
                    
                    Debug.WriteLine($"[BasketPage] OpenStreetMap layer added for partner {group.PartnerId}");

                    // Добавляем маркер партнёра (если координаты есть)
                    if (group.PartnerLatitude.HasValue && group.PartnerLongitude.HasValue)
                    {
                        AddPartnerMarker(map, group);
                        Debug.WriteLine($"[BasketPage] Partner marker added for partner {group.PartnerId}");
                    }

                    // Устанавливаем карту в MapView
                    mapView.Map = map;
                    Debug.WriteLine($"[BasketPage] Map set to MapView for partner {group.PartnerId}");

                    // Подписываемся на изменение центра карты
                    map.Navigator.ViewportChanged += (s, args) => OnMapViewportChanged(mapView, group);

                    // Принудительно обновляем карту
                    try
                    {
                        mapView.Map?.Refresh();
                        Debug.WriteLine($"[BasketPage] Map refreshed for partner {group.PartnerId}");
                    }
                    catch (Exception invEx)
                    {
                        Debug.WriteLine($"[BasketPage] Warning: Could not refresh map: {invEx.Message}");
                    }

                    _logger?.LogInformation("Map initialized for partner {PartnerId}", group.PartnerId);
                    Debug.WriteLine($"[BasketPage] Map initialized for partner {group.PartnerId}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error initializing map for partner {PartnerId}", group.PartnerId);
                    Debug.WriteLine($"[BasketPage] Error initializing map: {ex.Message}");
                }
            });

            // Небольшая задержка перед центрированием карты
            await Task.Delay(200);

            // Центрируем карту в главном потоке
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    // Проверяем, что MapView имеет правильные размеры
                    if (mapView.Width <= 0 || mapView.Height <= 0)
                    {
                        Debug.WriteLine($"[BasketPage] Warning: MapView has invalid size: {mapView.Width}x{mapView.Height}");
                        // Пытаемся установить размеры из контейнера
                        if (mapContainer.Width > 0 && mapContainer.Height > 0)
                        {
                            mapView.WidthRequest = mapContainer.Width;
                            mapView.HeightRequest = mapContainer.Height;
                            Debug.WriteLine($"[BasketPage] Set MapView size from container: {mapContainer.Width}x{mapContainer.Height}");
                        }
                    }

                    if (group.PartnerLatitude.HasValue && group.PartnerLongitude.HasValue)
                    {
                        CenterMapOnPartner(mapView, group);
                        Debug.WriteLine($"[BasketPage] Map centered on partner {group.PartnerId}");
                    }
                    else
                    {
                        CenterMapOnDefaultLocation(mapView);
                        Debug.WriteLine($"[BasketPage] Map centered on default location for partner {group.PartnerId}");
                    }

                    // Принудительно обновляем карту после центрирования
                    try
                    {
                        mapView.Map?.Refresh();
                        Debug.WriteLine($"[BasketPage] Map refreshed after centering for partner {group.PartnerId}");
                    }
                    catch (Exception invEx)
                    {
                        Debug.WriteLine($"[BasketPage] Warning: Could not refresh map after centering: {invEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error centering map for partner {PartnerId}", group.PartnerId);
                    Debug.WriteLine($"[BasketPage] Error centering map: {ex.Message}");
                }
            });

            Debug.WriteLine($"[BasketPage] Map fully initialized for partner {group.PartnerId}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing map view for partner {PartnerId}", group.PartnerId);
            Debug.WriteLine($"[BasketPage] Error initializing map view: {ex.Message}");
        }
    }

    private void AddPartnerMarker(Mapsui.Map map, PartnerCartGroup group)
    {
        try
        {
            if (!group.PartnerLatitude.HasValue || !group.PartnerLongitude.HasValue)
                return;

            var partnerLon = group.PartnerLongitude.Value;
            var partnerLat = group.PartnerLatitude.Value;
            (double x, double y) mercatorCoords = SphericalMercator.FromLonLat(partnerLon, partnerLat);
            var partnerPoint = new Mapsui.MPoint(mercatorCoords.x, mercatorCoords.y);

            var feature = new PointFeature(partnerPoint)
            {
                Styles = new[] { new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#DC2626")),
                    Outline = new Pen(Mapsui.Styles.Color.FromString("#FFFFFF"), 2),
                    SymbolScale = 0.5
                }}
            };

            var features = new List<IFeature> { feature };
            var partnerLayer = new MemoryLayer($"PartnerMarker_{group.PartnerId}");
            
            // Устанавливаем Features через MemoryProvider (как в MapPage)
            try
            {
                var featuresProperty = typeof(MemoryLayer).GetProperty("Features");
                if (featuresProperty != null && featuresProperty.CanWrite)
                {
                    featuresProperty.SetValue(partnerLayer, features);
                }
                else
                {
                    var memoryProvider = new MemoryProvider(features);
                    var dataSourceProperty = typeof(MemoryLayer).GetProperty("DataSource", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (dataSourceProperty != null)
                    {
                        dataSourceProperty.SetValue(partnerLayer, memoryProvider);
                    }
                    else
                    {
                        dynamic dynamicLayer = partnerLayer;
                        dynamicLayer.DataSource = memoryProvider;
                    }
                }
            }
            catch
            {
                try
                {
                    var memoryProvider = new MemoryProvider(features);
                    dynamic dynamicLayer = partnerLayer;
                    dynamicLayer.DataSource = memoryProvider;
                }
                catch (Exception ex2)
                {
                    _logger?.LogError(ex2, "All methods to set features on PartnerMarker layer failed");
                }
            }

            map.Layers.Add(partnerLayer);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding partner marker");
        }
    }

    private void CenterMapOnPartner(MapView mapView, PartnerCartGroup group)
    {
        try
        {
            if (!group.PartnerLatitude.HasValue || !group.PartnerLongitude.HasValue)
                return;

            var partnerLon = group.PartnerLongitude.Value;
            var partnerLat = group.PartnerLatitude.Value;
            (double x, double y) mercatorCoords = SphericalMercator.FromLonLat(partnerLon, partnerLat);
            var partnerPoint = new Mapsui.MPoint(mercatorCoords.x, mercatorCoords.y);

            // Устанавливаем зум для мини-карты (больше resolution = меньше зум)
            var resolution = 38.0; // Zoom level ~12 для мини-карты
            mapView.Map?.Navigator.CenterOnAndZoomTo(partnerPoint, resolution);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error centering map on partner");
        }
    }

    private void CenterMapOnDefaultLocation(MapView mapView)
    {
        try
        {
            // Используем координаты Бишкека по умолчанию
            double bishkekLon = 74.5698;
            double bishkekLat = 42.8746;
            (double x, double y) mercatorCoords = SphericalMercator.FromLonLat(bishkekLon, bishkekLat);
            var bishkekPoint = new Mapsui.MPoint(mercatorCoords.x, mercatorCoords.y);

            // Устанавливаем зум для мини-карты
            var resolution = 38.0; // Zoom level ~12 для мини-карты
            mapView.Map?.Navigator.CenterOnAndZoomTo(bishkekPoint, resolution);
            
            _logger?.LogInformation("Map centered on default location (Bishkek)");
            Debug.WriteLine("[BasketPage] Map centered on default location (Bishkek)");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error centering map on default location");
            Debug.WriteLine($"[BasketPage] Error centering map on default location: {ex.Message}");
        }
    }

    private void OnMapViewportChanged(object sender, PartnerCartGroup group)
    {
        try
        {
            if (sender is not MapView mapView || mapView.Map == null || group == null)
                return;

            if (_geocodingTimers == null)
            {
                _logger?.LogWarning("GeocodingTimers dictionary is null");
                return;
            }

            // Получаем центр карты через Navigator и Viewport (API Mapsui 4/5)
            var map = mapView.Map;
            if (map == null)
                return;

            var navigator = map.Navigator;
            if (navigator == null)
                return;

            var viewport = navigator.Viewport;
            if (viewport == null)
                return;

            // В Mapsui Viewport хранит центр через координаты CenterX/CenterY (в проекции Spherical Mercator)
            var centerX = viewport.CenterX;
            var centerY = viewport.CenterY;

            // Преобразуем из Spherical Mercator в долготу/широту
            (double lon, double lat) = SphericalMercator.ToLonLat(centerX, centerY);

            // Обновляем выбранные координаты
            group.SelectedLatitude = lat;
            group.SelectedLongitude = lon;

            // Отменяем предыдущий таймер для этого партнёра
            if (_geocodingTimers.TryGetValue(group.PartnerId, out var oldTimer))
            {
                try
                {
                    oldTimer?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error disposing old geocoding timer");
                }
            }

            // Создаём новый таймер с debounce (500ms)
            var timer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    // Получаем адрес через reverse geocoding
                    var address = await GetAddressFromCoordinatesAsync(lat, lon);
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            if (group != null)
                            {
                                group.SelectedAddress = address;
                                group.IsLocationSelected = !string.IsNullOrWhiteSpace(address);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error updating group properties on main thread");
                        }
                    });

                    _logger?.LogDebug("Map viewport changed for partner {PartnerId}: {Lat}, {Lon}, Address: {Address}",
                        group?.PartnerId ?? 0, lat, lon, address);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in geocoding timer");
                }
            }, null, 500, Timeout.Infinite);

            _geocodingTimers[group.PartnerId] = timer;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling map viewport change");
        }
    }

    private async Task<string?> GetAddressFromCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            // Используем Nominatim API для reverse geocoding
            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=18&addressdetails=1";
            
            var response = await _httpClient.GetStringAsync(url);
            var jsonDoc = JsonDocument.Parse(response);
            
            if (jsonDoc.RootElement.TryGetProperty("display_name", out var displayName))
            {
                return displayName.GetString();
            }

            // Если display_name нет, пытаемся собрать адрес из address
            if (jsonDoc.RootElement.TryGetProperty("address", out var address))
            {
                var addressParts = new List<string>();
                
                if (address.TryGetProperty("road", out var road))
                    addressParts.Add(road.GetString() ?? "");
                if (address.TryGetProperty("house_number", out var houseNumber))
                    addressParts.Add(houseNumber.GetString() ?? "");
                if (address.TryGetProperty("city", out var city))
                    addressParts.Add(city.GetString() ?? "");

                return string.Join(", ", addressParts.Where(s => !string.IsNullOrEmpty(s)));
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting address from coordinates {Lat}, {Lon}", latitude, longitude);
            return null;
        }
    }

    private void OnMapContainerGridLoaded(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Grid grid && grid.BindingContext is PartnerCartGroup group)
            {
                if (!_mapViews.ContainsKey(group.PartnerId))
                {
                    // Находим контейнер для карты
                    var mapContainer = grid.Children.OfType<Grid>().FirstOrDefault();
                    if (mapContainer != null)
                    {
                        Debug.WriteLine($"[BasketPage] OnMapContainerGridLoaded: Initializing map for partner {group.PartnerId}");
                        // InitializeMapView сам управляет потоками и задержками
                        InitializeMapView(mapContainer, group);
                    }
                    else
                    {
                        Debug.WriteLine($"[BasketPage] OnMapContainerGridLoaded: MapContainer Grid not found");
                    }
                }
                else
                {
                    Debug.WriteLine($"[BasketPage] OnMapContainerGridLoaded: Map already initialized for partner {group.PartnerId}");
                }
            }
            else
            {
                Debug.WriteLine($"[BasketPage] OnMapContainerGridLoaded: Invalid sender or BindingContext");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OnMapContainerGridLoaded");
            Debug.WriteLine($"[BasketPage] Error in OnMapContainerGridLoaded: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        try
        {
            // Очищаем таймеры
            if (_geocodingTimers != null)
            {
                foreach (var timer in _geocodingTimers.Values)
                {
                    try
                    {
                        timer?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error disposing geocoding timer");
                    }
                }
                _geocodingTimers.Clear();
            }
            
            // Очищаем карты
            if (_mapViews != null)
            {
                foreach (var mapView in _mapViews.Values)
                {
                    try
                    {
                        mapView?.Map?.Layers?.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error clearing map layers");
                    }
                }
                _mapViews.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in OnDisappearing");
        }
    }
}
