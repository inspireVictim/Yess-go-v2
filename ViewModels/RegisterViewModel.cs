using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Services.Domain;
using YessGoFront.Services.Api;
using RegisterRequestDto = YessGoFront.Services.Api.RegisterRequest;
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
    
    // SMS верификация
    [ObservableProperty] private string verificationCode = string.Empty;
    [ObservableProperty] private bool isVerificationStep = false;
    [ObservableProperty] private bool isCodeSent = false;
    [ObservableProperty] private string? successMessage;

    public RegisterViewModel(IAuthService authService, ILogger<RegisterViewModel>? logger = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy)
            return;

        // Если еще не отправлен код - отправляем
        if (!IsVerificationStep)
        {
            await SendVerificationCodeAsync();
            return;
        }

        // Если код отправлен - проверяем и регистрируем
        await VerifyCodeAndRegisterAsync();
    }

    private async Task SendVerificationCodeAsync()
    {
        // Валидация телефона
        var normalizedPhone = NormalizePhone(Phone);
        if (!IsPhoneValid(normalizedPhone))
        {
            PhoneError = "Введите корректный номер телефона (например, +996555123456)";
            HasPhoneError = true;
            return;
        }
        HasPhoneError = false;

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = null;
            SuccessMessage = null;

            _logger?.LogInformation("Sending verification code to: {Phone}", normalizedPhone);

            await _authService.SendVerificationCodeAsync(normalizedPhone);
            
            IsCodeSent = true;
            IsVerificationStep = true;
            SuccessMessage = "Код отправлен на ваш номер телефона";
            
            _logger?.LogInformation("Verification code sent successfully");
        }
        catch (NetworkException ex)
        {
            ShowError("Ошибка сети. Проверьте подключение к интернету.");
            _logger?.LogError(ex, "Network error during code sending");
        }
        catch (BadRequestException ex)
        {
            var errorMessage = ex.Message;
            if (errorMessage.StartsWith("Неверный запрос"))
            {
                errorMessage = errorMessage.Replace("Неверный запрос", "").Trim();
                if (errorMessage.StartsWith(":"))
                    errorMessage = errorMessage.Substring(1).Trim();
            }
            ShowError(string.IsNullOrWhiteSpace(errorMessage) ? "Ошибка отправки кода" : errorMessage);
            _logger?.LogWarning(ex, "Bad request during code sending: {Message}", errorMessage);
        }
        catch (ApiException ex)
        {
            ShowError($"Ошибка API: {ex.Message}");
            _logger?.LogWarning(ex, "API error during code sending");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка отправки кода: {ex.Message}");
            _logger?.LogError(ex, "Unexpected error during code sending");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task VerifyCodeAndRegisterAsync()
    {
        // Валидация кода
        if (string.IsNullOrWhiteSpace(VerificationCode) || VerificationCode.Length < 4)
        {
            ShowError("Введите код подтверждения");
            return;
        }

        // Валидация остальных полей
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ShowError("Введите имя");
            return;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            ShowError("Введите фамилию");
            return;
        }

        var normalizedPhone = NormalizePhone(Phone);
        if (!IsPhoneValid(normalizedPhone))
        {
            PhoneError = "Введите корректный номер телефона";
            HasPhoneError = true;
            return;
        }
        HasPhoneError = false;

        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Введите пароль");
            return;
        }

        if (Password.Length < 6)
        {
            ShowError("Пароль должен содержать минимум 6 символов");
            return;
        }

        if (Password != ConfirmPassword)
        {
            ShowError("Пароли не совпадают");
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = null;
            SuccessMessage = null;

            var request = new VerifyCodeRequest
            {
                phone_number = normalizedPhone,
                code = VerificationCode,
                password = Password,
                first_name = FirstName.Trim(),
                last_name = LastName.Trim()
            };

            _logger?.LogInformation("Attempting verification and registration for phone: {Phone}", normalizedPhone);

            var response = await _authService.VerifyCodeAndRegisterAsync(request);
            _logger?.LogInformation("Registration successful. UserId: {UserId}", response.UserId);

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
            var errorMessage = ex.Message;
            if (errorMessage.StartsWith("Неверный запрос"))
            {
                errorMessage = errorMessage.Replace("Неверный запрос", "").Trim();
                if (errorMessage.StartsWith(":"))
                    errorMessage = errorMessage.Substring(1).Trim();
            }
            ShowError(string.IsNullOrWhiteSpace(errorMessage) ? "Ошибка регистрации" : errorMessage);
            _logger?.LogWarning(ex, "Bad request during registration: {Message}", errorMessage);
        }
        catch (ApiException ex)
        {
            ShowError($"Ошибка API: {ex.Message}");
            _logger?.LogWarning(ex, "API error during registration");
        }
        catch (Exception ex)
        {
            ShowError($"Неизвестная ошибка: {ex.Message}");
            _logger?.LogError(ex, "Unexpected error during registration");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        _logger?.LogWarning("Registration error: {Message}", message);
    }

    private static string NormalizePhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var s = input.Trim();
        s = s.StartsWith("+") ? s : "+" + s;

        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsDigit(ch) || ch == '+')
                sb.Append(ch);
        }

        return sb.ToString();
    }

    private static bool IsPhoneValid(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        if (!phone.StartsWith("+")) return false;

        int digits = 0;
        for (int i = 0; i < phone.Length; i++)
        {
            if (char.IsDigit(phone[i]))
                digits++;
            else if (i != 0)
                return false;
        }

        return digits >= 7 && digits <= 15;
    }

    public event Func<AuthResponse, Task>? OnRegisterSuccess;
}
