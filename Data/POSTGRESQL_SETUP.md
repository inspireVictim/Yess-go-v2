# Настройка PostgreSQL для проекта

## 📋 Обзор

Проект настроен для работы с PostgreSQL через Entity Framework Core. Используется пакет `Npgsql.EntityFrameworkCore.PostgreSQL`.

## 🔧 Настройка подключения

### 1. Обновление строки подключения

Откройте файл `MauiProgram.cs` и обновите строку подключения в методе `ConfigureSettings`:

```csharp
Database = new DatabaseSettings
{
    ConnectionString = "Host=your_host;Port=5432;Database=yessgo;Username=your_username;Password=your_password",
    EnableSqlLogging = false // true для логирования SQL запросов в Debug режиме
}
```

### 2. Формат строки подключения

**Базовый формат:**
```
Host=localhost;Port=5432;Database=yessgo;Username=postgres;Password=password
```

**С дополнительными параметрами:**
```
Host=localhost;Port=5432;Database=yessgo;Username=postgres;Password=password;Timeout=30;Command Timeout=30;Pooling=true;Maximum Pool Size=100
```

**Параметры строки подключения:**
- `Host` - адрес сервера PostgreSQL
- `Port` - порт (по умолчанию 5432)
- `Database` - имя базы данных
- `Username` - имя пользователя
- `Password` - пароль
- `Timeout` - таймаут подключения в секундах
- `Command Timeout` - таймаут выполнения команд в секундах
- `Pooling` - включить пул подключений (по умолчанию true)
- `Maximum Pool Size` - максимальный размер пула

### 3. Использование переменных окружения

Для безопасности можно использовать переменные окружения:

```csharp
ConnectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") 
    ?? "Host=localhost;Port=5432;Database=yessgo;Username=postgres;Password=default_password"
```

**Установка переменной окружения (Windows):**
```powershell
$env:DATABASE_CONNECTION_STRING = "Host=localhost;Port=5432;Database=yessgo;Username=postgres;Password=password"
```

**Установка переменной окружения (Linux/Mac):**
```bash
export DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=yessgo;Username=postgres;Password=password"
```

## 📦 Структура проекта

```
Data/
├── AppDbContext.cs              # Основной контекст базы данных
├── DatabaseConnectionService.cs # Сервис для получения строки подключения
├── DatabaseInitializer.cs       # Инициализация и миграции БД
└── Entities/
    └── README.md                # Инструкции по созданию сущностей
```

## 🗄️ Создание сущностей

### Пример создания сущности:

1. Создайте файл в `Data/Entities/`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YessGoFront.Data.Entities;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? FirstName { get; set; }
    
    [MaxLength(50)]
    public string? LastName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

2. Добавьте DbSet в `AppDbContext.cs`:

```csharp
public DbSet<User> Users { get; set; } = null!;
```

3. При необходимости добавьте конфигурацию в `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.Email).IsUnique();
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    });
}
```

## 🔄 Миграции

Для создания и применения миграций используйте EF Core CLI инструменты.

**Создание миграции:**
```bash
dotnet ef migrations add InitialCreate --project YessGoFront.csproj
```

**Применение миграций:**
```bash
dotnet ef database update --project YessGoFront.csproj
```

**Откат миграции:**
```bash
dotnet ef database update PreviousMigrationName --project YessGoFront.csproj
```

**Удаление последней миграции:**
```bash
dotnet ef migrations remove --project YessGoFront.csproj
```

⚠️ **Примечание:** Для использования EF Core CLI инструментов в MAUI проекте может потребоваться установка:
```bash
dotnet tool install --global dotnet-ef
```

## 💾 Использование в коде

### Получение DbContext через DI:

```csharp
public class MyService
{
    private readonly AppDbContext _context;

    public MyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> CreateUserAsync(string email, string firstName)
    {
        var user = new User
        {
            Email = email,
            FirstName = firstName,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user;
    }
}
```

### Использование в ViewModel:

```csharp
public partial class MyViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    public MyViewModel(AppDbContext context)
    {
        _context = context;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var users = await _context.Users.ToListAsync();
        // Обработка данных
    }
}
```

## 🚀 Инициализация базы данных

База данных автоматически инициализируется при запуске приложения в `App.xaml.cs`.

Для применения миграций вручную:

```csharp
using var scope = services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await context.MigrateAsync();
```

## 🔍 Логирование SQL запросов

Для включения логирования SQL запросов установите `EnableSqlLogging = true` в настройках:

```csharp
Database = new DatabaseSettings
{
    ConnectionString = "...",
    EnableSqlLogging = true
}
```

SQL запросы будут логироваться в Debug Output (Visual Studio / Rider).

## ⚠️ Важные замечания

1. **Безопасность паролей:** Не храните пароли в коде. Используйте переменные окружения или безопасное хранилище.

2. **Подключения:** DbContext регистрируется как Scoped, что означает один экземпляр на страницу/запрос. Не нужно создавать DbContext вручную.

3. **Асинхронные операции:** Всегда используйте асинхронные методы (`ToListAsync`, `SaveChangesAsync` и т.д.).

4. **Миграции в продакшене:** Не используйте `EnsureCreatedAsync()` в продакшене. Используйте миграции через `MigrateAsync()`.

5. **Пулы подключений:** PostgreSQL провайдер автоматически использует пул подключений для улучшения производительности.

## 📚 Дополнительные ресурсы

- [PostgreSQL документация](https://www.postgresql.org/docs/)
- [Npgsql документация](https://www.npgsql.org/doc/)
- [Entity Framework Core документация](https://learn.microsoft.com/ef/core/)
- [EF Core PostgreSQL провайдер](https://www.npgsql.org/efcore/)

