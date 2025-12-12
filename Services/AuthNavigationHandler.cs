using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;

namespace YessGoFront.Services;

/// <summary>
/// Централизованный обработчик успешной аутентификации (логин и регистрация)
/// Обрабатывает обновление AccountStore, проверку PIN и навигацию
/// </summary>
public class AuthNavigationHandler
{
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private readonly IAuthService _authService;
    private readonly ILogger<AuthNavigationHandler>? _logger;
    
    // Кэш результата проверки PIN на время сессии
    private bool? _cachedHasPin;

    public AuthNavigationHandler(IAuthService authService, ILogger<AuthNavigationHandler>? logger = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает успешную аутентификацию (логин или регистрация)
    /// </summary>
    /// <param name="response">Ответ от API с токенами и данными пользователя</param>
    /// <param name="rememberMe">Запомнить пользователя</param>
    /// <param name="ct">Токен отмены</param>
    public async Task HandleSuccessfulAuthAsync(AuthResponse response, bool rememberMe = false, CancellationToken ct = default)
    {
        // Защита от повторных вызовов
        if (!await _navigationLock.WaitAsync(0, ct))
        {
            _logger?.LogWarning("[AuthNavigationHandler] Navigation already in progress, ignoring duplicate call");
            return;
        }

        try
        {
            // Проверка на null response
            if (response == null)
            {
                _logger?.LogError("[AuthNavigationHandler] Auth response is null");
                await ShowErrorOnMainThreadAsync("Получен пустой ответ от сервера. Попробуйте снова.");
                return;
            }

            _logger?.LogInformation("[AuthNavigationHandler] Processing successful auth for UserId: {UserId}, Phone: {Phone}", 
                response.UserId, response.User?.Phone ?? "unknown");

            // Проверка на валидный токен
            if (string.IsNullOrWhiteSpace(response.AccessToken))
            {
                _logger?.LogError("[AuthNavigationHandler] AccessToken is null or empty");
                await ShowErrorOnMainThreadAsync("Не получен токен доступа. Попробуйте снова.");
                return;
            }

            _logger?.LogDebug("[AuthNavigationHandler] Step 1: Updating AccountStore...");
            // Обновляем AccountStore
            await UpdateAccountStoreAsync(response, rememberMe);

            _logger?.LogDebug("[AuthNavigationHandler] Step 2: Checking PIN...");
            // Проверяем PIN с таймаутом
            var hasPin = await CheckPinWithTimeoutAsync(ct);

            _logger?.LogDebug("[AuthNavigationHandler] Step 3: Navigating... HasPin: {HasPin}", hasPin);
            // Выполняем навигацию
            await NavigateAsync(hasPin);
            
            _logger?.LogInformation("[AuthNavigationHandler] Successfully processed auth and navigated. UserId: {UserId}", response.UserId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[AuthNavigationHandler] Error processing successful auth: {Message}", ex.Message);
            await ShowErrorOnMainThreadAsync($"Произошла ошибка: {ex.Message}");
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    private async Task UpdateAccountStoreAsync(AuthResponse response, bool rememberMe)
    {
        try
        {
            var user = response.User;
            var email = user?.Email ?? user?.Phone ?? string.Empty;
            var firstName = user?.FirstName ?? string.Empty;
            var lastName = user?.LastName ?? string.Empty;
            var phone = user?.Phone ?? string.Empty;

            AccountStore.Instance.SignIn(
                email,
                firstName,
                lastName,
                rememberMe,
                phone
            );

            _logger?.LogInformation("[AuthNavigationHandler] AccountStore updated. RememberMe: {RememberMe}", rememberMe);
            // Убрана избыточная проверка IsSignedIn - SignIn всегда устанавливает флаг
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[AuthNavigationHandler] Error updating AccountStore: {Message}", ex.Message);
            // Не критично, продолжаем
        }
    }

    private async Task<bool> CheckPinWithTimeoutAsync(CancellationToken ct)
    {
        try
        {
            // Используем кэш, если результат уже был получен
            if (_cachedHasPin.HasValue)
            {
                _logger?.LogDebug("[AuthNavigationHandler] Using cached PIN result: {HasPin}", _cachedHasPin.Value);
                return _cachedHasPin.Value;
            }

            // Используем Task.WhenAny с таймаутом для HasPinAsync (2 секунды вместо 5)
            var pinTask = _authService.HasPinAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), ct);
            var completedTask = await Task.WhenAny(pinTask, timeoutTask);

            if (completedTask == pinTask)
            {
                var hasPin = await pinTask;
                _cachedHasPin = hasPin; // Кэшируем результат
                _logger?.LogInformation("[AuthNavigationHandler] PIN check completed. HasPin: {HasPin}", hasPin);
                return hasPin;
            }
            else
            {
                _logger?.LogWarning("[AuthNavigationHandler] PIN check timed out after 2 seconds, assuming no PIN");
                _cachedHasPin = false; // Кэшируем результат (нет PIN)
                return false; // При таймауте предполагаем, что PIN нет (безопасное значение)
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[AuthNavigationHandler] Error checking PIN: {Message}", ex.Message);
            _cachedHasPin = false; // Кэшируем результат (нет PIN)
            return false; // При ошибке предполагаем, что PIN нет
        }
    }

    private async Task NavigateAsync(bool hasPin)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                var shell = Shell.Current;
                if (shell == null)
                {
                    _logger?.LogError("[AuthNavigationHandler] Shell.Current is null");
                    await ShowErrorOnMainThreadAsync("Не удалось выполнить навигацию. Перезапустите приложение.");
                    return;
                }

                // Оптимизировано: определяем маршрут напрямую без промежуточной переменной
                var navigationRoute = !hasPin ? "///pinlogin?isCreatingPin=true" : "///pinlogin";
                _logger?.LogInformation("[AuthNavigationHandler] Navigating to: {Route} (HasPin: {HasPin})", navigationRoute, hasPin);

                await shell.GoToAsync(navigationRoute, animate: true);
                _logger?.LogInformation("[AuthNavigationHandler] Navigation completed successfully");
            }
            catch (Exception navEx)
            {
                _logger?.LogError(navEx, "[AuthNavigationHandler] Navigation error: {Message}", navEx.Message);
                await ShowErrorOnMainThreadAsync($"Не удалось перейти на следующий экран: {navEx.Message}");
            }
        });
    }

    private async Task ShowErrorOnMainThreadAsync(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                // Оптимизировано: используем Application.Current.MainPage напрямую
                var mainPage = Application.Current?.MainPage;
                if (mainPage != null)
                {
                    await mainPage.DisplayAlert("Ошибка", message, "OK");
                }
            }
            catch
            {
                // Игнорируем ошибки при показе алерта
            }
        });
    }
}

