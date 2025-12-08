using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YessGoFront.Config;
using YessGoFront.Infrastructure.Http;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Models;

namespace YessGoFront.Services.Api
{
    /// <summary>
    /// Реализация API сервиса для аутентификации
    /// </summary>
    public class AuthApiService : ApiClient, IAuthApiService
    {
        public AuthApiService(HttpClient httpClient, ILogger<AuthApiService>? logger = null)
            : base(httpClient, logger)
        {
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            try
            {
                // Валидация входных данных
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Login request cannot be null");
                }
                
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    throw new ArgumentException("Phone number is required", nameof(request));
                }
                
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    throw new ArgumentException("Password is required", nameof(request));
                }

                // Используем JSON для логина (backend принимает JSON на /api/v1/auth/login)
                var loginDto = new UserLoginDto
                {
                    Phone = request.Username.Trim(),  // Backend ожидает поле "phone" в JSON
                    Password = request.Password
                };

                Logger?.LogInformation("[AuthApiService] Attempting login for phone: {Phone}", loginDto.Phone);
                Logger?.LogDebug("[AuthApiService] BaseAddress: {BaseAddress}", HttpClient.BaseAddress);

                // Используем /api/v1/auth/login/json для JSON запросов
                // Согласно документации API, есть два варианта: /login и /login/json
                // Используем /login/json так как отправляем JSON
                Logger?.LogInformation("[AuthApiService] Using endpoint: {Endpoint}", ApiEndpoints.AuthEndpoints.LoginJson);
                var tokenResponse = await PostAsync<UserLoginDto, TokenResponseDto>(
                    ApiEndpoints.AuthEndpoints.LoginJson,
                    loginDto,
                    ct
                );

                // Проверка на null ответ
                if (tokenResponse == null)
                {
                    Logger?.LogError("[AuthApiService] Login response is null");
                    throw new ApiException("Получен пустой ответ от сервера при входе");
                }

                // Проверка на пустой токен
                if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                {
                    Logger?.LogError("[AuthApiService] AccessToken is null or empty in login response");
                    throw new ApiException("Не получен токен доступа от сервера");
                }

                // Конвертируем TokenResponseDto в AuthResponse
                var authResponse = new AuthResponse
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    TokenType = tokenResponse.TokenType ?? "bearer"
                };
                
                Logger?.LogInformation("[AuthApiService] Login successful. AccessToken: {HasAccess}, RefreshToken: {HasRefresh}", 
                    !string.IsNullOrEmpty(authResponse.AccessToken), 
                    !string.IsNullOrEmpty(authResponse.RefreshToken));
                return authResponse;
            }
            catch (NotFoundException ex)
            {
                // Специальная обработка 404 для login endpoint
                Logger?.LogError(ex, "[AuthApiService] Login endpoint not found (404). Endpoint: {Endpoint}, BaseAddress: {BaseAddress}", 
                    ApiEndpoints.AuthEndpoints.Login, HttpClient.BaseAddress);
                throw new NotFoundException(
                    $"Endpoint входа не найден на сервере. Проверьте конфигурацию API или обратитесь к администратору. " +
                    $"Попытка доступа к: {HttpClient.BaseAddress}{ApiEndpoints.AuthEndpoints.Login}", ex);
            }
            catch (UnauthorizedException)
            {
                // Пробрасываем UnauthorizedException без изменений
                throw;
            }
            catch (BadRequestException)
            {
                // Пробрасываем BadRequestException без изменений
                throw;
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                Logger?.LogError(ex, "[AuthApiService] Network error during login: {Message}", ex.Message);
                throw new NetworkException($"Ошибка сети при входе: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "[AuthApiService] Unexpected error during login: {Message}", ex.Message);
                throw new ApiException($"Ошибка при входе: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// DTO для отправки логина в JSON формате
        /// </summary>
        private class UserLoginDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("phone")]
            public string Phone { get; set; } = string.Empty;
            
            [System.Text.Json.Serialization.JsonPropertyName("password")]
            public string Password { get; set; } = string.Empty;
        }



        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            try
            {
                var requestBody = new { refresh_token = refreshToken };
                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var uri = BuildUri(ApiEndpoints.AuthEndpoints.Refresh);
                Logger?.LogDebug("➡️ POST {Url} (refresh token)", uri);

                var response = await HttpClient.PostAsync(uri, content, ct);

                if (!response.IsSuccessStatusCode)
                    throw await MapToApiExceptionAsync(response, "Ошибка обновления токена");

                var json = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<AuthResponse>(json, JsonOptions)
                       ?? throw new ApiException("Ошибка при разборе ответа refresh токена");
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
            }
        }

        // ❌ Пока нет такого эндпоинта на backend
        public Task LogoutAsync(CancellationToken ct = default)
        {
            throw new NotSupportedException("Эндпоинт /auth/logout отсутствует на сервере.");
        }

        // ❌ Этого эндпоинта тоже нет сейчас
        public Task<AuthResponse> VerifyCodeAsync(string code, CancellationToken ct = default)
        {
            throw new NotSupportedException("Эндпоинт /auth/verify отсутствует на сервере.");
        }

        public async Task<Dictionary<string, object>> SendVerificationCodeAsync(string phoneNumber, CancellationToken ct = default)
        {
            var request = new VerificationCodeRequest { phone_number = phoneNumber };
            
            // Пробуем сначала основной endpoint /send-verification-code
            try
            {
                Logger?.LogInformation("[AuthApiService] Sending verification code to: {Phone}, trying endpoint: {Endpoint}", 
                    phoneNumber, ApiEndpoints.AuthEndpoints.SendVerificationCode);
                
                var response = await PostAsync<VerificationCodeRequest, Dictionary<string, object>>(
                    ApiEndpoints.AuthEndpoints.SendVerificationCode,
                    request,
                    ct
                );
                
                Logger?.LogInformation("[AuthApiService] Successfully sent verification code using endpoint: {Endpoint}", 
                    ApiEndpoints.AuthEndpoints.SendVerificationCode);
                return response;
            }
            catch (NotFoundException)
            {
                // Если основной endpoint не найден, пробуем альтернативный /send-code
                Logger?.LogWarning("[AuthApiService] Primary endpoint {PrimaryEndpoint} returned 404, trying fallback: {FallbackEndpoint}", 
                    ApiEndpoints.AuthEndpoints.SendVerificationCode, ApiEndpoints.AuthEndpoints.SendCode);
                
                try
                {
                    var response = await PostAsync<VerificationCodeRequest, Dictionary<string, object>>(
                        ApiEndpoints.AuthEndpoints.SendCode,
                        request,
                        ct
                    );
                    
                    Logger?.LogInformation("[AuthApiService] Successfully sent verification code using fallback endpoint: {Endpoint}", 
                        ApiEndpoints.AuthEndpoints.SendCode);
                    return response;
                }
                catch (Exception fallbackEx)
                {
                    Logger?.LogError(fallbackEx, "[AuthApiService] Both endpoints failed. Primary: {Primary}, Fallback: {Fallback}", 
                        ApiEndpoints.AuthEndpoints.SendVerificationCode, ApiEndpoints.AuthEndpoints.SendCode);
                    throw new NotFoundException(
                        $"Endpoint отправки кода верификации не найден на сервере. " +
                        $"Попробованы: {ApiEndpoints.AuthEndpoints.SendVerificationCode} и {ApiEndpoints.AuthEndpoints.SendCode}. " +
                        $"Проверьте конфигурацию API или обратитесь к администратору.", fallbackEx);
                }
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
            }
        }

        public async Task<UserDto> VerifyCodeAndRegisterAsync(VerifyCodeRequest request, CancellationToken ct = default)
        {
            try
            {
                // Валидация входных данных
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "VerifyCodeRequest cannot be null");
                }
                
                if (string.IsNullOrWhiteSpace(request.phone_number))
                {
                    throw new ArgumentException("Phone number is required", nameof(request));
                }
                
                if (string.IsNullOrWhiteSpace(request.code))
                {
                    throw new ArgumentException("Verification code is required", nameof(request));
                }
                
                if (string.IsNullOrWhiteSpace(request.password))
                {
                    throw new ArgumentException("Password is required", nameof(request));
                }

                Logger?.LogInformation("[AuthApiService] Attempting registration for phone: {Phone}, Endpoint: {Endpoint}", 
                    request.phone_number, ApiEndpoints.AuthEndpoints.VerifyCode);
                
                // Используем константу из ApiEndpoints вместо конкатенации строк
                var response = await PostAsync<VerifyCodeRequest, UserDto>(
                    ApiEndpoints.AuthEndpoints.VerifyCode,
                    request,
                    ct
                );
                
                // Проверка на null ответ
                if (response == null)
                {
                    Logger?.LogError("[AuthApiService] Registration response is null");
                    throw new ApiException("Получен пустой ответ от сервера при регистрации");
                }
                
                // Проверка на валидный ID пользователя
                if (response.Id <= 0)
                {
                    Logger?.LogWarning("[AuthApiService] Registration response has invalid user ID: {Id}", response.Id);
                }
                
                Logger?.LogInformation("[AuthApiService] Registration successful. UserId: {Id}, Phone: {Phone}", 
                    response.Id, response.Phone);
                
                return response;
            }
            catch (NotFoundException ex)
            {
                // Специальная обработка 404 для registration endpoints
                Logger?.LogError(ex, "[AuthApiService] Registration endpoint not found (404). Endpoint: {Endpoint}, BaseAddress: {BaseAddress}", 
                    ApiEndpoints.AuthEndpoints.VerifyCode, HttpClient.BaseAddress);
                throw new NotFoundException(
                    $"Endpoint регистрации не найден на сервере. Проверьте конфигурацию API или обратитесь к администратору. " +
                    $"Попытка доступа к: {HttpClient.BaseAddress}{ApiEndpoints.AuthEndpoints.VerifyCode}", ex);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                Logger?.LogError(ex, "[AuthApiService] Network error during registration: {Message}", ex.Message);
                throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "[AuthApiService] Unexpected error during registration: {Message}", ex.Message);
                throw new ApiException($"Ошибка при регистрации: {ex.Message}", ex);
            }
        }

        public async Task<UserDto> GetMeAsync(CancellationToken ct = default)
        {
            try
            {
                // Используем константу из ApiEndpoints
                Logger?.LogInformation("[AuthApiService] Getting user profile, Endpoint: {Endpoint}", 
                    ApiEndpoints.AuthEndpoints.Me);
                
                // Получаем JSON ответ напрямую для отладки
                var uri = BuildUri(ApiEndpoints.AuthEndpoints.Me);
                Logger?.LogDebug("GET {Url}", uri);
                
                var response = await HttpClient.GetAsync(uri, ct);
                var jsonContent = await response.Content.ReadAsStringAsync(ct);
                
                // Логируем ответ независимо от статуса
                Logger?.LogInformation("GetMeAsync response: Status={StatusCode}, Body={Json}", response.StatusCode, jsonContent);
                
                // Проверяем статус код после логирования
                await EnsureSuccessStatusCode(response);
                
                // Десериализуем вручную - используем DefaultJsonSerializerOptions без PropertyNamingPolicy
                // Это позволит использовать только JsonPropertyName атрибуты
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = null // Отключаем camelCase, используем только JsonPropertyName атрибуты
                };
                
                var userDto = System.Text.Json.JsonSerializer.Deserialize<UserDto>(jsonContent, jsonOptions);
                
                if (userDto == null)
                {
                    throw new ApiException("Не удалось десериализовать ответ /me");
                }
                
                // Логируем полученные данные для отладки
                Logger?.LogInformation("GetMeAsync deserialized: Id={Id}, FirstName='{FirstName}', LastName='{LastName}', Phone='{Phone}'", 
                    userDto.Id, userDto.FirstName ?? "null", userDto.LastName ?? "null", userDto.Phone ?? "null");
                
                // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: если FirstName/LastName пустые, попробуем прочитать напрямую из JSON
                if (string.IsNullOrWhiteSpace(userDto.FirstName) && string.IsNullOrWhiteSpace(userDto.LastName))
                {
                    try
                    {
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                        var root = jsonDoc.RootElement;
                        
                        if (root.TryGetProperty("firstName", out var firstNameProp))
                        {
                            userDto.FirstName = firstNameProp.GetString() ?? string.Empty;
                            Logger?.LogWarning("GetMeAsync: Manually extracted firstName from JSON: '{FirstName}'", userDto.FirstName);
                        }
                        
                        if (root.TryGetProperty("lastName", out var lastNameProp))
                        {
                            userDto.LastName = lastNameProp.GetString() ?? string.Empty;
                            Logger?.LogWarning("GetMeAsync: Manually extracted lastName from JSON: '{LastName}'", userDto.LastName);
                        }
                        
                        if (root.TryGetProperty("phone", out var phoneProp))
                        {
                            var phoneValue = phoneProp.GetString();
                            if (!string.IsNullOrWhiteSpace(phoneValue) && string.IsNullOrWhiteSpace(userDto.Phone))
                            {
                                userDto.Phone = phoneValue;
                                Logger?.LogWarning("GetMeAsync: Manually extracted phone from JSON: '{Phone}'", userDto.Phone);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogWarning(ex, "GetMeAsync: Failed to manually extract properties from JSON");
                    }
                }
                
                return userDto;
            }
            catch (NotFoundException ex)
            {
                // Специальная обработка 404 для send-verification-code endpoint
                Logger?.LogError(ex, "[AuthApiService] SendVerificationCode endpoint not found (404). Endpoint: {Endpoint}, BaseAddress: {BaseAddress}", 
                    ApiEndpoints.AuthEndpoints.SendVerificationCode, HttpClient.BaseAddress);
                throw new NotFoundException(
                    $"Endpoint отправки кода верификации не найден на сервере. Проверьте конфигурацию API или обратитесь к администратору. " +
                    $"Попытка доступа к: {HttpClient.BaseAddress}{ApiEndpoints.AuthEndpoints.SendVerificationCode}", ex);
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
            }
        }

        public async Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
        {
            try
            {
                var response = await PutAsync<UpdateProfileRequest, UserDto>(
                    ApiEndpoints.UserEndpoints.UpdateProfile,
                    request,
                    ct
                );

                Logger?.LogInformation("Profile updated successfully. Id={Id}, FirstName={FirstName}, LastName={LastName}",
                    response.Id, response.FirstName ?? "null", response.LastName ?? "null");

                return response;
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
            }
        }


        // ----------------- Helpers -----------------

        /// <summary>
        /// Промежуточный класс для десериализации ответа от backend (camelCase)
        /// </summary>
        private class TokenResponseDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("accessToken")]
            public string AccessToken { get; set; } = string.Empty;
            
            [System.Text.Json.Serialization.JsonPropertyName("refreshToken")]
            public string RefreshToken { get; set; } = string.Empty;
            
            [System.Text.Json.Serialization.JsonPropertyName("tokenType")]
            public string? TokenType { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("expiresIn")]
            public int ExpiresIn { get; set; }
        }

        private static bool IsNetworkError(Exception ex) =>
            ex is HttpRequestException
            or SocketException
            or IOException
            or TaskCanceledException
            || ex.InnerException is SocketException
            || ex.InnerException is IOException;

        private static async Task<ApiException> MapToApiExceptionAsync(HttpResponseMessage response, string defaultMessage)
        {
            var status = response.StatusCode;
            var text = defaultMessage;

            try
            {
                var body = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(body))
                    text = $"{defaultMessage}: {body}";
            }
            catch { }

            return status switch
            {
                HttpStatusCode.Unauthorized => new UnauthorizedException("Неверные учетные данные"),
                HttpStatusCode.Forbidden => new ForbiddenException("Доступ запрещён"),
                HttpStatusCode.NotFound => new NotFoundException("Ресурс не найден"),
                HttpStatusCode.BadRequest => new BadRequestException("Неверный запрос", text),
                _ when (int)status >= 500 => new ServerException("Ошибка сервера", status),
                _ => new ApiException(text, status)
            };
        }
    }
}
