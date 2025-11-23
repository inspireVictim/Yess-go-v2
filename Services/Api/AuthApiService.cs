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
                // Используем JSON для логина (backend принимает JSON на /api/v1/auth/login)
                var loginDto = new UserLoginDto
                {
                    Phone = request.Username,  // Backend ожидает поле "phone" в JSON
                    Password = request.Password
                };

                // Используем базовый метод PostAsync, который отправляет JSON
                // Endpoint: /api/v1/auth/login (без /json суффикса)
                var tokenResponse = await PostAsync<UserLoginDto, TokenResponseDto>(
                    ApiEndpoints.AuthEndpoints.Login,
                    loginDto,
                    ct
                );

                // Конвертируем TokenResponseDto в AuthResponse
                var authResponse = new AuthResponse
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    TokenType = tokenResponse.TokenType ?? "bearer"
                };
                
                Logger?.LogInformation("Login successful. AccessToken: {HasAccess}, RefreshToken: {HasRefresh}", 
                    !string.IsNullOrEmpty(authResponse.AccessToken), 
                    !string.IsNullOrEmpty(authResponse.RefreshToken));
                return authResponse;
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
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
            try
            {
                var request = new VerificationCodeRequest { phone_number = phoneNumber };
                var response = await PostAsync<VerificationCodeRequest, Dictionary<string, object>>(
                    $"{ApiEndpoints.AuthEndpoints.Base}/send-verification-code",
                    request,
                    ct
                );
                return response;
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
                return await PostAsync<VerifyCodeRequest, UserDto>(
                    $"{ApiEndpoints.AuthEndpoints.Base}/verify-code",
                    request,
                    ct
                );
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
