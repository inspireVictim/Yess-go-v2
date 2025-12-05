#if ANDROID
using Android.Content;
using Android.OS;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using Java.Lang;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using YessGoFront.Config;
using YessGoFront.Services;
using Exception = System.Exception; // Явно указываем использовать System.Exception
using Debug = System.Diagnostics.Debug; // Явно указываем использовать System.Diagnostics.Debug

namespace YessGoFront.Platforms.Android;

public class FinikPaymentService : IFinikPaymentService
{
    private TaskCompletionSource<PaymentResult>? _paymentTaskCompletionSource;
    private ActivityResultLauncher? _finikLauncher;

    public FinikPaymentService()
    {
        // Инициализация будет выполнена при первом использовании
    }

    private YessGoFront.MainActivity GetMainActivity()
    {
        var activity = Platform.CurrentActivity as YessGoFront.MainActivity;
        if (activity == null)
        {
            throw new InvalidOperationException("MainActivity не найдена. Убедитесь, что приложение запущено.");
        }
        return activity;
    }

    private void EnsureLauncherInitialized()
    {
        if (_finikLauncher != null) return;

        try
        {
            var mainActivity = GetMainActivity();
            var activityResultRegistry = mainActivity.ActivityResultRegistry;
            
            _finikLauncher = activityResultRegistry.Register(
                "finik_payment",
                new ActivityResultContracts.StartActivityForResult(),
                new FinikPaymentCallback(this));
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[FinikPaymentService] Error initializing launcher: {ex.Message}");
            throw;
        }
    }

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            EnsureLauncherInitialized();
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Ошибка инициализации: {ex.Message}"
            });
        }

        if (_finikLauncher == null)
        {
            return Task.FromResult(new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Finik launcher не инициализирован"
            });
        }

        _paymentTaskCompletionSource = new TaskCompletionSource<PaymentResult>();

        try
        {
            var mainActivity = GetMainActivity();
            
            // Используем reflection для создания Intent к FinikActivity
            // Так как мы не можем создать bindings для Flutter-based AAR
            var intent = CreateFinikIntent(mainActivity, request);
            
            if (intent == null)
            {
                throw new InvalidOperationException("Не удалось создать Intent для Finik SDK");
            }

            // Запускаем FinikActivity через Intent
            if (_finikLauncher != null)
            {
                _finikLauncher.Launch(intent);
            }
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[FinikPaymentService] Error launching Finik: {ex.Message}");
            Debug.WriteLine($"[FinikPaymentService] StackTrace: {ex.StackTrace}");
            _paymentTaskCompletionSource?.SetResult(new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Ошибка запуска Finik SDK: {ex.Message}"
            });
        }

        return _paymentTaskCompletionSource.Task;
    }

    private void OnPaymentResult(global::Android.App.Result resultCode, Intent? data)
    {
        var result = new PaymentResult();

        try
        {
            if (resultCode == global::Android.App.Result.Ok && data != null)
            {
                var paymentResultJson = data.GetStringExtra("paymentResultJson");
                
                if (!string.IsNullOrEmpty(paymentResultJson))
                {
                    result.PaymentResultJson = paymentResultJson;
                    
                    // Парсим JSON результат (можно использовать System.Text.Json)
                    // Для упрощения здесь базовая обработка
                    result.IsSuccess = paymentResultJson.Contains("\"status\":\"SUCCEEDED\"");
                    result.Status = ExtractJsonValue(paymentResultJson, "status");
                    result.TransactionId = ExtractJsonValue(paymentResultJson, "transactionId");
                    
                    if (double.TryParse(ExtractJsonValue(paymentResultJson, "amount"), out var amount))
                    {
                        result.Amount = (decimal)amount;
                    }
                    
                    if (long.TryParse(ExtractJsonValue(paymentResultJson, "transactionDate"), out var date))
                    {
                        result.TransactionDate = date;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Не получен результат платежа";
                }
            }
            else if (resultCode == global::Android.App.Result.Canceled)
            {
                result.IsCancelled = true;
                result.ErrorMessage = "Платеж отменен пользователем";
            }
            else
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Неизвестная ошибка";
            }
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[FinikPaymentService] Error processing result: {ex.Message}");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            _paymentTaskCompletionSource?.SetResult(result);
        }
    }

    /// <summary>
    /// Создает Intent для запуска FinikActivity используя reflection
    /// для обхода проблемы с отсутствием Java bindings для Flutter-based AAR
    /// </summary>
    private Intent? CreateFinikIntent(global::Android.App.Activity activity, PaymentRequest request)
    {
        try
        {
            // Используем reflection для создания объектов Finik SDK
            var finikActivityClass = Java.Lang.Class.ForName("kg.finik.android.sdk.FinikActivity");
            var intent = new Intent(activity, finikActivityClass);

            // Обязательные параметры
            intent.PutExtra("apiKey", FinikConfig.ApiKey);

            // Создаем CreateItemHandlerWidget через reflection
            var widget = CreateFinikWidget(request);
            if (widget != null && widget is IParcelable parcelableWidget)
            {
                intent.PutExtra("widget", parcelableWidget);
            }

            // Локализация
            var locale = GetFinikLocale(FinikConfig.Locale);
            if (locale != null && locale is IParcelable parcelableLocale)
            {
                intent.PutExtra("locale", parcelableLocale);
            }

            // Опциональные параметры
            if (FinikConfig.IsBeta)
            {
                intent.PutExtra("isBeta", true);
            }

            intent.PutExtra("enableShare", FinikConfig.EnableShare);
            intent.PutExtra("tapableSupportButtons", FinikConfig.TapableSupportButtons);

            // TextScenario
            var textScenario = GetTextScenario(FinikConfig.TextScenario);
            if (textScenario != null && textScenario is IParcelable parcelableScenario)
            {
                intent.PutExtra("textScenario", parcelableScenario);
            }

            return intent;
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[FinikPaymentService] Error creating Finik Intent: {ex.Message}");
            Debug.WriteLine($"[FinikPaymentService] Exception type: {ex.GetType().Name}");
            return null;
        }
    }

    /// <summary>
    /// Создает CreateItemHandlerWidget через Java reflection
    /// </summary>
    private Java.Lang.Object? CreateFinikWidget(PaymentRequest request)
    {
        try
        {
            var widgetClass = Java.Lang.Class.ForName("kg.finik.android.sdk.CreateItemHandlerWidget");
            
            // Создаем RequiredFields (если есть)
            var requiredFieldsList = new Java.Util.ArrayList();
            if (request.RequiredFields != null)
            {
                var requiredFieldClass = Java.Lang.Class.ForName("kg.finik.android.sdk.RequiredField");
                var requiredFieldConstructor = requiredFieldClass.GetConstructor(
                    Java.Lang.Class.FromType(typeof(Java.Lang.String)),
                    Java.Lang.Class.FromType(typeof(Java.Lang.String)));

                foreach (var field in request.RequiredFields)
                {
                    var requiredField = requiredFieldConstructor.NewInstance(
                        new Java.Lang.String(field.Key),
                        new Java.Lang.String(field.Value));
                    requiredFieldsList.Add(requiredField);
                }
            }

            // Создаем FixedAmount (если указана сумма)
            Java.Lang.Object? fixedAmount = null;
            Java.Lang.Class? fixedAmountClass = null;
            if (request.Amount > 0)
            {
                fixedAmountClass = Java.Lang.Class.ForName("kg.finik.android.sdk.FixedAmount");
                var fixedAmountConstructor = fixedAmountClass.GetConstructor(Java.Lang.Class.FromType(typeof(Java.Lang.Double)));
                fixedAmount = fixedAmountConstructor.NewInstance(new Java.Lang.Double((double)request.Amount));
            }

            // Получаем конструктор CreateItemHandlerWidget
            // Конструктор принимает: accountId, name, description, amount, maxAvailableQuantity, requiredFields, callbackUrl, requestId
            if (fixedAmountClass == null)
            {
                fixedAmountClass = Java.Lang.Class.ForName("kg.finik.android.sdk.FixedAmount");
            }
            
            var constructorParams = new Java.Lang.Class[]
            {
                Java.Lang.Class.FromType(typeof(Java.Lang.String)), // accountId
                Java.Lang.Class.FromType(typeof(Java.Lang.String)), // name
                Java.Lang.Class.FromType(typeof(Java.Lang.String)), // description
                fixedAmountClass, // amount (nullable)
                Java.Lang.Class.FromType(typeof(Java.Lang.Integer)), // maxAvailableQuantity
                Java.Lang.Class.FromType(typeof(Java.Util.List)), // requiredFields (nullable)
                Java.Lang.Class.FromType(typeof(Java.Lang.String)), // callbackUrl
                Java.Lang.Class.FromType(typeof(Java.Lang.String))  // requestId
            };

            var constructor = widgetClass.GetConstructor(constructorParams);
            
            // Создаем массив аргументов для конструктора
            var args = new List<Java.Lang.Object>();
            args.Add(new Java.Lang.String(FinikConfig.AccountId));
            args.Add(new Java.Lang.String(request.NameEn ?? "Пополнение баланса"));
            args.Add(new Java.Lang.String(request.Description ?? "Пополнение баланса через YessGo"));
            
            // FixedAmount может быть null, используем JNI для передачи null
            if (fixedAmount != null)
                args.Add(fixedAmount);
            else
                args.Add(null!); // null для nullable параметра
            
            args.Add(new Java.Lang.Integer(request.MaxAvailableQuantity ?? 1));
            args.Add(requiredFieldsList.Size() > 0 ? requiredFieldsList : null!);
            args.Add(new Java.Lang.String(FinikConfig.CallbackUrl));
            args.Add(new Java.Lang.String(request.RequestId ?? Guid.NewGuid().ToString()));

            // Создаем объект
            var widget = constructor.NewInstance(args.ToArray());

            return widget;
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[FinikPaymentService] Error creating Finik widget: {ex.Message}");
            Debug.WriteLine($"[FinikPaymentService] Exception type: {ex.GetType().Name}");
            return null;
        }
    }

    /// <summary>
    /// Создает FinikSdkLocale через reflection
    /// </summary>
    private Java.Lang.Object? GetFinikLocale(string locale)
    {
        try
        {
            var localeClass = Java.Lang.Class.ForName("kg.finik.android.sdk.FinikSdkLocale");
            var localeValue = locale.ToUpper() switch
            {
                "KY" => "KY",
                "EN" => "EN",
                "RU" => "RU",
                _ => "KG"
            };
            
            // Получаем enum значение через reflection
            var field = localeClass.GetField(localeValue);
            return field?.Get(null) as Java.Lang.Object;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Создает TextScenario через reflection
    /// </summary>
    private Java.Lang.Object? GetTextScenario(string scenario)
    {
        try
        {
            var scenarioClass = Java.Lang.Class.ForName("kg.finik.android.sdk.TextScenario");
            var scenarioValue = scenario.ToUpper() == "REPLENISHMENT" ? "REPLENISHMENT" : "PAYMENT";
            
            var field = scenarioClass.GetField(scenarioValue);
            return field?.Get(null) as Java.Lang.Object;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractJsonValue(string json, string key)
    {
        try
        {
            var keyWithQuotes = $"\"{key}\"";
            var keyIndex = json.IndexOf(keyWithQuotes, StringComparison.Ordinal);
            if (keyIndex == -1) return null;

            var valueStart = json.IndexOf(':', keyIndex) + 1;
            var valueEnd = valueStart;

            // Пропускаем пробелы
            while (valueEnd < json.Length && char.IsWhiteSpace(json[valueEnd]))
                valueEnd++;

            if (valueEnd >= json.Length) return null;

            // Определяем тип значения (строка, число, boolean)
            if (json[valueEnd] == '"')
            {
                // Строковое значение
                valueStart = valueEnd + 1;
                valueEnd = json.IndexOf('"', valueStart);
                if (valueEnd == -1) return null;
                return json.Substring(valueStart, valueEnd - valueStart);
            }
            else
            {
                // Числовое или boolean значение
                valueEnd = valueStart;
                while (valueEnd < json.Length && 
                       (char.IsDigit(json[valueEnd]) || json[valueEnd] == '.' || 
                        json[valueEnd] == '-' || json[valueEnd] == 'E' || 
                        json[valueEnd] == 'e' || json[valueEnd] == '+' ||
                        (json[valueEnd] >= 'a' && json[valueEnd] <= 'z') ||
                        (json[valueEnd] >= 'A' && json[valueEnd] <= 'Z')))
                {
                    valueEnd++;
                }
                return json.Substring(valueStart, valueEnd - valueStart).Trim();
            }
        }
        catch
        {
            return null;
        }
    }

    // Callback класс для обработки результата Activity
    private class FinikPaymentCallback : Java.Lang.Object, AndroidX.Activity.Result.IActivityResultCallback
    {
        private readonly FinikPaymentService _service;

        public FinikPaymentCallback(FinikPaymentService service)
        {
            _service = service;
        }

        public void OnActivityResult(Java.Lang.Object? result)
        {
            if (result is ActivityResult activityResult)
            {
                _service.OnPaymentResult(
                    (global::Android.App.Result)activityResult.ResultCode,
                    activityResult.Data);
            }
        }
    }
}
#endif

