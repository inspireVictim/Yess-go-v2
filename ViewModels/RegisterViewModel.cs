using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Services.Domain;
using YessGoFront.Services.Api;
using VerifyCodeRequest = YessGoFront.Services.Api.VerifyCodeRequest;

namespace YessGoFront.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILogger<RegisterViewModel>? _logger;

    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private bool isBusy = false;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool hasError = false;
    [ObservableProperty] private string? phoneError;
    [ObservableProperty] private bool hasPhoneError = false;
    [ObservableProperty] private bool isPolicyAcknowledged = false;

    // SMS verification
    [ObservableProperty] private string verificationCode = string.Empty;
    [ObservableProperty] private bool isVerificationStep = false;
    [ObservableProperty] private bool isCodeSent = false;
    [ObservableProperty] private string? successMessage;
    [ObservableProperty] private string? displayedVerificationCode;
    
    // Реферальный код из URL
    [ObservableProperty] private string? referralCode;
    
    // Флаг успешной регистрации - предотвращает повторный вызов verify-code
    [ObservableProperty] private bool isRegistrationSuccessful = false;

    public RegisterViewModel(IAuthService authService, ILogger<RegisterViewModel>? logger = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        // Защита от повторного вызова после успешной регистрации
        if (IsRegistrationSuccessful)
        {
            _logger?.LogWarning("Registration already completed, ignoring duplicate call");
            return;
        }

        if (IsBusy)
            return;

        // Step 1: request code
        if (!IsVerificationStep)
        {
            await SendVerificationCodeAsync();
            return;
        }

        // Step 2: register
        await VerifyCodeAndRegisterAsync();
    }

    private async Task SendVerificationCodeAsync()
    {
        // Очищаем предыдущие ошибки
        HasPhoneError = false;
        PhoneError = null;
        ClearMessages();

        // Валидация телефона
        if (string.IsNullOrWhiteSpace(Phone))
        {
            PhoneError = "Введите номер телефона";
            HasPhoneError = true;
            return;
        }

        var phoneTrimmed = Phone.Trim();
        if (string.IsNullOrWhiteSpace(phoneTrimmed))
        {
            PhoneError = "Введите номер телефона";
            HasPhoneError = true;
            return;
        }

        var normalizedPhone = NormalizePhone(phoneTrimmed);

        if (!IsPhoneValid(normalizedPhone))
        {
            PhoneError = "Введите корректный номер телефона (9 цифр)";
            HasPhoneError = true;
            return;
        }
        HasPhoneError = false;

        try
        {
            IsBusy = true;
            ClearMessages();

            _logger?.LogInformation("[RegisterViewModel] Sending verification code to: {Phone}", normalizedPhone);
            var startTime = DateTime.UtcNow;

            var result = await _authService.SendVerificationCodeAsync(normalizedPhone);
            
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogInformation("[RegisterViewModel] Verification code sent in {Duration}ms", duration.TotalMilliseconds);

            // Проверка на null результат
            if (result == null)
            {
                _logger?.LogError("[RegisterViewModel] SendVerificationCodeAsync returned null");
                ShowError("Получен пустой ответ от сервера");
                return;
            }

            if (result.TryGetValue("code", out var codeObj) && codeObj != null)
                DisplayedVerificationCode = codeObj.ToString();
            else if (result.TryGetValue("verification_code", out var codeObj2) && codeObj2 != null)
                DisplayedVerificationCode = codeObj2.ToString();

            IsCodeSent = true;
            IsVerificationStep = true;
            SuccessMessage = "Код сгенерирован. Используйте его для верификации.";
        }
        catch (NetworkException ex)
        {
            ShowError("Ошибка сети. Проверьте подключение к интернету.");
            _logger?.LogError(ex, "Network error during code sending");
        }
        catch (BadRequestException ex)
        {
            // Проверяем, не является ли ошибка о том, что пользователь уже зарегистрирован
            if (ex.Message != null && ex.Message.Contains("уже зарегистрирован", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Этот номер телефона уже зарегистрирован. Перейдите на страницу входа или используйте код для восстановления доступа.");
                _logger?.LogInformation("User already registered, suggesting login page");
            }
            else
            {
                ShowError(ParseApiError(ex.Message));
            }
        }
        catch (ApiException ex)
        {
            ShowError($"Ошибка API: {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка отправки кода: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task VerifyCodeAndRegisterAsync()
    {
        // Защита от повторного вызова после успешной регистрации
        if (IsRegistrationSuccessful)
        {
            _logger?.LogWarning("Registration already completed, ignoring duplicate verify-code call");
            return;
        }

        if (IsBusy)
        {
            _logger?.LogWarning("Registration already in progress, ignoring duplicate call");
            return;
        }

        // Очищаем предыдущие ошибки
        HasPhoneError = false;
        PhoneError = null;
        ClearMessages();

        // Validate code
        if (string.IsNullOrWhiteSpace(VerificationCode))
        {
            ShowError("Введите код подтверждения");
            return;
        }
        
        var codeTrimmed = VerificationCode.Trim();
        if (string.IsNullOrWhiteSpace(codeTrimmed) || codeTrimmed.Length < 4)
        {
            ShowError("Введите код подтверждения (минимум 4 символа)");
            return;
        }

        // Validate names
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ShowError("Введите имя");
            return;
        }
        
        var firstNameTrimmed = FirstName.Trim();
        if (string.IsNullOrWhiteSpace(firstNameTrimmed))
        {
            ShowError("Введите имя");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(LastName))
        {
            ShowError("Введите фамилию");
            return;
        }
        
        var lastNameTrimmed = LastName.Trim();
        if (string.IsNullOrWhiteSpace(lastNameTrimmed))
        {
            ShowError("Введите фамилию");
            return;
        }

        // Validate phone
        if (string.IsNullOrWhiteSpace(Phone))
        {
            PhoneError = "Введите номер телефона";
            HasPhoneError = true;
            return;
        }
        
        var phoneTrimmed = Phone.Trim();
        var normalizedPhone = NormalizePhone(phoneTrimmed);
        if (!IsPhoneValid(normalizedPhone))
        {
            PhoneError = "Введите корректный номер телефона";
            HasPhoneError = true;
            return;
        }
        HasPhoneError = false;

        // Validate password
        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Введите пароль");
            return;
        }
        
        var passwordTrimmed = Password.Trim();
        if (string.IsNullOrWhiteSpace(passwordTrimmed))
        {
            ShowError("Введите пароль");
            return;
        }
        
        if (passwordTrimmed.Length < 6)
        {
            ShowError("Пароль должен содержать минимум 6 символов");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ShowError("Подтвердите пароль");
            return;
        }
        
        var confirmPasswordTrimmed = ConfirmPassword.Trim();
        if (passwordTrimmed != confirmPasswordTrimmed)
        {
            ShowError("Пароли не совпадают");
            return;
        }

        // NEW — Validate policy acknowledgment
        if (!IsPolicyAcknowledged)
        {
            ShowError("Вы должны подтвердить, что ознакомлены с политикой использования");
            return;
        }

        try
        {
            IsBusy = true;
            ClearMessages();

            var request = new VerifyCodeRequest
            {
                phone_number = normalizedPhone,
                code = codeTrimmed,
                password = passwordTrimmed,
                first_name = firstNameTrimmed,
                last_name = lastNameTrimmed,
                referral_code = !string.IsNullOrWhiteSpace(ReferralCode) ? ReferralCode.Trim() : null
            };

            _logger?.LogInformation("[RegisterViewModel] Attempting registration for phone: {Phone}", normalizedPhone);
            var startTime = DateTime.UtcNow;

            var response = await _authService.VerifyCodeAndRegisterAsync(request);
            
            var duration = DateTime.UtcNow - startTime;
            var userId = response?.UserId ?? response?.User?.Id ?? 0;
            _logger?.LogInformation("[RegisterViewModel] Registration successful in {Duration}ms. UserId: {UserId}", 
                duration.TotalMilliseconds, userId);
            
            // Проверка на null response
            if (response == null)
            {
                _logger?.LogError("[RegisterViewModel] Registration response is null");
                ShowError("Получен пустой ответ от сервера при регистрации");
                return;
            }
            
            // Проверка на валидный ID пользователя
            if (userId <= 0)
            {
                _logger?.LogWarning("[RegisterViewModel] Registration response has invalid user ID: UserId={UserId}, User.Id={UserId}", 
                    response.UserId, response.User?.Id ?? 0);
            }

            // Устанавливаем флаг успешной регистрации ПЕРЕД вызовом OnRegisterSuccess
            // чтобы предотвратить повторный вызов, если пользователь быстро нажмет кнопку
            IsRegistrationSuccessful = true;
            _logger?.LogInformation("Registration successful for phone: {Phone}, preventing duplicate calls", normalizedPhone);

            if (OnRegisterSuccess is not null)
                await OnRegisterSuccess.Invoke(response);
        }
        catch (NetworkException ex)
        {
            ShowError("Ошибка сети. Проверьте подключение к интернету.");
            _logger?.LogError(ex, "Network error during registration");
        }
        catch (BadRequestException ex)
        {
            // Проверяем, не является ли ошибка о том, что пользователь уже зарегистрирован
            if (ex.Message != null && ex.Message.Contains("уже зарегистрирован", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Этот номер телефона уже зарегистрирован. Перейдите на страницу входа или используйте код для восстановления доступа.");
                _logger?.LogInformation("User already registered during registration, suggesting login page");
            }
            else
            {
                ShowError(ParseApiError(ex.Message));
            }
        }
        catch (ApiException ex)
        {
            ShowError($"Ошибка API: {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowError($"Неизвестная ошибка: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearMessages()
    {
        HasError = false;
        ErrorMessage = null;
        SuccessMessage = null;
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        _logger?.LogWarning("Registration error: {Message}", message);
    }

    private static string ParseApiError(string? msg)
    {
        if (string.IsNullOrWhiteSpace(msg))
            return "Произошла ошибка";
            
        if (msg.StartsWith("Неверный запрос"))
        {
            msg = msg.Replace("Неверный запрос", "").Trim();
            if (msg.StartsWith(":"))
                msg = msg.Substring(1).Trim();
        }
        return string.IsNullOrWhiteSpace(msg) ? "Произошла ошибка" : msg;
    }

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

        return "+996" + digits;
    }

    private static bool IsPhoneValid(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        if (!phone.StartsWith("+")) return false;

        int digits = phone.Count(char.IsDigit);
        return digits >= 7 && digits <= 15;
    }

    public event Func<AuthResponse, Task>? OnRegisterSuccess;
}
