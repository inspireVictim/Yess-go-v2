using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Services.Domain;
using YessGoFront.Services.Api;

namespace YessGoFront.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginViewModel>? _logger;

    [ObservableProperty] private string emailOrPhone = string.Empty;
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
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(EmailOrPhone))
        {
            ShowError("Введите Email или номер телефона");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Введите пароль");
            return;
        }

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = null;

            _logger?.LogInformation("Attempting login for: {EmailOrPhone}", EmailOrPhone);

            var response = await _authService.LoginAsync(EmailOrPhone, Password);
            _logger?.LogInformation("Login successful. UserId: {UserId}", response.UserId);

            if (OnLoginSuccess is not null)
                await OnLoginSuccess.Invoke(response);
        }
        catch (NetworkException ex)
        {
            ShowError("Ошибка сети. Проверьте интернет-соединение.");
            _logger?.LogError(ex, "Network error during login");
        }
        catch (UnauthorizedException ex)
        {
            ShowError("Неверный Email/телефон или пароль");
            _logger?.LogWarning(ex, "Unauthorized login attempt");
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
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        _logger?.LogWarning("Login error: {Message}", message);
    }

    public event Func<AuthResponse, Task>? OnLoginSuccess;
}
