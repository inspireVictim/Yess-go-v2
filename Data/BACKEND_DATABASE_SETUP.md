# Настройка подключения к базе данных бэкенда

## Обзор

После клонирования репозитория бэкенда и создания Entity Framework сущностей, необходимо настроить подключение к PostgreSQL базе данных.

## Структура базы данных

Бэкенд использует PostgreSQL со следующими основными таблицами:
- `users` - Пользователи
- `wallets` - Кошельки пользователей
- `partners` - Партнёры
- `partner_locations` - Локации партнёров
- `transactions` - Транзакции
- `orders` - Заказы
- `notifications` - Уведомления
- `promotions` - Акции и промо-кампании
- `cities` - Города
- И другие...

## Настройка строки подключения

### Способ 1: Переменная окружения (рекомендуется)

Создайте переменную окружения `DATABASE_CONNECTION_STRING`:

```powershell
# Windows PowerShell
$env:DATABASE_CONNECTION_STRING = "Host=localhost;Port=5432;Database=yess_loyalty;Username=yess_user;Password=your_password"
```

Или через системные переменные окружения.

### Способ 2: Прямое редактирование MauiProgram.cs

В файле `MauiProgram.cs` в методе `ConfigureSettings` измените строку подключения:

```csharp
ConnectionString = "Host=localhost;Port=5432;Database=yess_loyalty;Username=yess_user;Password=your_password"
```

### Способ 3: Использование DATABASE_URL (как в бэкенде)

Если у вас уже настроен `DATABASE_URL` в формате PostgreSQL:

```bash
DATABASE_URL=postgresql://yess_user:password@localhost/yess_loyalty
```

Приложение автоматически преобразует его в формат Npgsql.

## Параметры подключения

| Параметр | Описание | Пример |
|----------|----------|--------|
| Host | Адрес сервера PostgreSQL | localhost |
| Port | Порт сервера | 5432 |
| Database | Имя базы данных | yess_loyalty |
| Username | Имя пользователя | yess_user |
| Password | Пароль | your_password |

## Настройка бэкенда

Убедитесь, что бэкенд настроен и база данных создана. См. документацию в репозитории бэкенда:

```
../Yess-Go-App-Backend/Yess-Money---app-master/yess-backend/README.md
```

## Применение миграций

После настройки подключения, создайте и примените миграции Entity Framework:

```powershell
# Создание миграции
dotnet ef migrations add InitialCreate --project "YessGoFront.csproj"

# Применение миграций
dotnet ef database update --project "YessGoFront.csproj"
```

**Важно:** Убедитесь, что структура БД в Entity Framework соответствует структуре БД бэкенда, так как обе системы должны работать с одной базой данных.

## Проверка подключения

При запуске приложения автоматически вызывается `DatabaseInitializer.InitializeAsync()`, который:
1. Проверяет подключение к базе данных
2. Применяет миграции (если они есть)
3. Создаёт базу данных, если её нет

## Логирование SQL

Для отладки можно включить логирование SQL запросов:

```csharp
EnableSqlLogging = true // Только в Debug режиме!
```

Это поможет увидеть все SQL запросы, выполняемые Entity Framework.

## Безопасность

⚠️ **Не коммитьте строки подключения с паролями в Git!**

Используйте:
- Переменные окружения
- Файлы конфигурации, исключённые из Git (например, `appsettings.local.json`)
- Системы управления секретами (Azure Key Vault, AWS Secrets Manager и т.д.)

