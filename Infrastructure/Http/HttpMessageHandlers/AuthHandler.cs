using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using YessGoFront.Infrastructure.Auth;

namespace YessGoFront.Infrastructure.Http.HttpMessageHandlers;

/// <summary>
/// HTTP Handler для автоматического добавления Authorization заголовка
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthHandler>? _logger;

    public AuthHandler(
        IAuthenticationService authService,
        ILogger<AuthHandler>? logger = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Добавляем токен к запросу, если он есть
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger?.LogDebug("Added Bearer token to request {Uri}", request.RequestUri);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Обработка 401 - попытка обновить токен
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger?.LogWarning("Received 401 Unauthorized, attempting token refresh");
            
            var refreshed = await _authService.RefreshTokenAsync();
            if (refreshed)
            {
                // Повторяем запрос с новым токеном
                token = await _authService.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    response = await base.SendAsync(request, cancellationToken);
                    _logger?.LogDebug("Retried request after token refresh");
                }
            }
            else
            {
                _logger?.LogWarning("Token refresh failed, user needs to re-authenticate");
            }
        }

        return response;
    }
}

