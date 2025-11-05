# Интеграция с бэкендом

## 📋 Обзор архитектуры

Проект использует многослойную архитектуру для интеграции с бэкендом:

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│  (Views, ViewModels, Pages)              │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│        Application Layer                 │
│  (Services/Domain - бизнес-логика)       │
│  - PartnersService                       │
│  - AuthService                           │
│  - WalletService                         │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│        Infrastructure Layer             │
│  (Services/Api - прямые вызовы API)      │
│  - PartnersApiService                   │
│  - AuthApiService                       │
│  - WalletApiService                     │
│  - ApiClient (базовый класс)            │
│  - AuthenticationService                │
│  - HTTP Handlers                        │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│        Backend API                      │
└─────────────────────────────────────┘
```

## 🗂️ Структура файлов

### Config/
- **AppSettings.cs** - Настройки приложения (API URL, таймауты)
- **ApiEndpoints.cs** - Централизованные endpoints API

### Infrastructure/
- **Exceptions/** - Кастомные исключения (ApiException, NetworkException и т.д.)
- **Http/ApiClient.cs** - Базовый класс для всех API клиентов
- **Http/HttpMessageHandlers/**
  - **AuthHandler.cs** - Автоматическое добавление токенов
  - **LoggingHandler.cs** - Логирование HTTP запросов
- **Auth/**
  - **IAuthenticationService.cs** - Интерфейс для работы с токенами
  - **AuthenticationService.cs** - Реализация с использованием SecureStorage

### Services/
- **Api/** - Прямые вызовы к API (инфраструктурный слой)
  - IPartnersApiService / PartnersApiService
  - IAuthApiService / AuthApiService
  - IWalletApiService / WalletApiService
  - IQRApiService / QRApiService

- **Domain/** - Бизнес-логика (прикладной слой)
  - IPartnersService / PartnersService
  - IAuthService / AuthService
  - IWalletService / WalletService
  - IQRService / QRService

## 🚀 Шаги для интеграции

### 1. Клонирование репозитория бэкенда

```bash
# Клонируйте репозиторий бэкенда в отдельную директорию
git clone <backend-repo-url> ../yess-go-backend
```

### 2. Настройка URL API

Откройте файл **Config/AppSettings.cs** и обновите `BaseUrl`:

```csharp
services.AddSingleton<AppSettings>(_ => new AppSettings
{
    Api = new ApiSettings
    {
        BaseUrl = "https://your-backend-api.com", // ← Обновить здесь
        ApiVersion = "v1",
        RequestTimeoutSeconds = 30
    }
});
```

**Для разработки (локальный сервер):**
- Android Emulator: `http://10.0.2.2:5000`
- iOS Simulator: `http://localhost:5000`
- Физическое устройство: `http://<your-ip>:5000`

### 3. Проверка endpoints

Откройте файл **Config/ApiEndpoints.cs** и убедитесь, что endpoints соответствуют вашему бэкенду:

```csharp
public static class ApiEndpoints
{
    public const string Auth = "/api/auth";
    public const string Partners = "/api/partners";
    // ... и т.д.
}
```

Если структура API отличается, обновите соответствующие endpoints.

### 4. Обновление моделей данных

Проверьте файлы в папке **Models/** и убедитесь, что они соответствуют структуре ответов вашего API:

- `PartnerDto.cs`
- `PartnerDetailDto.cs`
- `UserDto.cs`
- `PurchaseDto.cs`

При необходимости обновите имена свойств и атрибуты `[JsonPropertyName]`.

### 5. Реализация refresh token

Откройте файл **Infrastructure/Auth/AuthenticationService.cs** и реализуйте метод `RefreshTokenAsync`:

```csharp
public async Task<bool> RefreshTokenAsync()
{
    try
    {
        var refreshToken = await GetRefreshTokenAsync();
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        // TODO: Получить IAuthApiService через DI и вызвать RefreshTokenAsync
        // var authApi = ... // получить через DI
        // var response = await authApi.RefreshTokenAsync(refreshToken);
        // await SaveTokensAsync(response.AccessToken, response.RefreshToken);
        
        return true;
    }
    catch
    {
        await ClearTokensAsync();
        return false;
    }
}
```

**Важно:** Для использования DI в `AuthenticationService`, нужно передать зависимости через конструктор.

### 6. Обновление ViewModels для использования DI

Все ViewModels должны получать зависимости через конструктор. Пример:

```csharp
public partial class PartnerPageViewModel : ObservableObject
{
    private readonly IPartnersService _partnersService;
    
    public PartnerPageViewModel(IPartnersService partnersService)
    {
        _partnersService = partnersService ?? throw new ArgumentNullException(nameof(partnersService));
    }
}
```

В XAML или в code-behind создавайте ViewModel через DI:

```csharp
// В Page.xaml.cs
public partial class PartnerPage : ContentPage
{
    public PartnerPage(PartnerPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

Или через статический доступ (если не используется DI для страниц):

```csharp
var serviceProvider = Application.Current?.Handler?.MauiContext?.Services;
var viewModel = serviceProvider?.GetService<PartnerPageViewModel>() 
    ?? new PartnerPageViewModel(serviceProvider?.GetService<IPartnersService>());
```

### 7. Тестирование подключения

1. Запустите бэкенд локально или убедитесь, что production API доступен
2. Проверьте настройки URL в `AppSettings.cs`
3. Запустите приложение и попробуйте выполнить базовые операции:
   - Логин/Регистрация
   - Загрузка партнёров
   - Просмотр баланса

## 🔧 Настройка для разных окружений

### Development
```csharp
BaseUrl = "http://localhost:5000"; // или IP вашего локального сервера
```

### Staging
```csharp
BaseUrl = "https://staging-api.yessgo.com";
```

### Production
```csharp
BaseUrl = "https://api.yessgo.com";
```

Можно использовать условия компиляции:

```csharp
#if DEBUG
    BaseUrl = "http://localhost:5000";
#else
    BaseUrl = "https://api.yessgo.com";
#endif
```

## 📝 Примеры использования

### Получение партнёров через Domain Service

```csharp
// В ViewModel
private readonly IPartnersService _partnersService;

public async Task LoadPartnersAsync()
{
    try
    {
        var partners = await _partnersService.GetPartnersByCategoryAsync("для дома");
        // Обработка результата
    }
    catch (NetworkException ex)
    {
        // Нет интернета
    }
    catch (ApiException ex)
    {
        // Ошибка API
    }
}
```

### Аутентификация

```csharp
// В ViewModel или Service
private readonly IAuthService _authService;

public async Task LoginAsync(string email, string password)
{
    try
    {
        var response = await _authService.LoginAsync(email, password);
        // Токены автоматически сохраняются
        // Пользователь авторизован
    }
    catch (UnauthorizedException)
    {
        // Неверные credentials
    }
}
```

## ⚠️ Важные замечания

1. **HttpClient Management**: Все HttpClient создаются через `IHttpClientFactory` с правильным lifetime
2. **Токены**: Хранятся в `SecureStorage`, автоматически добавляются к запросам через `AuthHandler`
3. **Обработка ошибок**: Используйте кастомные исключения из `Infrastructure.Exceptions`
4. **Логирование**: Настроено через `ILogger`, работает только в Debug режиме

## 🔍 Отладка

### Проверка HTTP запросов

Логи запросов выводятся в Debug Output (Visual Studio / Rider):

```
HTTP GET https://api.yessgo.com/api/partners?category=...
HTTP GET ... - OK (234ms)
```

### Проверка токенов

Добавьте временный код для проверки:

```csharp
var authService = serviceProvider.GetService<IAuthenticationService>();
var token = await authService.GetAccessTokenAsync();
Debug.WriteLine($"Token: {token}");
```

### Проверка конфигурации

```csharp
var settings = serviceProvider.GetService<AppSettings>();
Debug.WriteLine($"API Base URL: {settings.Api.BaseUrl}");
```

## 📚 Дополнительные ресурсы

- [MAUI HttpClient Guide](https://learn.microsoft.com/dotnet/maui/data-cloud/httpclient)
- [Dependency Injection in MAUI](https://learn.microsoft.com/dotnet/maui/fundamentals/dependency-injection)

---

**Готово к интеграции!** 🎉

После выполнения всех шагов, приложение будет готово к работе с вашим бэкендом.

