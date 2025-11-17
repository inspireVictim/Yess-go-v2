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
using Microsoft.Maui.Handlers;
#if ANDROID
using YessGoFront.Platforms.Android.Handlers;
#endif

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

        // -----------------------------------------------
        // ✅ Регистрируем наш Emoji-safe Entry handler
        // Использует Android.Widget.EditText вместо AppCompatEditText
        // Это полностью избегает EmojiCompat, который активируется только для AppCompatEditText
        // -----------------------------------------------
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<Entry, NoEmojiEntryHandler>();
#endif
        });

        // -----------------------------------------------
        // Твой код конфигурации — оставлен как есть
        // -----------------------------------------------
        ConfigureSettings(builder.Services);
        ConfigureDatabase(builder.Services);
        ConfigureHttpClients(builder.Services);
        ConfigureServices(builder.Services);
        ConfigureViewModels(builder.Services);
        ConfigureLogging(builder);

        var app = builder.Build();
        Services = app.Services;

        // Тест API
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

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "yessgo.db");
        return $"Data Source={dbPath}";
    }

    private static string GetDefaultApiBaseUrl()
    {
#if ANDROID
        try
        {
            var fingerprint = Android.OS.Build.Fingerprint ?? "";
            var model = Android.OS.Build.Model ?? "";
            var product = Android.OS.Build.Product ?? "";
            var manufacturer = Android.OS.Build.Manufacturer ?? "";

            var isEmulator =
                fingerprint.Contains("generic", StringComparison.OrdinalIgnoreCase) ||
                fingerprint.Contains("emulator", StringComparison.OrdinalIgnoreCase) ||
                fingerprint.Contains("sdk", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("Emulator", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("sdk", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("gphone", StringComparison.OrdinalIgnoreCase) ||
                product.Contains("emulator", StringComparison.OrdinalIgnoreCase) ||
                manufacturer.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
                manufacturer.Equals("Genymotion", StringComparison.OrdinalIgnoreCase);

            if (isEmulator)
            {
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Using EMULATOR address");
                return "http://10.0.2.2:8000/";
            }
        }
        catch { }

        var envUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrEmpty(envUrl))
            return envUrl;

        return "http://192.168.1.7:8000/";
#else
        return "http://localhost:8000/";
#endif
    }

    private static void ConfigureSettings(IServiceCollection services)
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? GetDefaultApiBaseUrl();

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
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<YessGoFront.Services.ILocationService, YessGoFront.Services.LocationService>();
        services.AddSingleton<YessGoFront.Services.IImageCacheService, YessGoFront.Services.ImageCacheService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPartnersService, PartnersService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IQRService, QRService>();
    }

    private static void ConfigureViewModels(IServiceCollection services)
    {
        // Register viewmodels here
        services.AddTransient<ViewModels.TransactionsViewModel>();
        services.AddTransient<ViewModels.TransactionDetailsViewModel>();
    }

    private static void ConfigureLogging(MauiAppBuilder builder)
    {
#if DEBUG
        builder.Services.AddLogging(logging => logging.AddDebug());
#endif
    }
}
