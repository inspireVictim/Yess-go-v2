using Microsoft.Maui.Storage;

namespace YessGoFront.Infrastructure.Auth;

/// <summary>
/// Реализация сервиса аутентификации с использованием SecureStorage
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(AccessTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(RefreshTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveTokensAsync(string accessToken, string? refreshToken = null)
    {
        try
        {
            await SecureStorage.SetAsync(AccessTokenKey, accessToken);
            
            // RefreshToken может быть null, если бэкенд его не возвращает
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
            }
            else
            {
                // Если refreshToken не предоставлен, удаляем старый (если есть)
                SecureStorage.Remove(RefreshTokenKey);
            }
        }
        catch (Exception ex)
        {
            // Логируем ошибку, но не прерываем процесс
            System.Diagnostics.Debug.WriteLine($"[AuthenticationService] Error saving tokens: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        // TODO: Реализовать refresh token логику через API
        // Это будет реализовано после интеграции с бэкендом
        await Task.CompletedTask;
        return false;
    }

    public async Task ClearTokensAsync()
    {
        try
        {
            SecureStorage.Remove(AccessTokenKey);
            SecureStorage.Remove(RefreshTokenKey);
        }
        catch
        {
            // Игнорируем ошибки при очистке
        }
        await Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }
}

