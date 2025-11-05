using System;
using System.Collections.Generic;
using System.Text.Json;

namespace YessGoFront.Infrastructure.Auth;

/// <summary>
/// Простая утилита для декодирования JWT токенов (без проверки подписи)
/// </summary>
public static class JwtHelper
{
    /// <summary>
    /// Декодировать payload JWT токена без проверки подписи
    /// </summary>
    public static T? DecodePayload<T>(string token) where T : class
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            // JWT формат: header.payload.signature
            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];
            
            // Добавляем padding если нужно (base64url)
            var padding = 4 - payload.Length % 4;
            if (padding != 4)
                payload += new string('=', padding);

            // Заменяем - и _ обратно на + и /
            payload = payload.Replace('-', '+').Replace('_', '/');

            var bytes = Convert.FromBase64String(payload);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Получить user_id из JWT токена
    /// </summary>
    public static int? GetUserId(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        var payload = DecodePayload<Dictionary<string, JsonElement>>(accessToken);
        if (payload == null)
            return null;

        // Бэкенд хранит user_id в поле "sub" (subject)
        if (payload.TryGetValue("sub", out var subElement))
        {
            var sub = subElement.GetString();
            if (int.TryParse(sub, out var userId))
                return userId;
        }

        return null;
    }
}

