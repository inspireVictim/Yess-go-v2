using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using YessGoFront.Config;
using YessGoFront.Data;
using YessGoFront.Infrastructure.Auth;
using YessGoFront.Infrastructure.Http;
using YessGoFront.Infrastructure.Http.HttpMessageHandlers;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;
using ZXing.Net.Maui.Controls;

namespace YessGoFront;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseBarcodeReader();

        // Конфигурация приложения
        ConfigureSettings(builder.Services);
        ConfigureDatabase(builder.Services);
        ConfigureHttpClients(builder.Services);
        ConfigureServices(builder.Services);
        ConfigureViewModels(builder.Services);
        ConfigureLogging(builder);

        var app = builder.Build();
        Services = app.Services;

        // 🧪 Проверка API при запуске
        _ = Task.Run(async () =>
        {
            try
            {
                var settings = Services.GetRequiredService<AppSettings>();
                System.Diagnostics.Debug.WriteLine($"[AppSettings] BaseUrl = {settings.Api.BaseUrl}");

                var clientFactory = Services.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient("ApiClient");

                var response = await client.GetAsync("api/v1/health");
                var text = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[HEALTH TEST] ✅ {response.StatusCode}: {text}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HEALTH TEST] ❌ ERROR: {ex.Message}");
            }
        });

        return app;
    }

    private static string GetDatabaseConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(connectionString))
            return connectionString;

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            try
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                var username = Uri.UnescapeDataString(userInfo[0]);
                var password = Uri.UnescapeDataString(userInfo[1]);
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');
                return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
            }
            catch { }
        }

        return "Host=localhost;Port=5432;Database=yess_loyalty;Username=yess_user;Password=password";
    }

    private static string GetDefaultApiBaseUrl()
    {
#if ANDROID
        // Для эмулятора: 10.0.2.2
        // Для реального телефона: IP компьютера в локальной сети
        // Можно установить через переменную окружения: API_BASE_URL=http://10.0.2.2:8000/ (для эмулятора)
        // По умолчанию используем IP компьютера (для реального телефона)
        return "http://192.168.2.155:8000/";  // IP компьютера для реального телефона
#else
        return "http://192.168.2.155:8000/";
#endif
    }

    private static void ConfigureSettings(IServiceCollection services)
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? GetDefaultApiBaseUrl();

        // Логируем используемый URL для отладки
        System.Diagnostics.Debug.WriteLine($"[MauiProgram] API Base URL: {apiBaseUrl}");

        var dbConnectionString = GetDatabaseConnectionString();

        services.AddSingleton<AppSettings>(_ => new AppSettings
        {
            Api = new ApiSettings
            {
                BaseUrl = apiBaseUrl,
                ApiVersion = "v1",
                RequestTimeoutSeconds = 30
            },
            Timeouts = new TimeoutSettings
            {
                RequestTimeout = 30,
                RetryAttempts = 3,
                RetryDelayMs = 1000
            },
            Database = new DatabaseSettings
            {
                ConnectionString = dbConnectionString,
                EnableSqlLogging = false
            }
        });
    }

    private static void ConfigureDatabase(IServiceCollection services)
    {
        services.AddSingleton<IDatabaseConnectionService, DatabaseConnectionService>();

        services.AddScoped<AppDbContext>(serviceProvider =>
        {
            var connectionService = serviceProvider.GetRequiredService<IDatabaseConnectionService>();
            var logger = serviceProvider.GetService<ILogger<AppDbContext>>();
            var connectionString = connectionService.GetConnectionString();
            var enableSqlLogging = connectionService.IsSqlLoggingEnabled();

            return new AppDbContext(connectionString, enableSqlLogging, logger);
        });
    }

    // ✅ Самое важное – единый HttpClient для всех API
    private static void ConfigureHttpClients(IServiceCollection services)
    {
        services.AddTransient<AuthHandler>();
        services.AddTransient<LoggingHandler>();

        services.AddHttpClient("ApiClient", (sp, client) =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            client.BaseAddress = new Uri(settings.Api.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.Api.RequestTimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AuthHandler>()
        .AddHttpMessageHandler<LoggingHandler>();

        services.AddHttpClient<IAuthApiService, AuthApiService>("ApiClient");
        services.AddHttpClient<IPartnersApiService, PartnersApiService>("ApiClient");
        services.AddHttpClient<IWalletApiService, WalletApiService>("ApiClient");
        services.AddHttpClient<IQRApiService, QRApiService>("ApiClient");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Auth
        services.AddSingleton<IAuthenticationService, AuthenticationService>();

        // Domain
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPartnersService, PartnersService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IQRService, QRService>();
    }

    private static void ConfigureViewModels(IServiceCollection services)
    {
        // Регистрируй ViewModels при необходимости
    }

    private static void ConfigureLogging(MauiAppBuilder builder)
    {
#if DEBUG
        builder.Services.AddLogging(logging => logging.AddDebug());
#endif
    }
}
