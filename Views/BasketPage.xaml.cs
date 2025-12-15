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
    private bool _isAppearing = false; // Защита от повторных вызовов OnAppearing
    private readonly SemaphoreSlim _actionLock = new(1, 1); // Защита от повторных нажатий
    private bool _isPageActive = false;
    private readonly SemaphoreSlim _mapsLock = new(1, 1); // Защита для работы с картами

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

        if (_isAppearing)
            return; // Уже выполняется

        _isAppearing = true;
        try
        {
            await OnAppearingAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BasketPage] Error in OnAppearing: {ex.Message}");
        }
        finally
        {
            _isAppearing = false;
        }
    }

    protected virtual async Task OnAppearingAsync()
    {
        _isPageActive = true; // Отмечаем, что страница активна
        
        // Загружаем данные корзины при появлении страницы
        if (_viewModel != null)
        {
            await _viewModel.LoadCartCommand.ExecuteAsync(null);
            // Карты будут инициализированы лениво через OnMapContainerGridLoaded
        }
    }

    private void OnPartnerGroupsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Карты будут инициализированы лениво через OnMapContainerGridLoaded
        // Не вызываем InitializeMaps здесь, чтобы избежать задержек и рекурсивного поиска
    }

    public async void OnBackButtonClicked(object sender, EventArgs e)
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
            Debug.WriteLine($"[BasketPage] Error in OnBackButtonClicked: {ex.Message}");
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
        // Защита от повторных нажатий
        if (!await _actionLock.WaitAsync(0))
            return; // Уже обрабатывается

        try
        {
            // Отключаем кнопку визуально
            if (sender is VisualElement element)
                element.IsEnabled = false;

            await OnTopUpButtonClickedAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BasketPage] Error in OnTopUpButtonClicked: {ex.Message}");
        }
        finally
        {
            if (sender is VisualElement element)
                element.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnTopUpButtonClickedAsync()
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

    // Удалены методы InitializeMaps, FindVisualElement, FindAndInitializeMapForPartner, 
    // FindAllVisualElements, FindAllVisualElementsRecursive - они больше не нужны,
    // так как инициализация карт происходит лениво через OnMapContainerGridLoaded

    private async Task InitializeMapView(Grid mapContainer, PartnerCartGroup group)
    {
        // Проверяем, что страница еще активна
        if (!_isPageActive)
        {
            Debug.WriteLine($"[BasketPage] InitializeMapView: Page is not active, skipping for partner {group.PartnerId}");
            return;
        }
        
        try
        {
            // Используем блокировку для проверки
            if (!await _mapsLock.WaitAsync(0))
            {
                Debug.WriteLine($"[BasketPage] InitializeMapView: Maps lock is busy for partner {group.PartnerId}");
                return;
            }
            
            try
            {
                if (_mapViews.ContainsKey(group.PartnerId))
                {
                    // Карта уже инициализирована
                    return;
                }
                
                if (!_isPageActive)
                {
                    return; // Страница уже неактивна
                }
            }
            finally
            {
                _mapsLock.Release();
            }

            // Создаём MapView в главном потоке
            MapView? mapView = null;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    if (!_isPageActive)
                        return;
                
                    // Создаём MapView
                    mapView = new MapView
                    {
                        BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E0E0E0"),
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };

                    // Добавляем MapView в контейнер СНАЧАЛА
                    mapContainer.Children.Add(mapView);
                    
                    // ВАЖНО: Устанавливаем размеры из контейнера сразу
                    if (mapContainer.Width > 0 && mapContainer.Height > 0)
                    {
                        mapView.WidthRequest = mapContainer.Width;
                        mapView.HeightRequest = mapContainer.Height;
                    }
                    
                    // Блокируем доступ для добавления в словарь
                    _mapsLock.Wait();
                    try
                    {
                        if (!_isPageActive)
                        {
                            mapContainer.Children.Remove(mapView);
                            return;
                        }
                        _mapViews[group.PartnerId] = mapView;
                    }
                    finally
                    {
                        _mapsLock.Release();
                    }
                    
                    Debug.WriteLine($"[BasketPage] MapView created and added to container for partner {group.PartnerId}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error creating map view for partner {PartnerId}", group.PartnerId);
                    Debug.WriteLine($"[BasketPage] Error creating map view: {ex.Message}");
                }
            });

            if (mapView == null || !_isPageActive)
            {
                Debug.WriteLine($"[BasketPage] MapView is null or page inactive for partner {group.PartnerId}");
                return;
            }

            // ВАЖНО: Задержка для Release режима (как в MapPage)
            await Task.Delay(200);

            if (!_isPageActive)
            {
                Debug.WriteLine($"[BasketPage] Page became inactive during delay for partner {group.PartnerId}");
                return;
            }

            // Инициализируем карту в главном потоке
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    if (!_isPageActive || mapView == null)
                        return;
                
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
                    map.Navigator.ViewportChanged += (s, args) => 
                    {
                        if (_isPageActive)
                        {
                            OnMapViewportChanged(mapView, group);
                        }
                    };

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

            if (!_isPageActive)
            {
                return;
            }

            // Еще одна небольшая задержка перед центрированием (для Release)
            await Task.Delay(100);

            if (!_isPageActive)
            {
                return;
            }

            // Центрируем карту в главном потоке
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    if (!_isPageActive || mapView == null)
                        return;
                
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
                            
                            // Еще одна задержка после установки размеров
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(100);
                                if (_isPageActive)
                                {
                                    await MainThread.InvokeOnMainThreadAsync(() =>
                                    {
                                        if (group.PartnerLatitude.HasValue && group.PartnerLongitude.HasValue)
                                        {
                                            CenterMapOnPartner(mapView, group);
                                        }
                                        else
                                        {
                                            CenterMapOnDefaultLocation(mapView);
                                        }
                                        mapView.Map?.Refresh();
                                    });
                                }
                            });
                            return;
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
        // Проверяем, что страница еще активна
        if (!_isPageActive)
        {
            return;
        }
        
        try
        {
            if (sender is not MapView mapView || mapView.Map == null || group == null)
                return;

            if (_geocodingTimers == null)
            {
                _logger?.LogWarning("GeocodingTimers dictionary is null");
                return;
            }

            // Получаем центр карты через Navigator и Viewport
            var map = mapView.Map;
            if (map == null)
                return;

            var navigator = map.Navigator;
            if (navigator == null)
                return;

            var viewport = navigator.Viewport;
            if (viewport == null)
                return;

            // В Mapsui Viewport хранит центр через координаты CenterX/CenterY
            var centerX = viewport.CenterX;
            var centerY = viewport.CenterY;

            // Преобразуем из Spherical Mercator в долготу/широту
            (double lon, double lat) = SphericalMercator.ToLonLat(centerX, centerY);

            // Обновляем выбранные координаты
            group.SelectedLatitude = lat;
            group.SelectedLongitude = lon;

            // Отменяем предыдущий таймер для этого партнёра (с блокировкой)
            System.Threading.Timer? oldTimer = null;
            lock (_geocodingTimers)
            {
                if (_geocodingTimers.TryGetValue(group.PartnerId, out oldTimer))
                {
                    _geocodingTimers.Remove(group.PartnerId);
                }
            }
            
            if (oldTimer != null)
            {
                try
                {
                    oldTimer.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error disposing old geocoding timer");
                }
            }

            // Проверяем, что страница еще активна перед созданием нового таймера
            if (!_isPageActive)
            {
                return;
            }

            // Создаём новый таймер с debounce (500ms)
            var timer = new System.Threading.Timer(async _ =>
            {
                // Проверяем активность страницы перед выполнением
                if (!_isPageActive)
                {
                    return;
                }
                
                try
                {
                    // Получаем адрес через reverse geocoding
                    var address = await GetAddressFromCoordinatesAsync(lat, lon);
                    
                    // Проверяем активность еще раз перед обновлением UI
                    if (!_isPageActive)
                    {
                        return;
                    }
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            if (_isPageActive && group != null)
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

            // Добавляем таймер в словарь с блокировкой
            lock (_geocodingTimers)
            {
                if (_isPageActive)
                {
                    _geocodingTimers[group.PartnerId] = timer;
                }
                else
                {
                    timer.Dispose();
                }
            }
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
        // Проверяем, что страница еще активна
        if (!_isPageActive)
        {
            Debug.WriteLine("[BasketPage] OnMapContainerGridLoaded: Page is not active, skipping map initialization");
            return;
        }
        
        try
        {
            if (sender is Grid grid && grid.BindingContext is PartnerCartGroup group)
            {
                // Используем блокировку для проверки и инициализации
                if (!_mapsLock.WaitAsync(0).Result)
                {
                    Debug.WriteLine("[BasketPage] OnMapContainerGridLoaded: Maps lock is busy, retrying...");
                    // Пытаемся через небольшую задержку
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        if (_isPageActive && await _mapsLock.WaitAsync(100))
                        {
                            try
                            {
                                if (!_mapViews.ContainsKey(group.PartnerId))
                                {
                                    var mapContainer = grid.Children.OfType<Grid>().FirstOrDefault();
                                    if (mapContainer != null)
                                    {
                                        await InitializeMapView(mapContainer, group);
                                    }
                                }
                            }
                            finally
                            {
                                _mapsLock.Release();
                            }
                        }
                    });
                    return;
                }
                
                try
                {
                    if (!_mapViews.ContainsKey(group.PartnerId))
                    {
                        // Находим контейнер для карты напрямую через Children
                        var mapContainer = grid.Children.OfType<Grid>().FirstOrDefault();
                        if (mapContainer != null)
                        {
                            Debug.WriteLine($"[BasketPage] OnMapContainerGridLoaded: Initializing map for partner {group.PartnerId}");
                            // Инициализируем карту с проверкой активности страницы
                            _ = InitializeMapView(mapContainer, group);
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
                finally
                {
                    _mapsLock.Release();
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
        
        // Сразу отмечаем, что страница неактивна
        _isPageActive = false;
        
        try
        {
            // Используем блокировку для безопасной очистки
            _mapsLock.Wait();
            
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
                        // Отписываемся от событий
                        if (mapView?.Map?.Navigator != null)
                        {
                            mapView.Map.Navigator.ViewportChanged -= (s, args) => { }; // Отписываемся
                        }
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
        finally
        {
            _mapsLock.Release();
        }
    }
}
