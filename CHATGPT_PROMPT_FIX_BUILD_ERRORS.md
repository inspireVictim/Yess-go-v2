# Промпт для ChatGPT: Исправление ошибок компиляции .NET MAUI проекта

## Контекст проекта

Это .NET MAUI проект (версия .NET 9.0), использующий:
- Microsoft.Maui.Controls 9.0.100
- Mapsui.Maui 4.1.9 (с предупреждениями о версиях)
- PostgreSQL база данных через Entity Framework
- HttpClient для API запросов

## Критические ошибки компиляции

### 1. Отсутствует класс NetworkException (62 ошибки CS0246)

**Проблема**: В коде используется класс `NetworkException`, но он не определён в проекте.

**Где используется**:
- `Infrastructure/Http/ApiClient.cs` (множество мест)
- `Services/Api/AuthApiService.cs`
- `Services/Domain/AuthService.cs`
- `Services/Domain/WalletService.cs`
- `Services/Domain/QRService.cs`
- `Services/Domain/PartnersService.cs`
- `ViewModels/LoginViewModel.cs`
- `ViewModels/RegisterViewModel.cs`
- `ViewModels/PartnerPageViewModel.cs`

**Контекст использования**:
В `ApiClient.cs` класс используется для обработки сетевых ошибок:
```csharp
catch (HttpRequestException ex)
{
    Logger?.LogError(ex, "HTTP request failed for GET {Endpoint}", endpoint);
    throw new NetworkException("Ошибка при выполнении запроса", ex);
}
catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
{
    Logger?.LogError("Request timeout for GET {Endpoint}", endpoint);
    throw new NetworkException("Превышено время ожидания запроса");
}
catch (TaskCanceledException ex)
{
    Logger?.LogError(ex, "Request cancelled for GET {Endpoint}", endpoint);
    throw new NetworkException("Запрос был отменён");
}
```

**Существующие исключения**: В проекте уже есть `Infrastructure/Exceptions/ApiException.cs` с базовым классом `ApiException` и производными классами (`UnauthorizedException`, `ForbiddenException`, `NotFoundException`, `BadRequestException`, `ServerException`).

**Решение**: 
- Добавить класс `NetworkException` в файл `Infrastructure/Exceptions/ApiException.cs` или создать отдельный файл
- Класс должен наследоваться от `ApiException` или от `Exception`
- Должен поддерживать конструкторы с сообщением и внутренним исключением

**Пример использования**:
```csharp
throw new NetworkException("Не удалось подключиться к серверу. Проверьте адрес и доступность сервера.", ex);
throw new NetworkException("Превышено время ожидания запроса");
throw new NetworkException("Запрос был отменён");
```

## Предупреждения, которые стоит исправить

### 2. Nullable параметры в IValueConverter (предупреждения CS8767)

**Проблема**: Интерфейс `IValueConverter` в .NET 9.0 требует nullable параметры (`object?`), но в конвертерах используются non-nullable (`object`).

**Файлы**:
- `Converters/InverseBoolConverter.cs`
- `Converters/ProgressToWidthConverter.cs`

**Текущая сигнатура**:
```csharp
public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
```

**Должна быть**:
```csharp
public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
```

### 3. Устаревший Application.MainPage (предупреждения CS0618)

**Проблема**: В .NET MAUI 9.0 свойство `Application.MainPage` устарело.

**Файлы**:
- `App.xaml.cs` (строки 25, 32)
- `Converters/ProgressToWidthConverter.cs` (строка 16)
- `ViewModels/PartnerPageViewModel.cs` (строки 86, 92, 98)

**Рекомендация**: Заменить на `Application.Current?.Windows[0]?.Page` для single-window приложений.

**Пример замены**:
```csharp
// Было:
Application.Current.MainPage

// Должно быть:
Application.Current?.Windows[0]?.Page ?? throw new InvalidOperationException("No active window")
```

### 4. Разыменование вероятной пустой ссылки (предупреждения CS8602)

**Файлы**:
- `App.xaml.cs` - разыменование `Application.Current.MainPage` после получения
- `ViewModels/LoginViewModel.cs` - строка 72
- `ViewModels/RegisterViewModel.cs` - строка 124
- `ViewModels/PartnerPageViewModel.cs` - строки 86, 92, 98

**Решение**: Добавить проверки на null или использовать оператор `?.`.

### 5. Поле не инициализировано (предупреждение CS8618)

**Файл**: `Pages/PartnerDetailPage.xaml.cs`

**Проблема**: Поле `partnerId` не инициализировано при выходе из конструктора.

**Текущий код**:
```csharp
private string partnerId;
public string PartnerId
{
    get => partnerId;
    set
    {
        partnerId = value;
        LoadPartnerInfo(partnerId);
    }
}
```

**Решение**: Добавить модификатор `required` или сделать поле nullable (`string?`) с проверкой на null.

### 6. Асинхронный метод без await (предупреждения CS1998)

**Файлы**:
- `Data/DatabaseInitializer.cs` (строка 57)
- `Infrastructure/Auth/AuthenticationService.cs` (строка 43)

**Решение**: Убрать `async` или добавить `await Task.CompletedTask` если метод должен оставаться асинхронным для совместимости.

### 7. Предупреждения о версиях пакетов (NU1608)

**Проблема**: Mapsui.Maui 4.1.9 требует Microsoft.Maui.Controls < 9.0.0, но установлена версия 9.0.100.

**Решение**: Либо обновить Mapsui.Maui до версии, поддерживающей .NET 9.0, либо понизить версию MAUI (не рекомендуется), либо проигнорировать предупреждения, если совместимость подтверждена тестами.

## Задача

Исправь все критические ошибки компиляции (CS0246) и предупреждения, которые препятствуют сборке. Начни с создания класса `NetworkException`, затем исправь остальные проблемы по порядку приоритета.

## Структура файлов для справки

```
Infrastructure/
  Exceptions/
    ApiException.cs  # Здесь нужно добавить NetworkException
  Http/
    ApiClient.cs      # Основное использование NetworkException
```

## Важные замечания

1. Не меняй логику работы кода, только исправляй ошибки компиляции
2. Сохраняй все существующие сообщения об ошибках на русском языке
3. Следуй существующему стилю кода в проекте
4. Все исключения должны быть в пространстве имен `YessGoFront.Infrastructure.Exceptions`
5. NetworkException должен быть логически связан с другими исключениями в проекте

