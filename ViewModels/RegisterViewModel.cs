using System;
using System.Collections.Generic;
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
    private readonly SemaphoreSlim _registerLock = new(1, 1);

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
    [ObservableProperty] private string? firstNameError;
    [ObservableProperty] private bool hasFirstNameError = false;
    [ObservableProperty] private string? lastNameError;
    [ObservableProperty] private bool hasLastNameError = false;
    [ObservableProperty] private string? passwordError;
    [ObservableProperty] private bool hasPasswordError = false;
    [ObservableProperty] private string? confirmPasswordError;
    [ObservableProperty] private bool hasConfirmPasswordError = false;
    [ObservableProperty] private string? verificationCodeError;
    [ObservableProperty] private bool hasVerificationCodeError = false;
    [ObservableProperty] private bool isPolicyAcknowledged = false;

    // SMS verification
    [ObservableProperty] private string verificationCode = string.Empty;
    [ObservableProperty] private bool isVerificationStep = false;
    [ObservableProperty] private bool isCodeSent = false;
    [ObservableProperty] private string? successMessage;
    [ObservableProperty] private string? displayedVerificationCode;
    
    // Реферальный код из URL
    [ObservableProperty] private string? referralCode;
    
    // Флаг успешной регистрации - предотвращает повторный вызов
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
            _logger?.LogWarning("[RegisterViewModel] Registration already completed, ignoring duplicate call");
            return;
        }

        // Защита от повторных вызовов
        if (!await _registerLock.WaitAsync(0))
        {
            _logger?.LogWarning("[RegisterViewModel] Registration already in progress, ignoring duplicate call");
            return;
        }

        try
        {
            // Step 1: request code
            if (!IsVerificationStep)
            {
                await SendVerificationCodeAsync();
                // _registerLock освобождается в SendVerificationCodeAsync после успешной отправки кода
                // чтобы пользователь мог продолжить регистрацию
                return;
            }

            // Step 2: register
            await VerifyCodeAndRegisterAsync();
            // _registerLock освобождается в VerifyCodeAndRegisterAsync после успешной регистрации
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[RegisterViewModel] Unexpected error in RegisterAsync: {Message}", ex.Message);
            ShowError($"Произошла ошибка: {ex.Message}");
            IsBusy = false;
            _registerLock.Release();
        }
    }

    private async Task SendVerificationCodeAsync()
    {
        // Очищаем ошибки
        ClearErrors();

        _logger?.LogInformation("[RegisterViewModel] SendVerificationCodeAsync: Starting. Lock acquired.");

        // Валидация всех обязательных полей перед отправкой кода
        var validationErrors = new List<string>();

        var firstNameError = ValidateFirstName();
        if (firstNameError != null)
        {
            FirstNameError = firstNameError;
            HasFirstNameError = true;
            validationErrors.Add(firstNameError);
        }

        var lastNameError = ValidateLastName();
        if (lastNameError != null)
        {
            LastNameError = lastNameError;
            HasLastNameError = true;
            validationErrors.Add(lastNameError);
        }

        var phoneError = ValidatePhone();
        if (phoneError != null)
        {
            PhoneError = phoneError;
            HasPhoneError = true;
            validationErrors.Add(phoneError);
        }

        var passwordError = ValidatePassword();
        if (passwordError != null)
        {
            PasswordError = passwordError;
            HasPasswordError = true;
            validationErrors.Add(passwordError);
        }

        var confirmPasswordError = ValidateConfirmPassword();
        if (confirmPasswordError != null)
        {
            ConfirmPasswordError = confirmPasswordError;
            HasConfirmPasswordError = true;
            validationErrors.Add(confirmPasswordError);
        }

        if (validationErrors.Count > 0)
        {
            ShowError(string.Join("\n", validationErrors));
            _logger?.LogWarning("[RegisterViewModel] Validation failed. Errors: {Errors}. IsBusy set to false, lock released.", string.Join("; ", validationErrors));
            IsBusy = false;
            _registerLock.Release();
            return;
        }

        var phoneTrimmed = Phone.Trim();
        var normalizedPhone = NormalizePhone(phoneTrimmed);

        try
        {
            IsBusy = true;
            _logger?.LogInformation("[RegisterViewModel] IsBusy set to true. Sending verification code to: {Phone}", normalizedPhone);
            ClearMessages();

            var startTime = DateTime.UtcNow;

            var result = await _authService.SendVerificationCodeAsync(normalizedPhone);

            if (result.TryGetValue("code", out var codeObj) && codeObj != null)
                DisplayedVerificationCode = codeObj.ToString();
            else if (result.TryGetValue("verification_code", out var codeObj2) && codeObj2 != null)
                DisplayedVerificationCode = codeObj2.ToString();

            IsCodeSent = true;
            IsVerificationStep = true;
            SuccessMessage = "Код отправлен. Введите его для завершения регистрации.";
            
            // Сбрасываем IsBusy после успешной отправки кода, чтобы кнопка стала активной для второго этапа
            IsBusy = false;
            _logger?.LogInformation("[RegisterViewModel] Verification code sent successfully. IsBusy set to false, lock released. User can now proceed with registration.");
            
            // Освобождаем lock после успешной отправки кода, чтобы пользователь мог продолжить
            _registerLock.Release();
        }
        catch (OperationCanceledException)
        {
            ShowError("Операция отправки кода заняла слишком много времени. Проверьте подключение к интернету и попробуйте снова.");
            _logger?.LogWarning("[RegisterViewModel] Send verification code operation timed out after 15 seconds. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("[RegisterViewModel] Send verification code operation timed out after 15 seconds");
            ShowError("Операция отправки кода заняла слишком много времени. Проверьте подключение к интернету и попробуйте снова.");
        }
        catch (NetworkException ex)
        {
            var errorMessage = ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                ? "Сервер не отвечает. Проверьте подключение к интернету."
                : "Ошибка сети. Проверьте подключение к интернету.";
            ShowError(errorMessage);
            _logger?.LogError(ex, "[RegisterViewModel] Network error during code sending: {Message}. IsBusy set to false, lock released.", ex.Message);
            IsBusy = false;
            _registerLock.Release();
        }
        catch (BadRequestException ex)
        {
            if (ex.Message != null && ex.Message.Contains("уже зарегистрирован", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Этот номер телефона уже зарегистрирован. Перейдите на страницу входа.");
            }
            else
            {
                ShowError(ParseApiError(ex.Message));
            }
            _logger?.LogWarning(ex, "[RegisterViewModel] Bad request during code sending. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
        catch (ApiException ex)
        {
            ShowError($"Ошибка API: {ex.Message}");
            _logger?.LogError(ex, "[RegisterViewModel] API error during code sending. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка отправки кода: {ex.Message}");
            _logger?.LogError(ex, "[RegisterViewModel] Unexpected error during code sending. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
    }

    private async Task VerifyCodeAndRegisterAsync()
    {
        // Защита от повторного вызова после успешной регистрации
        if (IsRegistrationSuccessful)
        {
            _logger?.LogWarning("[RegisterViewModel] Registration already completed, ignoring duplicate verify-code call. Lock released.");
            IsBusy = false;
            _registerLock.Release();
            return;
        }

        _logger?.LogInformation("[RegisterViewModel] VerifyCodeAndRegisterAsync: Starting. Lock acquired.");

        // Очищаем ошибки
        ClearErrors();

        // Валидация всех полей
        var validationErrors = new List<string>();

        var firstNameError = ValidateFirstName();
        if (firstNameError != null)
        {
            FirstNameError = firstNameError;
            HasFirstNameError = true;
            validationErrors.Add(firstNameError);
        }

        var lastNameError = ValidateLastName();
        if (lastNameError != null)
        {
            LastNameError = lastNameError;
            HasLastNameError = true;
            validationErrors.Add(lastNameError);
        }

        var phoneError = ValidatePhone();
        if (phoneError != null)
        {
            PhoneError = phoneError;
            HasPhoneError = true;
            validationErrors.Add(phoneError);
        }

        var passwordError = ValidatePassword();
        if (passwordError != null)
        {
            PasswordError = passwordError;
            HasPasswordError = true;
            validationErrors.Add(passwordError);
        }

        var confirmPasswordError = ValidateConfirmPassword();
        if (confirmPasswordError != null)
        {
            ConfirmPasswordError = confirmPasswordError;
            HasConfirmPasswordError = true;
            validationErrors.Add(confirmPasswordError);
        }

        var verificationCodeError = ValidateVerificationCode();
        if (verificationCodeError != null)
        {
            VerificationCodeError = verificationCodeError;
            HasVerificationCodeError = true;
            validationErrors.Add(verificationCodeError);
        }

        if (!IsPolicyAcknowledged)
        {
            validationErrors.Add("Вы должны подтвердить, что ознакомлены с политикой использования");
        }

        if (validationErrors.Count > 0)
        {
            ShowError(string.Join("\n", validationErrors));
            _logger?.LogWarning("[RegisterViewModel] Validation failed. Errors: {Errors}. IsBusy set to false, lock released.", string.Join("; ", validationErrors));
            IsBusy = false;
            _registerLock.Release();
            return;
        }

        // Все поля валидны, продолжаем регистрацию
        var codeTrimmed = VerificationCode.Trim();
        var firstNameTrimmed = FirstName.Trim();
        var lastNameTrimmed = LastName.Trim();
        var phoneTrimmed = Phone.Trim();
        var normalizedPhone = NormalizePhone(phoneTrimmed);
        var passwordTrimmed = Password.Trim();

        try
        {
            IsBusy = true;
            _logger?.LogInformation("[RegisterViewModel] All fields validated. IsBusy set to true. Starting registration.");
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

            // Устанавливаем флаг успешной регистрации ПЕРЕД вызовом OnRegisterSuccess
            IsRegistrationSuccessful = true;
            _logger?.LogInformation("[RegisterViewModel] Registration successful for phone: {Phone}. IsRegistrationSuccessful set to true. Calling OnRegisterSuccess.", normalizedPhone);

            // Вызываем обработчик успешной регистрации
            // IsBusy остается true до завершения навигации, lock освобождается после
            if (OnRegisterSuccess != null)
            {
                try
                {
                    await OnRegisterSuccess.Invoke(response);
                    _logger?.LogInformation("[RegisterViewModel] OnRegisterSuccess completed. IsBusy set to false, lock released.");
                }
                catch (Exception navEx)
                {
                    _logger?.LogError(navEx, "[RegisterViewModel] Error in OnRegisterSuccess: {Message}", navEx.Message);
                }
                finally
                {
                    // Освобождаем lock после завершения обработки (включая навигацию)
                    IsBusy = false;
                    _registerLock.Release();
                }
            }
            else
            {
                _logger?.LogWarning("[RegisterViewModel] OnRegisterSuccess is null. IsBusy set to false, lock released.");
                IsBusy = false;
                _registerLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            ShowError("Операция регистрации заняла слишком много времени. Проверьте подключение к интернету и попробуйте снова.");
            _logger?.LogWarning("[RegisterViewModel] Registration operation timed out after 15 seconds. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("[RegisterViewModel] Registration operation timed out after 15 seconds");
            ShowError("Операция регистрации заняла слишком много времени. Проверьте подключение к интернету и попробуйте снова.");
        }
        catch (NetworkException ex)
        {
            var errorMessage = ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                ? "Сервер не отвечает. Проверьте подключение к интернету."
                : "Ошибка сети. Проверьте подключение к интернету.";
            ShowError(errorMessage);
            _logger?.LogError(ex, "[RegisterViewModel] Network error during registration: {Message}. IsBusy set to false, lock released.", ex.Message);
            IsBusy = false;
            _registerLock.Release();
        }
        catch (BadRequestException ex)
        {
            if (ex.Message != null && ex.Message.Contains("уже зарегистрирован", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Этот номер телефона уже зарегистрирован. Перейдите на страницу входа.");
            }
            else
            {
                ShowError(ParseApiError(ex.Message));
            }
            _logger?.LogWarning(ex, "[RegisterViewModel] Bad request during registration. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
        catch (UnauthorizedException ex)
        {
            ShowError("Ошибка авторизации. Проверьте правильность введенных данных.");
            _logger?.LogWarning(ex, "[RegisterViewModel] Unauthorized during registration. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
        catch (ApiException ex)
        {
            ShowError($"Ошибка API: {ex.Message}");
            _logger?.LogError(ex, "[RegisterViewModel] API error during registration. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
        catch (Exception ex)
        {
            ShowError($"Неизвестная ошибка: {ex.Message}");
            _logger?.LogError(ex, "[RegisterViewModel] Unexpected error during registration. IsBusy set to false, lock released.");
            IsBusy = false;
            _registerLock.Release();
        }
    }

    // Упрощенная валидация - возвращает ошибку или null
    private string? ValidateFirstName()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
            return "Введите имя";
        
        if (string.IsNullOrWhiteSpace(FirstName.Trim()))
            return "Введите имя";
        
        return null;
    }

    private string? ValidateLastName()
    {
        if (string.IsNullOrWhiteSpace(LastName))
            return "Введите фамилию";
        
        if (string.IsNullOrWhiteSpace(LastName.Trim()))
            return "Введите фамилию";
        
        return null;
    }

    private string? ValidatePhone()
    {
        if (string.IsNullOrWhiteSpace(Phone))
            return "Введите номер телефона";

        var phoneTrimmed = Phone.Trim();
        // Убрана избыточная проверка - если Phone не пустой, то phoneTrimmed тоже не будет пустым после Trim()
        var normalizedPhone = NormalizePhone(phoneTrimmed);
        if (!IsPhoneValid(normalizedPhone))
            return "Введите корректный номер телефона (9 цифр)";

        return null;
    }

    private string? ValidatePassword()
    {
        if (string.IsNullOrWhiteSpace(Password))
            return "Введите пароль";

        var passwordTrimmed = Password.Trim();
        // Убрана избыточная проверка - если Password не пустой, то passwordTrimmed тоже не будет пустым после Trim()
        if (passwordTrimmed.Length < 6)
            return "Пароль должен содержать минимум 6 символов";

        return null;
    }

    private string? ValidateConfirmPassword()
    {
        if (string.IsNullOrWhiteSpace(ConfirmPassword))
            return "Подтвердите пароль";

        var confirmPasswordTrimmed = ConfirmPassword.Trim();
        var passwordTrimmed = Password.Trim();
        
        // Проверяем, что основной пароль не пустой
        if (string.IsNullOrWhiteSpace(passwordTrimmed))
            return "Сначала введите пароль";
        
        if (passwordTrimmed != confirmPasswordTrimmed)
            return "Пароли не совпадают";

        return null;
    }

    private string? ValidateVerificationCode()
    {
        if (string.IsNullOrWhiteSpace(VerificationCode))
            return "Введите код подтверждения";

        var codeTrimmed = VerificationCode.Trim();
        if (string.IsNullOrWhiteSpace(codeTrimmed) || codeTrimmed.Length < 4)
            return "Код должен содержать минимум 4 символа";

        return null;
    }

    private void ClearErrors()
    {
        HasError = false;
        ErrorMessage = null;
        HasPhoneError = false;
        PhoneError = null;
        HasFirstNameError = false;
        FirstNameError = null;
        HasLastNameError = false;
        LastNameError = null;
        HasPasswordError = false;
        PasswordError = null;
        HasConfirmPasswordError = false;
        ConfirmPasswordError = null;
        HasVerificationCodeError = false;
        VerificationCodeError = null;
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
