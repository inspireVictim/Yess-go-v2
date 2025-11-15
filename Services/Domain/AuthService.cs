using System.Linq;
using Microsoft.Extensions.Logging;
using YessGoFront.Data;
using YessGoFront.Data.Entities;
using YessGoFront.Infrastructure.Auth;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Models;
using YessGoFront.Services.Api;
using YessGoFront.Services;
using VerifyCodeRequest = YessGoFront.Services.Api.VerifyCodeRequest;

namespace YessGoFront.Services.Domain;

public class AuthService : IAuthService
{
    private readonly IAuthApiService _apiService;
    private readonly IAuthenticationService _authService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AuthService>? _logger;

    public AuthService(
        IAuthApiService apiService,
        IAuthenticationService authService,
        AppDbContext dbContext,
        ILogger<AuthService>? logger = null)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    //Проверка/логин по биометрии
    private readonly BiometricService _biometricService = new();
    private readonly PinStorageService _pinService = new();

    public async Task<bool> AuthenticateWithBiometricsAsync()
    {
        try
        {
            return await _biometricService.AuthenticateAsync("Подтвердите вход в YessGo");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ошибка биометрической аутентификации");
            return false;
        }
    }

    public async Task<bool> ValidatePinAsync(string pin)
    {
        try
        {
            return await _pinService.ValidatePinAsync(pin);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ошибка проверки PIN-кода");
            return false;
        }
    }

    public async Task SavePinAsync(string pin)
    {
        try
        {
            await _pinService.SavePinAsync(pin);
            _logger?.LogInformation("PIN успешно сохранён");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ошибка сохранения PIN-кода");
        }
    }

    public async Task<bool> HasPinAsync()
    {
        try
        {
            var pin = await _pinService.GetPinAsync();
            return !string.IsNullOrWhiteSpace(pin);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ошибка проверки наличия PIN-кода");
            return false;
        }
    }

    public async Task<AuthResponse> LoginWithPhoneAsync(string phone, string password, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Phone and password are required");

            // Нормализуем телефон (добавляем +996 если нужно)
            var normalizedPhone = NormalizePhone(phone);

            var request = new LoginRequest
            {
                Phone = normalizedPhone,
                Password = password
            };

            if (!request.IsValid)
                throw new ArgumentException("Invalid login credentials");

            var response = await _apiService.LoginAsync(request, ct);

            if (response.UserId == 0)
                response.UserId = JwtHelper.GetUserId(response.AccessToken) ?? 0;

            // Сохраняем токены (refreshToken может быть null)
            await _authService.SaveTokensAsync(
                response.AccessToken, 
                string.IsNullOrWhiteSpace(response.RefreshToken) ? null : response.RefreshToken);

            if (response.UserId > 0)
                await SaveOrUpdateUserAsync(response.UserId, response.User, ct);

            _logger?.LogInformation("User logged in: {Phone}, UserId: {UserId}", normalizedPhone, response.UserId);
            return response;
        }
        catch (ApiException) { throw; }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during login");
            throw new NetworkException("Не удалось войти в систему", ex);
        }
    }

    // Старый метод для обратной совместимости
    [Obsolete("Use LoginWithPhoneAsync instead")]
    public async Task<AuthResponse> LoginAsync(string emailOrPhone, string password, CancellationToken ct = default)
    {
        // Если это email - выбрасываем ошибку (больше не поддерживается)
        if (emailOrPhone.Contains("@"))
        {
            throw new ArgumentException("Вход по email больше не поддерживается. Используйте номер телефона.");
        }
        
        // Используем новый метод
        return await LoginWithPhoneAsync(emailOrPhone, password, ct);
    }

    private static string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        // Удаляем все нецифровые символы
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Если начинается с 996, убираем его
        if (digits.StartsWith("996") && digits.Length > 3)
        {
            digits = digits.Substring(3);
        }

        // Если номер начинается с 0, убираем его
        if (digits.StartsWith("0") && digits.Length > 1)
        {
            digits = digits.Substring(1);
        }

        // Возвращаем с префиксом +996
        return "+996" + digits;
    }



    public async Task<Dictionary<string, object>> SendVerificationCodeAsync(string phoneNumber, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number is required");

            var result = await _apiService.SendVerificationCodeAsync(phoneNumber, ct);
            _logger?.LogInformation("Verification code sent to: {Phone}", phoneNumber);
            return result;
        }
        catch (ApiException) { throw; }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending verification code");
            throw new NetworkException("Не удалось отправить код верификации", ex);
        }
    }

    public async Task<AuthResponse> VerifyCodeAndRegisterAsync(VerifyCodeRequest request, CancellationToken ct = default)
    {
        try
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // 1) Проверяем код и завершаем регистрацию
            var registeredUser = await _apiService.VerifyCodeAndRegisterAsync(request, ct);

            // 2) Автоматический логин после успешной регистрации
            var loginRequest = new LoginRequest
            {
                Phone = request.phone_number,
                Password = request.password
            };

            var response = await _apiService.LoginAsync(loginRequest, ct);

            if (response.UserId == 0)
                response.UserId = JwtHelper.GetUserId(response.AccessToken) ?? registeredUser.Id;

            await _authService.SaveTokensAsync(response.AccessToken, response.RefreshToken);

            var userId = response.UserId > 0 ? response.UserId : registeredUser.Id;
            if (userId > 0)
                await SaveOrUpdateUserAsync(userId, registeredUser, ct);

            response.User = registeredUser;

            _logger?.LogInformation("User registered with verification: {Phone}, UserId: {UserId}", request.phone_number, userId);
            return response;
        }
        catch (ApiException) { throw; }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during verification and registration");
            throw new NetworkException("Не удалось завершить регистрацию", ex);
        }
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken ct = default)
    {
        try
        {
            var refreshToken = await _authService.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var response = await _apiService.RefreshTokenAsync(refreshToken, ct);
            await _authService.SaveTokensAsync(response.AccessToken, response.RefreshToken);

            _logger?.LogDebug("Token refreshed successfully");
            return true;
        }
        catch
        {
            await _authService.ClearTokensAsync();
            return false;
        }
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try { await _apiService.LogoutAsync(ct); }
        catch (Exception ex) { _logger?.LogWarning(ex, "Error during logout API call"); }
        finally
        {
            await _authService.ClearTokensAsync();
            
            // Удаляем PIN-код при выходе
            try
            {
                await _pinService.ClearPinAsync();
                _logger?.LogInformation("PIN code cleared on logout");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error clearing PIN on logout");
            }
            
            _logger?.LogInformation("User logged out");
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
        => await _authService.IsAuthenticatedAsync();

    private async Task SaveOrUpdateUserAsync(int userId, UserDto? userDto, CancellationToken ct)
    {
        try
        {
            var existingUser = await _dbContext.Users.FindAsync(new object[] { userId }, ct);

            if (existingUser != null)
            {
                if (userDto != null)
                {
                    existingUser.Name = userDto.DisplayName;
                    existingUser.Email = userDto.Email;
                    existingUser.Phone = userDto.Phone;
                    existingUser.CityId = userDto.CityId;
                    existingUser.UpdatedAt = DateTime.UtcNow;
                }

                existingUser.LastLoginAt = DateTime.UtcNow;
            }
            else if (userDto != null)
            {
                _dbContext.Users.Add(new User
                {
                    Id = userId,
                    Name = userDto.DisplayName,
                    Email = userDto.Email,
                    Phone = userDto.Phone,
                    CityId = userDto.CityId,
                    IsActive = true,
                    CreatedAt = userDto.CreatedAt != default ? userDto.CreatedAt : DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save user to local DB");
        }
    }
}
