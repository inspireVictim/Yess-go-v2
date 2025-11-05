using YessGoFront.Models;
using YessGoFront.Services.Api;

namespace YessGoFront.Services.Domain;

/// <summary>
/// Domain сервис для аутентификации (бизнес-логика)
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Вход в систему. Поддерживает вход по Email или Phone
    /// </summary>
    Task<AuthResponse> LoginAsync(string emailOrPhone, string password, CancellationToken ct = default);
    /// <summary>
    /// Регистрация. После успешной регистрации автоматически выполняет вход
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<bool> RefreshTokenAsync(CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<bool> IsAuthenticatedAsync();
}

