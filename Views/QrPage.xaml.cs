using YessGoFront.ViewModels;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;
using YessGoFront.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using System.Text.Json;
using System.Reflection;
using System.Linq;
using System.Threading;
#if ANDROID
using AndroidX.Camera.View;
using Microsoft.Maui.Platform;
#endif
#if IOS
using AVFoundation;
using UIKit;
using CoreAnimation;
using Microsoft.Maui.Platform;
#endif

namespace YessGoFront.Views;

public partial class QrPage : ContentPage
{
    private readonly IPartnersApiService _partnersService;
    private readonly ILogger<QrPage>? _logger;
    private bool _isProcessing = false;
    private bool _isInitialized = false;
    
    // Оптимизация: кэширование партнеров и защита от повторных вызовов
    private List<PartnerDto>? _cachedPartners;
    private DateTime _lastPartnersUpdate = DateTime.MinValue;
    private const int PartnersCacheSeconds = 60;
    private readonly SemaphoreSlim _processQrLock = new(1, 1);

    public QrPage()
    {
        try
        {
            InitializeComponent();
            
            // Безопасная инициализация сервисов
            if (MauiProgram.Services == null)
            {
                throw new InvalidOperationException("MauiProgram.Services не инициализирован");
            }
            
            try
            {
                BindingContext = MauiProgram.Services.GetRequiredService<QrViewModel>();
                _partnersService = MauiProgram.Services.GetRequiredService<IPartnersApiService>();
                _logger = MauiProgram.Services.GetService<ILogger<QrPage>>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QrPage] Ошибка при получении сервисов: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QrPage] КРИТИЧЕСКАЯ ОШИБКА В КОНСТРУКТОРЕ: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[QrPage] Stack trace: {ex.StackTrace}");
            _logger?.LogError(ex, "[QrPage] Ошибка в конструкторе");
            throw;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (_isInitialized)
            return;

        try
        {
            System.Diagnostics.Debug.WriteLine("[QrPage] === OnAppearing НАЧАЛО ===");
            
            // Проверяем и запрашиваем разрешение на камеру
            var hasPermission = await RequestCameraPermissionAsync();
            
            if (!hasPermission)
            {
                _logger?.LogWarning("[QrPage] Разрешение на камеру не получено");
                await DisplayAlert("Доступ к камере", 
                    "Для сканирования QR-кодов необходимо разрешение на использование камеры. Пожалуйста, предоставьте разрешение в настройках приложения.", 
                    "OK");
                
                // Закрываем страницу, если разрешения нет
                await Shell.Current.GoToAsync("///main/partner");
                return;
            }

            // Активируем камеру после получения разрешений
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (BarCodeReader != null)
                    {
                        BarCodeReader.IsDetecting = true;
                        System.Diagnostics.Debug.WriteLine("[QrPage] Камера активирована");
                        
                        // Настраиваем камеру на высокое качество в фоне (fire-and-forget)
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(500);
                            await ConfigureCameraForHighQuality();
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[QrPage] ВНИМАНИЕ: BarCodeReader == null");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[QrPage] Ошибка при активации камеры: {ex.Message}");
                    _logger?.LogError(ex, "[QrPage] Ошибка при активации камеры");
                }
            });
            
            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine("[QrPage] === OnAppearing ЗАВЕРШЕНО ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QrPage] ОШИБКА В OnAppearing: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[QrPage] Stack trace: {ex.StackTrace}");
            _logger?.LogError(ex, "[QrPage] Ошибка в OnAppearing");
            
            await DisplayAlert("Ошибка", 
                "Не удалось инициализировать камеру. Попробуйте позже.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("[QrPage] OnDisappearing - останавливаем камеру");
            
            // Останавливаем камеру при уходе со страницы
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (BarCodeReader != null)
                    {
                        BarCodeReader.IsDetecting = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[QrPage] Ошибка при остановке камеры: {ex.Message}");
                    _logger?.LogError(ex, "[QrPage] Ошибка при остановке камеры");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QrPage] Ошибка в OnDisappearing: {ex.Message}");
            _logger?.LogError(ex, "[QrPage] Ошибка в OnDisappearing");
        }
    }

    private async Task ConfigureCameraForHighQuality()
    {
        try
        {
            // Небольшая задержка для полной инициализации камеры ZXing (неблокирующая)
            await Task.Delay(300);
            
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    
#if ANDROID
                    await ConfigureCameraForHighQualityAndroid();
#elif IOS
                    await ConfigureCameraForHighQualityIOS();
#endif
                }
                catch (Exception ex)
                {
                    // Не критично, если не удалось настроить качество
                    System.Diagnostics.Debug.WriteLine($"[QrPage] Предупреждение: не удалось настроить качество камеры: {ex.Message}");
                    _logger?.LogWarning(ex, "[QrPage] Не удалось настроить качество камеры");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QrPage] Ошибка при настройке качества камеры: {ex.Message}");
            _logger?.LogWarning(ex, "[QrPage] Ошибка при настройке качества камеры");
        }
    }

#if ANDROID
    private async Task ConfigureCameraForHighQualityAndroid()
    {
        try
        {
            // Небольшая задержка для инициализации камеры ZXing
            await Task.Delay(500);
            
            // Получаем platform view камеры через Handler
            var handler = BarCodeReader?.Handler;
            PreviewView? previewView = null;
            
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (handler?.PlatformView is PreviewView pv)
                {
                    previewView = pv;
                    break;
                }
                
                if (attempt < 2)
                {
                    System.Diagnostics.Debug.WriteLine($"[QrPage] Android: Попытка {attempt + 1}/3 получить PreviewView...");
                    await Task.Delay(500);
                    handler = BarCodeReader?.Handler;
                }
            }

            if (previewView == null)
            {
                System.Diagnostics.Debug.WriteLine("[QrPage] Android: Не удалось получить PreviewView после нескольких попыток");
                return;
            }

            // Настраиваем качество отображения
            // Используем рефлексию для безопасной установки свойств без прямых ссылок на типы
            try
            {
                // Способ 1: Устанавливаем ScaleType через свойство
                var scaleTypeProperty = typeof(PreviewView).GetProperty("ScaleType");
                if (scaleTypeProperty != null && scaleTypeProperty.CanWrite)
                {
                    // Получаем тип enum из свойства
                    var scaleTypeEnumType = scaleTypeProperty.PropertyType;
                    if (scaleTypeEnumType.IsEnum)
                    {
                        // Пробуем найти значение FillCenter
                        var fillCenterValue = Enum.GetValues(scaleTypeEnumType)
                            .Cast<object>()
                            .FirstOrDefault(v => v.ToString() == "FillCenter");
                        
                        if (fillCenterValue != null)
                        {
                            scaleTypeProperty.SetValue(previewView, fillCenterValue);
                            System.Diagnostics.Debug.WriteLine("[QrPage] Android: ScaleType установлен на FillCenter");
                        }
                    }
                }
            }
            catch (Exception propEx)
            {
                System.Diagnostics.Debug.WriteLine($"[QrPage] Android: Установка ScaleType через свойство не сработала: {propEx.Message}");
                
                // Способ 2: Пробуем вызов метода SetScaleType через рефлексию
                try
                {
                    // Получаем тип ScaleType через пространство имен
                    var scaleTypeType = typeof(PreviewView).Assembly.GetType("AndroidX.Camera.View.ScaleType") ??
                                      Type.GetType("AndroidX.Camera.View.ScaleType, AndroidX.Camera.View");
                    
                    if (scaleTypeType != null)
                    {
                        var setScaleTypeMethod = typeof(PreviewView).GetMethod("SetScaleType", 
                            new[] { scaleTypeType });
                        
                        if (setScaleTypeMethod != null)
                        {
                            var fillCenterValue = Enum.GetValues(scaleTypeType)
                                .Cast<object>()
                                .FirstOrDefault(v => v.ToString() == "FillCenter");
                            
                            if (fillCenterValue != null)
                            {
                                setScaleTypeMethod.Invoke(previewView, new[] { fillCenterValue });
                                System.Diagnostics.Debug.WriteLine("[QrPage] Android: ScaleType установлен через SetScaleType");
                            }
                        }
                    }
                }
                catch (Exception methodEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[QrPage] Android: SetScaleType не сработал: {methodEx.Message}");
                }
            }

            // Также настраиваем ImplementationMode, если доступен
            try
            {
                var implModeProperty = typeof(PreviewView).GetProperty("ImplementationMode");
                if (implModeProperty != null)
                {
                    var compatibleValue = Enum.Parse(implModeProperty.PropertyType, "Compatible");
                    implModeProperty.SetValue(previewView, compatibleValue);
                    System.Diagnostics.Debug.WriteLine("[QrPage] Android: ImplementationMode установлен");
                }
            }
            catch (Exception implEx)
            {
                System.Diagnostics.Debug.WriteLine($"[QrPage] Android: ImplementationMode не доступен: {implEx.Message}");
            }

            System.Diagnostics.Debug.WriteLine("[QrPage] Android: Настройка качества камеры завершена");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QrPage] Android: Ошибка настройки качества камеры: {ex.Message}");
            _logger?.LogWarning(ex, "[QrPage] Android: Ошибка настройки качества камеры");
        }
    }
#endif

#if IOS
    private async Task ConfigureCameraForHighQualityIOS()
    {
        try
        {
            // Получаем platform view камеры через Handler
            var handler = BarCodeReader?.Handler;
            if (handler?.PlatformView == null)
            {
                System.Diagnostics.Debug.WriteLine("[QrPage] iOS: Не удалось получить platform view - попробуем позже");
                // Повторная попытка через некоторое время
                await Task.Delay(500);
                handler = BarCodeReader?.Handler;
                if (handler?.PlatformView == null)
                {
                    System.Diagnostics.Debug.WriteLine("[QrPage] iOS: Не удалось получить platform view после повтора");
                    return;
                }
            }

            // На iOS ZXing использует AVCaptureVideoPreviewLayer через UIView
            // Настраиваем качество через videoGravity для лучшего отображения
            var platformView = handler?.PlatformView;
            
            // Настраиваем слой предпросмотра для улучшения качества
            if (platformView is UIView uiView)
            {
                // Ищем AVCaptureVideoPreviewLayer в слоях
                var previewLayer = FindPreviewLayer(uiView);
                if (previewLayer != null)
                {
                    // Устанавливаем высокое качество отображения
                    previewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
                    
                    System.Diagnostics.Debug.WriteLine("[QrPage] iOS: Параметры отображения камеры настроены для улучшения качества");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[QrPage] iOS: Не удалось найти AVCaptureVideoPreviewLayer");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QrPage] iOS: Ошибка настройки качества камеры: {ex.Message}");
            _logger?.LogWarning(ex, "[QrPage] iOS: Ошибка настройки качества камеры");
        }
    }

    private AVCaptureVideoPreviewLayer? FindPreviewLayer(UIView view)
    {
        // Ищем AVCaptureVideoPreviewLayer в слоях view
        foreach (var layer in view.Layer.Sublayers ?? Array.Empty<CALayer>())
        {
            if (layer is AVCaptureVideoPreviewLayer previewLayer)
            {
                return previewLayer;
            }
            
            // Рекурсивный поиск во вложенных слоях
            if (layer.Sublayers != null)
            {
                foreach (var subLayer in layer.Sublayers)
                {
                    if (subLayer is AVCaptureVideoPreviewLayer subPreviewLayer)
                    {
                        return subPreviewLayer;
                    }
                }
            }
        }
        
        return null;
    }
#endif

    private async Task<bool> RequestCameraPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            
            if (status == PermissionStatus.Granted)
            {
                System.Diagnostics.Debug.WriteLine("[QrPage] Разрешение на камеру уже предоставлено");
                return true;
            }

            if (status == PermissionStatus.Denied)
            {
                System.Diagnostics.Debug.WriteLine("[QrPage] Разрешение на камеру отклонено");
                return false;
            }

            // Запрашиваем разрешение
            status = await Permissions.RequestAsync<Permissions.Camera>();
            var granted = status == PermissionStatus.Granted;
            
            System.Diagnostics.Debug.WriteLine($"[QrPage] Результат запроса разрешения на камеру: {status} (granted: {granted})");
            return granted;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QrPage] Ошибка при запросе разрешения на камеру: {ex.Message}");
            _logger?.LogError(ex, "[QrPage] Ошибка при запросе разрешения на камеру");
            return false;
        }
    }

    private async void BarCodeReader_BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result == null || _isProcessing)
            return;

        if (!await _processQrLock.WaitAsync(0))
            return; // Уже обрабатывается

        _isProcessing = true;

        try
        {
            var scannedQrCode = result.Value;
            _logger?.LogInformation("QR код отсканирован: {QrCode}", scannedQrCode);

            await Dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    PartnerDto? matchedPartner = null;
                    int? partnerIdFromQr = null;
                    string? partnerNameFromQr = null;

                    // Пытаемся распарсить QR-код как JSON
                    try
                    {
                        var qrData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(scannedQrCode);
                        
                        if (qrData != null && qrData.ContainsKey("partner_id"))
                        {
                            var partnerIdElement = qrData["partner_id"];
                            if (partnerIdElement.ValueKind == JsonValueKind.Number)
                            {
                                partnerIdFromQr = partnerIdElement.GetInt32();
                            }
                            else if (partnerIdElement.ValueKind == JsonValueKind.String)
                            {
                                if (int.TryParse(partnerIdElement.GetString(), out var parsedId))
                                {
                                    partnerIdFromQr = parsedId;
                                }
                            }
                            
                            if (qrData.ContainsKey("partner_name"))
                            {
                                partnerNameFromQr = qrData["partner_name"].GetString();
                            }
                            
                            _logger?.LogInformation("Извлечен partner_id из QR: {PartnerId}", partnerIdFromQr);
                        }
                        else
                        {
                            _logger?.LogWarning("QR код не содержит partner_id в JSON");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger?.LogDebug(jsonEx, "QR код не является JSON, попробуем найти по QrCodeUrl");
                    }
                    catch (Exception parseEx)
                    {
                        _logger?.LogWarning(parseEx, "Ошибка парсинга QR кода как JSON");
                    }

                    // Получаем список всех партнёров (используем кэш)
                    var partners = await GetCachedPartnersAsync();
                    
                    // Если удалось извлечь partner_id из JSON, ищем по ID
                    if (partnerIdFromQr.HasValue)
                    {
                        matchedPartner = partners.FirstOrDefault(p => p.Id == partnerIdFromQr.Value);
                        
                        if (matchedPartner != null)
                        {
                            _logger?.LogInformation("Найден партнёр по ID из QR: {PartnerId} - {PartnerName}", 
                                matchedPartner.Id, matchedPartner.Name);
                        }
                    }
                    
                    // Если не нашли по ID, пробуем найти по QrCodeUrl (обратная совместимость)
                    if (matchedPartner == null)
                    {
                        matchedPartner = partners.FirstOrDefault(p => 
                            !string.IsNullOrWhiteSpace(p.QrCodeUrl) && 
                            p.QrCodeUrl.Equals(scannedQrCode, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchedPartner != null)
                        {
                            _logger?.LogInformation("Найден партнёр по QrCodeUrl: {PartnerId} - {PartnerName}", 
                                matchedPartner.Id, matchedPartner.Name);
                        }
                    }

                    if (matchedPartner != null)
                    {
                        _logger?.LogInformation("Найден партнёр: {PartnerId} - {PartnerName}", matchedPartner.Id, matchedPartner.Name);
                        
                        // Переходим на страницу оплаты с параметрами партнёра
                        var partnerNameParam = Uri.EscapeDataString(matchedPartner.Name ?? "");
                        var qrCodeParam = Uri.EscapeDataString(scannedQrCode);
                        
                        await Shell.Current.GoToAsync($"payment?partnerId={matchedPartner.Id}&partnerName={partnerNameParam}&qrCode={qrCodeParam}");
                    }
                    else
                    {
                        _logger?.LogWarning("Партнёр с QR кодом не найден. PartnerId из QR: {PartnerId}, QrCode: {QrCode}", 
                            partnerIdFromQr, scannedQrCode);
                        await DisplayAlert("QR код не найден", 
                            "Данный QR код не соответствует ни одному партнёру.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка обработки QR кода");
                    await DisplayAlert("Ошибка", 
                        "Не удалось обработать QR код. Попробуйте ещё раз.", "OK");
                }
                finally
                {
                    _isProcessing = false;
                    _processQrLock.Release();
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при сканировании QR");
            _isProcessing = false;
            _processQrLock.Release();
        }
    }

    private async Task<List<PartnerDto>> GetCachedPartnersAsync()
    {
        if (_cachedPartners != null && 
            (DateTime.Now - _lastPartnersUpdate).TotalSeconds < PartnersCacheSeconds)
        {
            return _cachedPartners;
        }
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var partners = await _partnersService.GetAllAsync();
            // Преобразуем IReadOnlyList в List
            _cachedPartners = partners?.ToList() ?? new List<PartnerDto>();
            _lastPartnersUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка загрузки партнеров для QR");
            // Возвращаем старый кэш, если есть
            if (_cachedPartners != null)
                return _cachedPartners;
            // Иначе возвращаем пустой список
            return new List<PartnerDto>();
        }
        
        return _cachedPartners;
    }
}
