# Сводка реализации безопасной системы аутентификации

## ✅ Что было реализовано

### 1. GlobalAuthService (Глобальный сервис аутентификации)

**Файл**: `YessGoFrontV2/Services/GlobalAuthService.cs`

Централизованный singleton-сервис для управления токенами:

- ✅ **EnsureValidTokensAsync()** - проверяет и обновляет токены при старте/разблокировке
- ✅ **RefreshTokensAsync()** - обновляет токены с защитой от параллельных вызовов
- ✅ **HandleUnauthorizedAsync()** - обрабатывает 401 ошибки от API
- ✅ **GetValidAccessTokenAsync()** - получает валидный токен с автоматическим обновлением
- ✅ **IsAuthenticatedAsync()** - проверяет статус аутентификации

**Особенности**:
- Защита от параллельных refresh запросов через `SemaphoreSlim`
- Минимальный интервал между попытками refresh (5 секунд)
- Автоматическая очистка токенов при ошибках

### 2. Улучшенная обработка 401 ошибок

**Файл**: `YessGoFrontV2/Infrastructure/Http/HttpMessageHandlers/AuthHandler.cs`

- ✅ Автоматическое обновление токенов при получении 401
- ✅ Интеграция с GlobalAuthService
- ✅ Защита от бесконечных циклов (не обновляет при запросах к `/auth/refresh`)
- ✅ Проактивное обновление токенов (если истекают в течение 5 минут)

### 3. Автоматическое обновление при старте/разблокировке

**Файл**: `YessGoFrontV2/Views/PinLoginPage.xaml.cs`

- ✅ Обновление токенов после успешной PIN-аутентификации
- ✅ Обновление токенов после успешной биометрической аутентификации
- ✅ Использование GlobalAuthService для централизованного управления

### 4. Регистрация в DI

**Файл**: `YessGoFrontV2/MauiProgram.cs`

- ✅ GlobalAuthService зарегистрирован как Singleton
- ✅ Доступен из любой страницы через `MauiProgram.Services.GetService<GlobalAuthService>()`

### 5. Документация

**Файлы**:
- `YessGoFrontV2/JWT_TOKEN_SECURITY_GUIDE.md` - полное руководство по настройке токенов
- `yess-backend-dotnet/IMPROVED_REFRESH_TOKEN_ENDPOINT.md` - улучшенный пример API эндпоинта

## 🔄 Как это работает

### Сценарий 1: Открытие приложения

1. Пользователь открывает приложение
2. AppShell проверяет наличие токенов
3. Если есть refresh token → переход на PIN-экран
4. После успешной PIN/биометрии → `GlobalAuthService.EnsureValidTokensAsync()`
5. Если access token истек → автоматический refresh
6. Переход в приложение

### Сценарий 2: API запрос с истекшим токеном

1. API запрос с истекшим access token
2. Сервер возвращает 401 Unauthorized
3. `AuthHandler` перехватывает 401
4. Вызывает `GlobalAuthService.HandleUnauthorizedAsync()`
5. Обновляет токены через `/api/v1/auth/refresh`
6. Повторяет исходный запрос с новым токеном

### Сценарий 3: Refresh token истек

1. Попытка обновить токены
2. Сервер возвращает 401 (refresh token истек)
3. `GlobalAuthService` очищает все токены
4. Пользователь перенаправляется на экран логина

## 📋 Настройка на бэкенде

### appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "YOUR_SECRET_KEY_HERE",
    "Issuer": "YessBackend",
    "Audience": "YessUsers",
    "AccessTokenExpireMinutes": 10,
    "RefreshTokenExpireDays": 7
  }
}
```

**Рекомендуемые значения**:
- AccessTokenExpireMinutes: **10** (5-15 минут)
- RefreshTokenExpireDays: **7** (7-30 дней)

## 🔒 Безопасность

✅ Токены хранятся в SecureStorage (Keychain/Keystore)
✅ Access token короткоживущий (10 минут)
✅ Refresh token долгоживущий (7 дней)
✅ Token rotation (новый refresh token при каждом обновлении)
✅ Автоматическая очистка при ошибках
✅ Защита от параллельных refresh запросов
✅ Валидация токенов на бэкенде

## 📱 Использование в коде

### Получить GlobalAuthService

```csharp
var globalAuthService = MauiProgram.Services.GetService<GlobalAuthService>();
```

### Проверить и обновить токены

```csharp
bool tokensValid = await globalAuthService.EnsureValidTokensAsync();
if (!tokensValid)
{
    // Перенаправить на логин
    await Shell.Current.GoToAsync("///login");
}
```

### Получить валидный access token

```csharp
string? accessToken = await globalAuthService.GetValidAccessTokenAsync();
if (accessToken == null)
{
    // Токены невалидны, перенаправить на логин
}
```

### Обработать 401 ошибку

```csharp
bool refreshed = await globalAuthService.HandleUnauthorizedAsync();
if (!refreshed)
{
    // Refresh не удался, перенаправить на логин
}
```

## 🧪 Тестирование

### Проверка работы refresh

1. Войдите в приложение
2. Подождите 10+ минут (или измените время жизни токена на 1 минуту для теста)
3. Выполните любой API запрос
4. Токен должен автоматически обновиться

### Проверка обработки 401

1. Удалите access token из SecureStorage (оставьте refresh token)
2. Выполните API запрос
3. Должен автоматически обновиться и повторить запрос

### Проверка истекшего refresh token

1. Измените время жизни refresh token на 1 минуту
2. Подождите 1+ минуту
3. Попробуйте обновить токены
4. Должен перенаправить на экран логина

## 📚 Дополнительная документация

- **JWT_TOKEN_SECURITY_GUIDE.md** - полное руководство по настройке и безопасности
- **IMPROVED_REFRESH_TOKEN_ENDPOINT.md** - улучшенный пример API эндпоинта

## ⚠️ Важные замечания

1. **SecretKey** должен быть длинным (минимум 32 символа) и храниться в переменных окружения
2. **HTTPS** обязателен для production
3. **Не логируйте токены** в production
4. **Token rotation** включен - при каждом refresh выдается новый refresh token
5. **SecureStorage** использует Keychain (iOS) / Keystore (Android) для безопасного хранения

## 🎯 Следующие шаги (опционально)

1. Добавить rate limiting на эндпоинт refresh
2. Реализовать отзыв токенов (token revocation)
3. Добавить device fingerprinting
4. Реализовать 2FA для дополнительной защиты

