using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
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

                // Проверяем корневой endpoint вместо /health
                var response = await client.GetAsync("");
                var text = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[App] API Health Check: {response.StatusCode} - {text}");
            }
            catch (Exception ex)
            {
                // Только логируем, не показываем ошибку пользователю
                System.Diagnostics.Debug.WriteLine($"[App] API Health Check failed: {ex.Message}");
            }
        });
#endif
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // ✅ Просто создаем главное окно без инициализации базы данных
        return new Window(new AppShell());
    }

    /*
    ❌ Удаляем/отключаем инициализацию локальной базы данных
    private async void InitializeDatabase()
    {
        try
        {
            var services = Handler?.MauiContext?.Services;
            if (services == null) return;

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var initializer = new DatabaseInitializer(
                context,
                scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILogger<DatabaseInitializer>>());

            await initializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
        }
    }
    */
}
