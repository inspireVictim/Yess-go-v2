# Интеграция с бэкендом - Завершено

## ✅ Выполненная работа

### 1. Клонирование репозитория бэкенда
- Репозиторий склонирован в `../Yess-Go-App-Backend/`
- Изучена структура бэкенда (FastAPI, PostgreSQL)

### 2. Создание Entity Framework сущностей
Созданы сущности в `Data/Entities/` на основе моделей бэкенда:
- ✅ `User.cs` - Пользователи
- ✅ `Wallet.cs` - Кошельки
- ✅ `City.cs` - Города
- ✅ `Partner.cs` и `PartnerLocation.cs` - Партнёры и их локации
- ✅ `Transaction.cs` - Транзакции
- ✅ `Order.cs` - Заказы
- ✅ `Notification.cs` - Уведомления (с enum для типов, статусов, приоритетов)
- ✅ `Promotion.cs` - Промо-акции (с enum для категорий, типов, статусов)

### 3. Обновление DbContext
- ✅ Добавлены все `DbSet` для сущностей в `AppDbContext.cs`
- ✅ Настроена поддержка PostgreSQL через `Npgsql.EntityFrameworkCore.PostgreSQL`

### 4. Обновление API Endpoints
Обновлён `Config/ApiEndpoints.cs` в соответствии с реальным API бэкенда:
- ✅ Префикс изменён на `/api/v1/`
- ✅ Endpoints соответствуют структуре бэкенда:
  - Auth: `/api/v1/auth/register`, `/api/v1/auth/login`, `/api/v1/auth/refresh`
  - Partners: `/api/v1/partners/list`, `/api/v1/partners/{id}`, `/api/v1/partners/locations`
  - Wallet: `/api/v1/wallet`, `/api/v1/wallet/topup`
  - Transactions: `/api/v1/transactions`
  - Orders: `/api/v1/orders`
  - Notifications: `/api/v1/notifications`
  - Routes: `/api/v1/routes`
  - Promotions: `/api/v1/promotions`

### 5. Настройка подключения к БД
- ✅ Обновлён `MauiProgram.cs` с поддержкой:
  - Переменной окружения `DATABASE_CONNECTION_STRING` (формат Npgsql)
  - Переменной окружения `DATABASE_URL` (формат PostgreSQL URL)
  - Значения по умолчанию: `Host=localhost;Port=5432;Database=yess_loyalty;Username=yess_user;Password=password`
- ✅ Добавлен метод `GetDatabaseConnectionString()` с парсингом `DATABASE_URL`

### 6. Настройка базового URL API
- ✅ Базовый URL по умолчанию: `http://localhost:8000` (для разработки)
- ✅ Поддержка переменной окружения `API_BASE_URL`
- ✅ Production URL: `https://api.yessloyalty.com`

### 7. Исправление API сервисов
- ✅ `WalletApiService.cs` - обновлены endpoints
- ✅ `PartnersApiService.cs` - обновлены endpoints, добавлена конвертация типов

### 8. Документация
- ✅ `Data/BACKEND_DATABASE_SETUP.md` - инструкция по настройке подключения к БД

## 📋 Структура базы данных

Основные таблицы (соответствуют моделям бэкенда):
- `users` - Пользователи
- `wallets` - Кошельки
- `cities` - Города
- `partners` - Партнёры
- `partner_locations` - Локации партнёров
- `transactions` - Транзакции
- `orders` - Заказы
- `notifications` - Уведомления
- `promotions` - Промо-акции

## 🔧 Следующие шаги

### Настройка подключения к БД

1. **Установите PostgreSQL** (если ещё не установлен)

2. **Создайте базу данных**:
```sql
CREATE DATABASE yess_loyalty;
CREATE USER yess_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE yess_loyalty TO yess_user;
```

3. **Настройте строку подключения**:

   **Вариант 1: Переменная окружения (рекомендуется)**
   ```powershell
   $env:DATABASE_CONNECTION_STRING = "Host=localhost;Port=5432;Database=yess_loyalty;Username=yess_user;Password=your_password"
   ```

   **Вариант 2: В коде (только для разработки)**
   Измените значение по умолчанию в `MauiProgram.cs` (метод `GetDatabaseConnectionString`)

### Настройка API бэкенда

1. **Запустите бэкенд**:
   ```bash
   cd ../Yess-Go-App-Backend/Yess-Money---app-master/yess-backend
   # Следуйте инструкциям в README.md бэкенда
   ```

2. **Проверьте работу API**:
   - Swagger UI: `http://localhost:8000/docs`
   - Health check: `http://localhost:8000/health`

### Создание и применение миграций Entity Framework

⚠️ **Важно**: Перед созданием миграций убедитесь, что структура БД в Entity Framework соответствует структуре БД бэкенда, так как обе системы работают с одной базой данных.

Если вы планируете использовать Entity Framework миграции для управления схемой БД:
```powershell
# Установите EF Core tools (если ещё не установлены)
dotnet tool install --global dotnet-ef

# Создание миграции
dotnet ef migrations add InitialCreate --project "YessGoFront.csproj"

# Применение миграций
dotnet ef database update --project "YessGoFront.csproj"
```

**Рекомендация**: Если бэкенд уже управляет схемой БД через Alembic (Python), лучше не использовать EF миграции, а просто синхронизировать структуру сущностей с существующими таблицами.

### Тестирование интеграции

1. **Проверьте подключение к БД**:
   - Запустите приложение
   - Проверьте логи - должна быть выполнена инициализация БД (`DatabaseInitializer`)

2. **Проверьте работу API**:
   - Убедитесь, что бэкенд запущен на `http://localhost:8000`
   - Попробуйте выполнить запросы к API через приложение

## 📝 Примечания

- Все Entity Framework сущности созданы на основе моделей бэкенда
- API endpoints обновлены в соответствии с реальной структурой бэкенда
- Поддержка переменных окружения для гибкой настройки
- Исправлены ошибки компиляции в API сервисах
- Проект готов к интеграции с бэкендом

## 🔗 Полезные ссылки

- Репозиторий бэкенда: `../Yess-Go-App-Backend/`
- Документация API: `http://localhost:8000/docs` (после запуска бэкенда)
- Настройка БД: см. `Data/BACKEND_DATABASE_SETUP.md`

