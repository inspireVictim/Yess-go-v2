using YessGoFront.Models;

namespace YessGoFront.Services.Api;

/// <summary>
/// API сервис для аутентификации
/// </summary>
public interface IAuthApiService
{
    /// <summary>
    /// Логин
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Регистрация
    /// Возвращает данные пользователя (UserDto), но не токены
    /// </summary>
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>
    /// Обновить токен
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Выход
    /// </summary>
    Task LogoutAsync(CancellationToken ct = default);

    /// <summary>
    /// Верификация кода
    /// </summary>
    Task<AuthResponse> VerifyCodeAsync(string code, CancellationToken ct = default);
}

/// <summary>
/// Запрос на логин
/// Поддерживает вход по Email или Phone
/// </summary>
public class LoginRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Определяет, что использовать для логина (Email или Phone)
    /// </summary>
    public string Username => !string.IsNullOrWhiteSpace(Phone) ? Phone : Email ?? string.Empty;
    
    /// <summary>
    /// Валидация: должен быть указан либо Email, либо Phone
    /// </summary>
    public bool IsValid => (!string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Phone)) 
                           && !string.IsNullOrWhiteSpace(Password);
}

/// <summary>
/// Запрос на регистрацию
/// Соответствует UserCreate схеме бэкенда
/// </summary>
/// <summary>
/// Запрос на регистрацию
/// Соответствует UserCreate схеме бэкенда
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Номер телефона (обязательно)
    /// </summary>
    public string phone_number { get; set; } = string.Empty;

    /// <summary>
    /// Пароль (обязательно)
    /// </summary>
    public string password { get; set; } = string.Empty;

    /// <summary>
    /// Имя (обязательно)
    /// </summary>
    public string first_name { get; set; } = string.Empty;

    /// <summary>
    /// Фамилия (обязательно)
    /// </summary>
    public string last_name { get; set; } = string.Empty;
}


/// <summary>
/// Ответ на аутентификацию
/// Соответствует TokenResponse схеме бэкенда
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "bearer";
    public int UserId { get; set; }
    
    /// <summary>
    /// Данные пользователя (могут быть в отдельном запросе /me)
    /// </summary>
    public UserDto? User { get; set; }
}

