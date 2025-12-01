# Руководство по безопасной настройке JWT токенов

## Обзор

Это руководство описывает безопасную настройку системы аутентификации с использованием Access и Refresh токенов для .NET MAUI приложения и ASP.NET Core бэкенда.

## Архитектура

### Access Token (Короткоживущий)
- **Время жизни**: 5-15 минут (рекомендуется 10 минут)
- **Назначение**: Авторизация API запросов
- **Хранение**: SecureStorage (MAUI)
- **Обновление**: Автоматически через Refresh Token при истечении

### Refresh Token (Долгоживущий)
- **Время жизни**: 7-30 дней (рекомендуется 7 дней)
- **Назначение**: Обновление Access Token без повторного ввода логина/пароля
- **Хранение**: SecureStorage (MAUI)
- **Обновление**: При каждом успешном refresh выдается новый refresh token (rotation)

## Настройка на бэкенде (ASP.NET Core)

### appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "YOUR_VERY_LONG_AND_SECURE_SECRET_KEY_HERE_MIN_32_CHARS",
    "Issuer": "YessBackend",
    "Audience": "YessUsers",
    "AccessTokenExpireMinutes": 10,
    "RefreshTokenExpireDays": 7
  }
}
```

### Рекомендуемые значения

#### Для Production:
```json
{
  "Jwt": {
    "AccessTokenExpireMinutes": 10,    // 10 минут - баланс между безопасностью и UX
    "RefreshTokenExpireDays": 7         // 7 дней - достаточно для удобства пользователя
  }
}
```

#### Для Development:
```json
{
  "Jwt": {
    "AccessTokenExpireMinutes": 60,    // 1 час - удобнее для разработки
    "RefreshTokenExpireDays": 30       // 30 дней - не нужно часто логиниться
  }
}
```

#### Для максимальной безопасности:
```json
{
  "Jwt": {
    "AccessTokenExpireMinutes": 5,     // 5 минут - минимальное время жизни
    "RefreshTokenExpireDays": 1        // 1 день - требует ежедневного подтверждения
  }
}
```

### Генерация SecretKey

**ВАЖНО**: Никогда не храните секретный ключ в коде или в публичных репозиториях!

#### Генерация безопасного ключа:

```bash
# Linux/Mac
openssl rand -base64 64

# PowerShell (Windows)
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

#### Хранение SecretKey:

1. **Production**: Используйте переменные окружения или Azure Key Vault / AWS Secrets Manager
2. **Development**: Используйте `appsettings.Development.json` (добавьте в `.gitignore`)

```json
// appsettings.Development.json (НЕ коммитить в Git!)
{
  "Jwt": {
    "SecretKey": "your-dev-secret-key-here"
  }
}
```

### Docker Compose

```yaml
services:
  backend:
    environment:
      - Jwt__SecretKey=${JWT_SECRET_KEY}  # Из .env файла
      - Jwt__Issuer=YessBackend
      - Jwt__Audience=YessUsers
      - Jwt__AccessTokenExpireMinutes=10
      - Jwt__RefreshTokenExpireDays=7
```

Создайте `.env` файл (НЕ коммитить!):
```env
JWT_SECRET_KEY=your-very-long-secure-secret-key-min-32-chars
```

## Настройка на клиенте (MAUI)

### Автоматическое обновление токенов

Система автоматически обновляет токены в следующих случаях:

1. **При старте приложения** (после PIN/биометрии)
2. **При разблокировке** (после PIN/биометрии)
3. **При получении 401 Unauthorized** от API
4. **Проактивно** - если токен истекает в течение 5 минут

### Использование GlobalAuthService

```csharp
// Получить сервис из DI
var globalAuthService = MauiProgram.Services.GetService<GlobalAuthService>();

// Проверить и обновить токены при старте/разблокировке
bool tokensValid = await globalAuthService.EnsureValidTokensAsync();

// Получить валидный access token (с автоматическим обновлением при необходимости)
string? accessToken = await globalAuthService.GetValidAccessTokenAsync();

// Обработать 401 ошибку
bool refreshed = await globalAuthService.HandleUnauthorizedAsync();
```

## Безопасность

### ✅ Рекомендации

1. **Используйте HTTPS** для всех API запросов
2. **Храните токены в SecureStorage** (MAUI) - использует Keychain (iOS) / Keystore (Android)
3. **Реализуйте Token Rotation** - при каждом refresh выдавайте новый refresh token
4. **Валидируйте refresh token** на бэкенде перед выдачей новых токенов
5. **Используйте короткое время жизни access token** (5-15 минут)
6. **Очищайте токены при выходе** из приложения
7. **Храните SecretKey в переменных окружения** или секретных хранилищах

### ❌ Избегайте

1. **Не храните токены в Preferences** или обычном хранилище
2. **Не используйте одинаковый SecretKey** для разных окружений
3. **Не делайте access token долгоживущим** (> 1 часа)
4. **Не логируйте токены** в production
5. **Не передавайте токены в URL** (только в заголовках)
6. **Не используйте слабый SecretKey** (< 32 символов)

## Обработка ошибок

### 401 Unauthorized

При получении 401 ошибки система автоматически:

1. Проверяет наличие refresh token
2. Отправляет запрос на `/api/v1/auth/refresh`
3. Обновляет токены в SecureStorage
4. Повторяет исходный запрос с новым access token

Если refresh не удался:
- Очищаются все токены
- Пользователь перенаправляется на экран логина

### Refresh Token истек

Если refresh token истек:
- Все токены очищаются
- Пользователь должен войти заново (логин/пароль)

## Мониторинг и логирование

### Рекомендуемые метрики

1. Количество успешных refresh операций
2. Количество неудачных refresh (истек refresh token)
3. Количество 401 ошибок
4. Среднее время жизни access token

### Логирование

```csharp
_logger?.LogInformation("[GlobalAuthService] Tokens refreshed successfully");
_logger?.LogWarning("[GlobalAuthService] Token refresh failed, clearing tokens");
_logger?.LogError(ex, "[GlobalAuthService] Error during token refresh");
```

## Тестирование

### Проверка времени жизни токенов

```csharp
// Проверить оставшееся время access token
var remainingMinutes = JwtHelper.GetTokenRemainingMinutes(accessToken);

// Проверить валидность токена
bool isValid = JwtHelper.IsTokenValid(accessToken);
```

### Тестовые сценарии

1. **Access token истекает** - должен автоматически обновиться
2. **Refresh token истекает** - должен перенаправить на логин
3. **Нет интернета** - должен показать ошибку сети
4. **Неверный refresh token** - должен очистить токены и перенаправить на логин

## Миграция с существующей системы

Если у вас уже есть система с только access token:

1. Обновите бэкенд для выдачи refresh token при логине
2. Обновите клиент для сохранения refresh token в SecureStorage
3. Добавьте эндпоинт `/api/v1/auth/refresh`
4. Обновите AuthHandler для обработки 401 ошибок
5. Добавьте GlobalAuthService для централизованного управления

## Дополнительные ресурсы

- [OWASP JWT Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [Microsoft JWT Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [.NET MAUI SecureStorage](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage)

