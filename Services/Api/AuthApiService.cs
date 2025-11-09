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
            var formData = new List<KeyValuePair<string, string>>
            {
                new("username", request.Username),
                new("password", request.Password)
            };

            var formContent = new FormUrlEncodedContent(formData);
            formContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            try
            {
                var uri = BuildUri(ApiEndpoints.AuthEndpoints.Login);
                Logger?.LogDebug("➡️ POST {Url} (OAuth2 login)", uri);

                var response = await HttpClient.PostAsync(uri, formContent, ct);

                if (!response.IsSuccessStatusCode)
                    throw await MapToApiExceptionAsync(response, "Ошибка входа");

                var json = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<AuthResponse>(json, JsonOptions)
                       ?? throw new ApiException("Ошибка при разборе ответа сервера");
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
            }
        }

        public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            try
            {
                return await PostAsync<RegisterRequest, UserDto>(
                    ApiEndpoints.AuthEndpoints.Register,
                    request,
                    ct
                );
            }
            catch (Exception ex) when (IsNetworkError(ex))
            {
                throw new NetworkException("Ошибка сети. Проверьте подключение к интернету.", ex);
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            var endpoint = $"{ApiEndpoints.AuthEndpoints.Refresh}?refresh_token={Uri.EscapeDataString(refreshToken)}";

            try
            {
                var uri = BuildUri(endpoint);
                Logger?.LogDebug("➡️ POST {Url} (refresh token)", uri);

                var response = await HttpClient.PostAsync(uri, new StringContent(string.Empty), ct);

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
