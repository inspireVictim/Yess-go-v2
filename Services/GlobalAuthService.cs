using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Auth;
using YessGoFront.Services.Domain;

namespace YessGoFront.Services;

/// <summary>
/// Глобальный сервис аутентификации для централизованного управления токенами
/// Singleton сервис, доступный из любой страницы MAUI
/// </summary>
public class GlobalAuthService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthService _authService;
    private readonly ILogger<GlobalAuthService>? _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _isRefreshing = false;
    private DateTime? _lastRefreshAttempt = null;
    private const int MinRefreshIntervalSeconds = 5; // Минимальный интервал между попытками refresh

    public GlobalAuthService(
        IAuthenticationService authenticationService,
        IAuthService authService,
        ILogger<GlobalAuthService>? logger = null)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger;
    }

    /// <summary>
    /// Проверяет и обновляет токены при старте приложения или разблокировке
    /// Вызывается автоматически после успешной PIN/биометрической аутентификации
    /// </summary>
    public async Task<bool> EnsureValidTokensAsync(CancellationToken ct = default)
    {
        try
        {
            var accessToken = await _authenticationService.GetAccessTokenAsync();
            var refreshToken = await _authenticationService.GetRefreshTokenAsync();

            // Если нет refresh токена - пользователь должен войти заново
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger?.LogInformation("[GlobalAuthService] No refresh token found, user needs to login");
                return false;
            }

            // Проверяем, валиден ли access token
            bool needsRefresh = false;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                needsRefresh = true;
                _logger?.LogInformation("[GlobalAuthService] Access token missing, refreshing");
            }
            else
            {
                var isValid = JwtHelper.IsTokenValid(accessToken);
                var remainingMinutes = JwtHelper.GetTokenRemainingMinutes(accessToken);

                // Обновляем, если токен невалиден или истекает в течение 5 минут
                if (!isValid || remainingMinutes < 5)
                {
                    needsRefresh = true;
                    _logger?.LogInformation(
                        "[GlobalAuthService] Access token invalid or expiring soon (remaining: {Remaining} min), refreshing",
                        remainingMinutes);
                }
            }

            if (needsRefresh)
            {
                return await RefreshTokensAsync(ct);
            }

            _logger?.LogDebug("[GlobalAuthService] Access token is valid, no refresh needed");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[GlobalAuthService] Error ensuring valid tokens");
            return false;
        }
    }

    /// <summary>
    /// Обновляет access и refresh токены используя текущий refresh token
    /// Защищено от параллельных вызовов через SemaphoreSlim
    /// </summary>
    public async Task<bool> RefreshTokensAsync(CancellationToken ct = default)
    {
        // Защита от параллельных вызовов
        if (!await _refreshLock.WaitAsync(0, ct))
        {
            _logger?.LogDebug("[GlobalAuthService] Refresh already in progress, waiting...");
            await _refreshLock.WaitAsync(ct);
            _logger?.LogDebug("[GlobalAuthService] Refresh completed by another thread");
            return true; // Предполагаем успех, если другой поток уже обновил
        }

        try
        {
            // Проверяем минимальный интервал между попытками
            if (_lastRefreshAttempt.HasValue)
            {
                var timeSinceLastAttempt = DateTime.UtcNow - _lastRefreshAttempt.Value;
                if (timeSinceLastAttempt.TotalSeconds < MinRefreshIntervalSeconds)
                {
                    _logger?.LogDebug(
                        "[GlobalAuthService] Refresh attempted too soon ({Seconds} seconds ago), skipping",
                        timeSinceLastAttempt.TotalSeconds);
                    return true; // Возвращаем true, так как недавно уже обновляли
                }
            }

            _isRefreshing = true;
            _lastRefreshAttempt = DateTime.UtcNow;

            var refreshToken = await _authenticationService.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger?.LogWarning("[GlobalAuthService] Refresh token not found, cannot refresh");
                await _authenticationService.ClearTokensAsync();
                return false;
            }

            _logger?.LogInformation("[GlobalAuthService] Attempting to refresh tokens...");
            var success = await _authService.RefreshTokenAsync(ct);

            if (success)
            {
                _logger?.LogInformation("[GlobalAuthService] Tokens refreshed successfully");
                
                // Проверяем, что токены действительно обновились
                var newAccessToken = await _authenticationService.GetAccessTokenAsync();
                if (string.IsNullOrWhiteSpace(newAccessToken))
                {
                    _logger?.LogWarning("[GlobalAuthService] Access token not found after refresh, clearing tokens");
                    await _authenticationService.ClearTokensAsync();
                    return false;
                }

                return true;
            }
            else
            {
                _logger?.LogWarning("[GlobalAuthService] Token refresh failed, clearing tokens");
                await _authenticationService.ClearTokensAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[GlobalAuthService] Error during token refresh");
            await _authenticationService.ClearTokensAsync();
            return false;
        }
        finally
        {
            _isRefreshing = false;
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Обрабатывает 401 Unauthorized ошибку от API
    /// Автоматически обновляет токен и возвращает true, если успешно
    /// </summary>
    public async Task<bool> HandleUnauthorizedAsync(CancellationToken ct = default)
    {
        _logger?.LogWarning("[GlobalAuthService] Handling 401 Unauthorized error");

        // Если уже обновляем токены, ждем завершения
        if (_isRefreshing)
        {
            _logger?.LogDebug("[GlobalAuthService] Refresh already in progress, waiting...");
            await _refreshLock.WaitAsync(ct);
            _refreshLock.Release();
            return true; // Предполагаем успех
        }

        return await RefreshTokensAsync(ct);
    }

    /// <summary>
    /// Проверяет, аутентифицирован ли пользователь (есть ли валидные токены)
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var refreshToken = await _authenticationService.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            // Если есть refresh token, проверяем access token
            var accessToken = await _authenticationService.GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                // Нет access token, но есть refresh - можем обновить
                return true;
            }

            // Проверяем валидность access token
            return JwtHelper.IsTokenValid(accessToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[GlobalAuthService] Error checking authentication status");
            return false;
        }
    }

    /// <summary>
    /// Получает текущий access token, обновляя его при необходимости
    /// </summary>
    public async Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default)
    {
        try
        {
            var accessToken = await _authenticationService.GetAccessTokenAsync();
            
            // Если токен валиден, возвращаем его
            if (!string.IsNullOrWhiteSpace(accessToken) && JwtHelper.IsTokenValid(accessToken))
            {
                var remainingMinutes = JwtHelper.GetTokenRemainingMinutes(accessToken);
                // Если токен истекает в течение 5 минут, обновляем проактивно
                if (remainingMinutes >= 5)
                {
                    return accessToken;
                }
            }

            // Токен невалиден или скоро истечет - обновляем
            var refreshed = await RefreshTokensAsync(ct);
            if (refreshed)
            {
                return await _authenticationService.GetAccessTokenAsync();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[GlobalAuthService] Error getting valid access token");
            return null;
        }
    }

    /// <summary>
    /// Очищает все токены (выход из системы)
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            _logger?.LogInformation("[GlobalAuthService] Signing out, clearing tokens");
            await _authenticationService.ClearTokensAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[GlobalAuthService] Error during sign out");
        }
    }
}

