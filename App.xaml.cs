using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using YessGoFront.Data;
using YessGoFront.Infrastructure.Ui;

namespace YessGoFront;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // ✅ Проверка соединения с API при старте (только для отладки)
#if DEBUG
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000); // Ждём, пока приложение полностью запустится
                var clientFactory = MauiProgram.Services.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient("ApiClient");

                var baseUrl = client.BaseAddress?.ToString() ?? "unknown";
                System.Diagnostics.Debug.WriteLine($"[App] 🔍 Testing API connection to: {baseUrl}");

                // Проверяем корневой endpoint
                var response = await client.GetAsync("");
                var text = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[App] ✅ API Health Check: {response.StatusCode} - {text.Substring(0, Math.Min(100, text.Length))}");
            }
            catch (Exception ex)
            {
                // Логируем подробную информацию об ошибке
                System.Diagnostics.Debug.WriteLine($"[App] ❌ API Health Check FAILED!");
                System.Diagnostics.Debug.WriteLine($"[App] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[App] ⚠️ Check if backend is running and IP address is correct!");
                System.Diagnostics.Debug.WriteLine($"[App] ⚠️ Set API_BASE_URL environment variable if needed!");
            }
        });
#endif
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Инициализируем базу данных при запуске приложения
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500); // Небольшая задержка для полной инициализации сервисов
                await InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Database initialization error: {ex.Message}");
            }
        });

        return new Window(new AppShell());
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var services = MauiProgram.Services;
            if (services == null) return;

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILogger<DatabaseInitializer>>();
            var initializer = new DatabaseInitializer(context, logger);

            await initializer.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("[App] Database initialized successfully");
        }
        catch (Exception ex)
        {
            // Логируем ошибку, но не прерываем работу приложения
            System.Diagnostics.Debug.WriteLine($"[App] Database initialization error: {ex.Message}");
        }
    }
}
