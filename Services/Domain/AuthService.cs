using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
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

    // Биометрия + PIN
    private readonly BiometricService _biometricService = new();
    private readonly PinStorageService _pinService = new();

    // Кэш для GetLocalUserAsync с TTL 2 секунды
    private User? _cachedLocalUser;
    private DateTime _cacheTimestamp = DateTime.MinValue;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(2);
    private readonly object _cacheLock = new object();

    /// <summary>
    /// Инвалидировать кэш локального пользователя
    /// </summary>
    private void InvalidateLocalUserCache()
    {
        lock (_cacheLock)
        {
            _cachedLocalUser = null;
            _cacheTimestamp = DateTime.MinValue;
        }
    }

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
            var hasValidPin = await _pinService.ValidateStoredPinOrReset();

            System.Diagnostics.Debug.WriteLine($"[AuthService] HasPinAsync: hasValidPin={hasValidPin}");
            return hasValidPin;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ошибка проверки наличия PIN-кода");
            return false;
        }
    }

    public Task<int?> GetCurrentUserIdAsync()
    {
        try
        {
            int storedId = Preferences.Get("UserId", -1);

            if (storedId == -1)
                return Task.FromResult<int?>(null);

            return Task.FromResult<int?>(storedId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting current user ID");
            return Task.FromResult<int?>(null);
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
            _logger?.LogDebug("Saving tokens. AccessToken: {HasAccess}, RefreshToken: {HasRefresh}", 
                !string.IsNullOrEmpty(response.AccessToken), 
                !string.IsNullOrEmpty(response.RefreshToken));
            await _authService.SaveTokensAsync(
                response.AccessToken,
                string.IsNullOrWhiteSpace(response.RefreshToken) ? null : response.RefreshToken);
            
            // Проверяем, что refresh token сохранился
            var savedRefreshToken = await _authService.GetRefreshTokenAsync();
            _logger?.LogInformation("Refresh token saved: {Saved}", !string.IsNullOrEmpty(savedRefreshToken));

            if (response.UserId > 0)
            {
                // Сначала сохраняем данные из response.User (если есть)
                await SaveOrUpdateUserAsync(response.UserId, response.User, ct);
                
                // Затем пытаемся получить полный профиль пользователя через API /me в фоне
                // Это гарантирует, что у нас будут актуальные данные (FirstName, LastName)
                // Выполняем неблокирующе, чтобы не замедлять процесс входа
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Используем короткий таймаут (5 секунд) для запроса профиля
                        using var profileCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        _logger?.LogDebug("Fetching full user profile from /me endpoint in background...");
                        var userProfile = await _apiService.GetMeAsync(profileCts.Token);
                        if (userProfile != null)
                        {
                            _logger?.LogDebug("[AuthService] Got user profile from /me: Id={Id}, FirstName={FirstName}, LastName={LastName}, Phone={Phone}", 
                                userProfile.Id, userProfile.FirstName, userProfile.LastName, userProfile.Phone);
                            await SaveOrUpdateUserAsync(response.UserId, userProfile, profileCts.Token);
                        }
                        else
                        {
                            _logger?.LogWarning("GetMeAsync returned null profile");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger?.LogDebug("GetMeAsync timed out, using data from login response");
                    }
                    catch (Exception ex)
                    {
                        // Не критично - используем данные из response.User
                        _logger?.LogWarning(ex, "Failed to fetch full user profile, using data from login response");
                    }
                });
            }

            _logger?.LogInformation("User logged in: {Phone}, UserId: {UserId}", normalizedPhone, response.UserId);
            Preferences.Set("UserId", response.UserId);
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

            // Create welcome notification for the new user
            if (userId > 0)
            {
                var welcomeNotification = new Notification
                {
                    UserId = userId, // Уведомление для нового пользователя
                    Title = "Добро пожаловать в YESS!GO",
                    Message = "Спасибо за регистрацию в приложении YESS!GO. Желаем приятного пользования!",
                    NotificationType = NotificationType.InApp,
                    Priority = NotificationPriority.Normal,
                    Status = NotificationStatus.Delivered,
                    CreatedAt = DateTime.UtcNow,
                    DeliveredAt = DateTime.UtcNow
                };

                await _dbContext.Notifications.AddAsync(welcomeNotification, ct);
                await _dbContext.SaveChangesAsync(ct);
                
                _logger?.LogInformation("Welcome notification created for user {UserId}", userId);
            }

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
        try
        {
            // На бэке logout пока не реализован — ок, игнорируем NotSupported
            await _apiService.LogoutAsync(ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error during logout API call");
        }
        finally
        {
            // ✅ Токены и PIN очищаем при выходе
            await _authService.ClearTokensAsync();

            try
            {
                await _pinService.ClearPinAsync();
                System.Diagnostics.Debug.WriteLine("[AuthService] LogoutAsync: PIN cleared on logout");
            }
            catch (Exception pinEx)
            {
                _logger?.LogWarning(pinEx, "Failed to clear PIN on logout");
            }

            // Инвалидируем кэш локального пользователя при выходе
            InvalidateLocalUserCache();

            _logger?.LogInformation("User logged out (tokens and PIN cleared)");
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
        => await _authService.IsAuthenticatedAsync();

    private async Task SaveOrUpdateUserAsync(int userId, UserDto? userDto, CancellationToken ct)
    {
        try
        {
            // Используем AsTracking() для операций записи (по умолчанию NoTracking)
            var existingUser = await _dbContext.Users
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            // Если Phone пустой в userDto, пытаемся получить его из токена
            string? phone = userDto?.Phone;
            if (string.IsNullOrWhiteSpace(phone))
            {
                try
                {
                    var accessToken = await _authService.GetAccessTokenAsync();
                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        var phoneFromToken = JwtHelper.GetPhone(accessToken);
                        if (!string.IsNullOrWhiteSpace(phoneFromToken))
                        {
                            phone = phoneFromToken;
                            _logger?.LogDebug("Using phone from token: {Phone}", phone);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to get phone from token");
                }
            }

            // Формируем имя из FirstName и LastName (не используем DisplayName, так как он может вернуть телефон)
            string? fullName = null;
            if (userDto != null)
            {
                var firstName = userDto.FirstName?.Trim() ?? string.Empty;
                var lastName = userDto.LastName?.Trim() ?? string.Empty;
                fullName = $"{firstName} {lastName}".Trim();
                // Если ФИО пустое, оставляем null (не сохраняем пустую строку)
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = null;
                }
                _logger?.LogDebug("SaveOrUpdateUserAsync: FirstName={FirstName}, LastName={LastName}, FullName={FullName}", 
                    firstName, lastName, fullName ?? "null");
            }

            if (existingUser != null)
            {
                if (userDto != null)
                {
                    // Всегда обновляем Name если есть реальное ФИО (даже если в БД уже было имя)
                    // Также обновляем, если в БД имя равно "Пользователь" или пустое
                    var shouldUpdateName = !string.IsNullOrWhiteSpace(fullName) && 
                                          (string.IsNullOrWhiteSpace(existingUser.Name) || 
                                           existingUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase) ||
                                           !existingUser.Name.Contains(' '));
                    
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        // Принудительно обновляем имя, если оно получено из API и отличается от текущего
                        if (shouldUpdateName || existingUser.Name != fullName)
                        {
                            existingUser.Name = fullName;
                            _logger?.LogInformation("✅ Updated user Name from FirstName/LastName: '{OldName}' -> '{NewName}'", 
                                existingUser.Name ?? "empty", fullName);
                        }
                        else
                        {
                            _logger?.LogDebug("Name not updated: already has valid name '{Name}'", existingUser.Name);
                        }
                    }
                    else
                    {
                        _logger?.LogDebug("Name not updated: FirstName and LastName are empty in API response");
                    }
                    
                    existingUser.Email = userDto.Email;
                    // Обновляем Phone только если он не пустой (либо из userDto, либо из токена)
                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        existingUser.Phone = phone;
                        _logger?.LogDebug("Updated user Phone: {Phone}", phone);
                    }
                    existingUser.CityId = userDto.CityId;
                    existingUser.ReferralCode = userDto.ReferralCode; // Сохраняем реферальный код
                    existingUser.UpdatedAt = DateTime.UtcNow;
                    
                    // Сущность уже отслеживается благодаря AsTracking()
                    // EF автоматически отслеживает изменения
                }

                existingUser.LastLoginAt = DateTime.UtcNow;
                _logger?.LogInformation("User updated in local DB: Id={Id}, Name='{Name}', Phone='{Phone}'", 
                    existingUser.Id, existingUser.Name ?? "empty", existingUser.Phone ?? "empty");
            }
            else if (userDto != null)
            {
                _dbContext.Users.Add(new User
                {
                    Id = userId,
                    Name = fullName ?? string.Empty, // Сохраняем ФИО или пустую строку
                    Email = userDto.Email,
                    Phone = phone ?? string.Empty, // Используем phone из токена, если он был пустой
                    CityId = userDto.CityId,
                    ReferralCode = userDto.ReferralCode, // Сохраняем реферальный код
                    IsActive = true,
                    CreatedAt = userDto.CreatedAt != default ? userDto.CreatedAt : DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                });
                _logger?.LogDebug("Created new user in local DB: Name={Name}, Phone={Phone}", fullName ?? "empty", phone);
            }

            // Сохраняем изменения в БД
            var savedChanges = await _dbContext.SaveChangesAsync(ct);
            _logger?.LogInformation("SaveOrUpdateUserAsync: Saved {Count} changes to database", savedChanges);
            
            // Инвалидируем кэш локального пользователя, так как данные изменились
            InvalidateLocalUserCache();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save user to local DB");
        }
    }

    /// <summary>
    /// Получить пользователя из локальной SQLite БД
    /// Оптимизировано: использует кэш и AsNoTracking для максимальной производительности
    /// </summary>
    public async Task<User?> GetLocalUserAsync(CancellationToken ct = default)
    {
        try
        {
            // Проверяем кэш
            lock (_cacheLock)
            {
                if (_cachedLocalUser != null && DateTime.UtcNow - _cacheTimestamp < _cacheTtl)
                {
                    _logger?.LogDebug("Returning cached local user: ID={UserId}", _cachedLocalUser.Id);
                    return _cachedLocalUser;
                }
            }

            // Получаем первого активного пользователя из локальной БД
            // Используем AsNoTracking для read-only операции (ускоряет запрос на 20-30%)
            // Индекс на IsActive, IsBlocked, LastLoginAt ускоряет этот запрос
            var localUser = await _dbContext.Users
                .AsNoTracking() // Явно указываем NoTracking для read-only операции
                .Where(u => u.IsActive && !u.IsBlocked)
                .OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt)
                .FirstOrDefaultAsync(ct);

            // Обновляем кэш
            lock (_cacheLock)
            {
                _cachedLocalUser = localUser;
                _cacheTimestamp = DateTime.UtcNow;
            }

            if (localUser != null)
            {
                _logger?.LogInformation("Local user found: ID={UserId}, Phone={Phone}", localUser.Id, localUser.Phone);
            }
            else
            {
                _logger?.LogInformation("No local user found in SQLite database");
            }

            return localUser;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting local user from SQLite database");
            return null;
        }
    }

    /// <summary>
    /// Автоматический вход, если пользователь есть на сервере (есть токены), но нет в локальной БД
    /// </summary>
    public async Task<bool> AutoLoginIfNoLocalUserAsync(CancellationToken ct = default)
    {
        try
        {
            // Проверяем, есть ли уже локальный пользователь
            var localUser = await GetLocalUserAsync(ct);
            if (localUser != null)
            {
                _logger?.LogInformation("Local user already exists (ID={UserId}), skipping auto-login", localUser.Id);
                return true;
            }

            // Проверяем, есть ли refresh token (пользователь есть на сервере)
            var refreshToken = await _authService.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger?.LogInformation("No refresh token found, cannot auto-login");
                return false;
            }

            // Пробуем обновить токены
            var tokenRefreshed = await RefreshTokenAsync(ct);
            if (!tokenRefreshed)
            {
                _logger?.LogWarning("Failed to refresh token during auto-login");
                return false;
            }

            // Получаем информацию о пользователе из токена
            try
            {
                var accessToken = await _authService.GetAccessTokenAsync();
                var userIdFromToken = JwtHelper.GetUserId(accessToken);
                if (userIdFromToken == null || userIdFromToken <= 0)
                {
                    _logger?.LogWarning("Cannot get user ID from token");
                    return false;
                }

                var phoneFromToken = JwtHelper.GetPhone(accessToken);
                if (string.IsNullOrWhiteSpace(phoneFromToken))
                {
                    _logger?.LogWarning("Cannot get phone from token");
                    phoneFromToken = "Unknown";
                }

                // Сохраняем пользователя локально с минимальными данными из токена
                // Более полные данные будут обновлены при следующем запросе профиля или при следующем входе
                var tempUserDto = new UserDto
                {
                    Id = userIdFromToken.Value,
                    Phone = phoneFromToken,
                    Email = null,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                await SaveOrUpdateUserAsync(userIdFromToken.Value, tempUserDto, ct);
                
                // Сохраняем UserId в Preferences для совместимости
                Preferences.Set("UserId", userIdFromToken.Value);
                
                _logger?.LogInformation("Auto-login successful, user saved to local DB: UserId={UserId}, Phone={Phone}", 
                    userIdFromToken, phoneFromToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during auto-login: failed to get user info from token");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during auto-login check");
            return false;
        }
    }

    /// <summary>
    /// Получить профиль пользователя из API
    /// </summary>
    public async Task<UserDto?> GetUserProfileAsync(CancellationToken ct = default)
    {
        try
        {
            _logger?.LogDebug("GetUserProfileAsync: Fetching profile from API...");
            var userProfile = await _apiService.GetMeAsync(ct);
            if (userProfile != null && userProfile.Id > 0)
            {
                _logger?.LogInformation("GetUserProfileAsync: Received profile - Id={Id}, FirstName={FirstName}, LastName={LastName}, Phone={Phone}", 
                    userProfile.Id, userProfile.FirstName ?? "null", userProfile.LastName ?? "null", userProfile.Phone ?? "null");
                
                // Сохраняем обновленный профиль в локальную БД
                await SaveOrUpdateUserAsync(userProfile.Id, userProfile, ct);
                
                return userProfile;
            }
            _logger?.LogWarning("GetUserProfileAsync: API returned null or invalid profile");
            return null;
        }
        catch (UnauthorizedException ex)
        {
            _logger?.LogWarning(ex, "GetUserProfileAsync: ❌ Unauthorized - tokens may be expired or invalid. Message: {Message}", ex.Message);
            // Токены истекли или недействительны - не критично, просто не сможем загрузить профиль
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "GetUserProfileAsync: ❌ Failed to load user profile from API: {Message}", ex.Message);
            return null;
        }
    }
}
