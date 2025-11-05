using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using YessGoFront.Infrastructure.Ui;

namespace YessGoFront;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // ✅ Проверка соединения с API при старте (остается)
        _ = Task.Run(async () =>
        {
            try
            {
                var clientFactory = MauiProgram.Services.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient("ApiClient");

                var response = await client.GetAsync("health");
                var text = await response.Content.ReadAsStringAsync();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var page = AppUiHelper.TryGetCurrentPage();
                    if (page != null)
                    {
                        await page.DisplayAlert("API Health", text, "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var page = AppUiHelper.TryGetCurrentPage();
                    if (page != null)
                    {
                        await page.DisplayAlert("API Error", $"{ex.GetType().Name}: {ex.Message}", "OK");
                    }
                });
            }
        });
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
