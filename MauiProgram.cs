using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using SkiaSharp.Views.Maui.Controls.Hosting;
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
            .UseSkiaSharp(true)
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
        // Проверяем переменную окружения для SQLite
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(connectionString))
            return connectionString;

        // По умолчанию используем SQLite в локальной папке данных приложения
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "yessgo.db");
        return $"Data Source={dbPath}";
    }

    private static string GetDefaultApiBaseUrl()
    {
#if ANDROID
        // 🤖 ANDROID: Автоматически определяем эмулятор или реальный телефон
        try
        {
            // Попытка определить эмулятор несколькими способами
            var isEmulator = 
                Android.OS.Build.Fingerprint?.Contains("generic") == true || 
                Android.OS.Build.Fingerprint?.Contains("emulator") == true ||
                Android.OS.Build.Model?.Contains("Emulator") == true ||
                Android.OS.Build.Model?.Contains("emulator") == true ||
                Android.OS.Build.Product?.Contains("emulator") == true ||
                Android.OS.Build.Manufacturer?.Equals("unknown") == true;
            
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Emulator detection: {isEmulator}");
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Build.Fingerprint: {Android.OS.Build.Fingerprint}");
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Build.Model: {Android.OS.Build.Model}");
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Build.Product: {Android.OS.Build.Product}");
            
            if (isEmulator)
            {
                // 🔷 Эмулятор: используем специальный IP 10.0.2.2
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Using EMULATOR address: 10.0.2.2:8000");
                return "http://10.0.2.2:8000/";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Error detecting emulator: {ex.Message}");
        }
        
        // 📱 Реальный телефон: используем IP из переменной окружения
        var envUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrEmpty(envUrl))
        {
            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Using URL from API_BASE_URL: {envUrl}");
            return envUrl;
        }
        
        // 📱 Дефолт для реального телефона
        // IP вашего компьютера в локальной сети: 192.168.2.155
        var defaultPhoneUrl = "http://192.168.2.155:8000/";
        System.Diagnostics.Debug.WriteLine($"[MauiProgram] Using DEFAULT for real phone: {defaultPhoneUrl}");
        return defaultPhoneUrl;
#else
        // 🖥️ Desktop (WinUI/WPF): используем localhost
        return "http://localhost:8000/";
#endif
    }

    private static void ConfigureSettings(IServiceCollection services)
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? GetDefaultApiBaseUrl();

        // Логируем используемый URL для отладки
        System.Diagnostics.Debug.WriteLine($"[MauiProgram] ======================================");
        System.Diagnostics.Debug.WriteLine($"[MauiProgram] API BASE URL: {apiBaseUrl}");
        System.Diagnostics.Debug.WriteLine($"[MauiProgram] ======================================");

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
        services.AddHttpClient<IBannerApiService, BannerApiService>("ApiClient");
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
