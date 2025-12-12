using System;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Services.Domain;
using YessGoFront.Services.Api;
#if ANDROID
using Android.Util;
#endif

namespace YessGoFront.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginViewModel>? _logger;
    private readonly SemaphoreSlim _loginLock = new(1, 1);

    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool rememberMe = false;
    [ObservableProperty] private bool isBusy = false;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool hasError = false;

    public LoginViewModel(IAuthService authService, ILogger<LoginViewModel>? logger = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        // Защита от повторных вызовов
        if (!await _loginLock.WaitAsync(0))
        {
            _logger?.LogWarning("[LoginViewModel] Login already in progress, ignoring duplicate call");
            return;
        }

        try
        {
            // Очищаем предыдущие ошибки
            HasError = false;
            ErrorMessage = null;

            // Простая валидация перед отправкой
            var validationError = ValidateInputs();
            if (validationError != null)
            {
                ShowError(validationError);
                return;
            }

            // Нормализуем телефон (используем оптимизированный метод)
            var phoneTrimmed = Phone.Trim();
            var normalizedPhone = NormalizePhone(phoneTrimmed);
            var passwordTrimmed = Password.Trim();

            IsBusy = true;
            _logger?.LogInformation("[LoginViewModel] Attempting login for phone: {Phone}", normalizedPhone);
            var startTime = DateTime.UtcNow;

            // Используем таймаут для операции входа (15 секунд)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await _authService.LoginWithPhoneAsync(normalizedPhone, passwordTrimmed, cts.Token);
            
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogInformation("[LoginViewModel] Login successful in {Duration}ms. UserId: {UserId}", 
                duration.TotalMilliseconds, response?.UserId ?? 0);
            
            // Проверка на null response
            if (response == null)
            {
                _logger?.LogError("[LoginViewModel] Login response is null");
                ShowError("Получен пустой ответ от сервера");
                return;
            }
            
            // Проверка на валидный токен
            if (string.IsNullOrWhiteSpace(response.AccessToken))
            {
                _logger?.LogError("[LoginViewModel] AccessToken is null or empty in response");
                ShowError("Не получен токен доступа от сервера");
                return;
            }

            // Вызываем событие успешного входа
            if (OnLoginSuccess != null)
            {
                await OnLoginSuccess.Invoke(response);
            }
        }
        catch (OperationCanceledException)
        {
            ShowError("Операция входа заняла слишком много времени. Проверьте подключение к интернету и попробуйте снова.");
            _logger?.LogWarning("[LoginViewModel] Login operation timed out after 15 seconds");
        }
        catch (NetworkException ex)
        {
            var errorMessage = ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                ? "Сервер не отвечает. Проверьте подключение к интернету."
                : ex.Message.Contains("cleartext", StringComparison.OrdinalIgnoreCase) || 
                  ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) || 
                  ex.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase)
                ? "Ошибка подключения к серверу. Проверьте настройки сети."
                : "Ошибка сети. Проверьте интернет-соединение.";
            
            ShowError(errorMessage);
            _logger?.LogError(ex, "Network error during login: {Message}", ex.Message);
            
#if ANDROID
            Android.Util.Log.Error("LoginViewModel", $"Network error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Android.Util.Log.Error("LoginViewModel", $"Inner exception: {ex.InnerException.Message}");
            }
#endif
        }
        catch (UnauthorizedException ex)
        {
            ShowError(ex.Message.Contains("Неверный", StringComparison.OrdinalIgnoreCase) || 
                     ex.Message.Contains("неверный", StringComparison.OrdinalIgnoreCase)
                ? ex.Message 
                : "Неверный телефон или пароль");
            _logger?.LogWarning(ex, "Unauthorized login attempt");
        }
        catch (BadRequestException ex)
        {
            ShowError(ex.Message);
            _logger?.LogWarning(ex, "Bad request during login");
        }
        catch (ApiException ex)
        {
            ShowError($"Ошибка при входе: {ex.Message}");
            _logger?.LogWarning(ex, "API error during login");
        }
        catch (Exception ex)
        {
            ShowError($"Неизвестная ошибка: {ex.Message}");
            _logger?.LogError(ex, "Unexpected error during login");
        }
        finally
        {
            IsBusy = false;
            _loginLock.Release();
        }
    }

    private string? ValidateInputs()
    {
        // Валидация телефона
        if (string.IsNullOrWhiteSpace(Phone))
        {
            return "Введите номер телефона";
        }

        var phoneTrimmed = Phone.Trim();
        // Убрана избыточная проверка - если Phone не пустой, то phoneTrimmed тоже не будет пустым после Trim()
        var phoneDigits = new string(phoneTrimmed.Where(char.IsDigit).ToArray());
        if (phoneDigits.StartsWith("996") && phoneDigits.Length > 3)
        {
            phoneDigits = phoneDigits.Substring(3);
        }
        if (phoneDigits.Length != 9)
        {
            return "Введите корректный номер телефона (9 цифр)";
        }

        // Валидация пароля
        if (string.IsNullOrWhiteSpace(Password))
        {
            return "Введите пароль";
        }

        var passwordTrimmed = Password.Trim();
        // Убрана избыточная проверка - если Password не пустой, то passwordTrimmed тоже не будет пустым после Trim()
        if (passwordTrimmed.Length < 6)
        {
            return "Пароль должен содержать минимум 6 символов";
        }

        return null; // Валидация прошла успешно
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        _logger?.LogWarning("Login error: {Message}", message);
    }

    /// <summary>
    /// Очистить все поля формы логина
    /// </summary>
    public void ClearFields()
    {
        Phone = string.Empty;
        Password = string.Empty;
        RememberMe = false;
        ErrorMessage = null;
        HasError = false;
        IsBusy = false;
        _logger?.LogDebug("Login fields cleared");
    }

    public event Func<AuthResponse, Task>? OnLoginSuccess;

    private static string NormalizePhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        if (input.StartsWith("+996"))
            return input;

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("996") && digits.Length > 3)
            digits = digits[3..];

        if (digits.StartsWith("0") && digits.Length > 1)
            digits = digits[1..];

        return digits.Length == 9 ? "+996" + digits : input;
    }

}
