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

#if DEBUG
        // Тест здоровья API — можно оставить, не влияет на навигацию
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000);
                var clientFactory = MauiProgram.Services.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient("ApiClient");

                var baseUrl = client.BaseAddress?.ToString() ?? "unknown";
                System.Diagnostics.Debug.WriteLine($"[App] 🔍 Testing API connection to: {baseUrl}");

                var response = await client.GetAsync("");
                var text = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"[App] ✅ API Health Check: {response.StatusCode} - {text.Substring(0, Math.Min(100, text.Length))}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] ❌ API Health Check FAILED!");
                System.Diagnostics.Debug.WriteLine($"[App] Error: {ex.Message}");
            }
        });
#endif
    }

    /// <summary>
    /// Создаём окно с AppShell без дополнительной навигации.
    /// Вся логика переходов теперь в AppShell (Loaded + токен + PIN).
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
