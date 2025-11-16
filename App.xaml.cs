using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using YessGoFront.Data;
using YessGoFront.Infrastructure.Ui;

namespace YessGoFront;

public partial class App : Application
{
    private const string TokenKey = "access_token";   // ✔️ Единый ключ

    public App()
    {
        InitializeComponent();

#if DEBUG
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

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        window.Created += async (_, __) =>
        {
            try
            {
                var token = await SecureStorage.GetAsync(TokenKey);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        System.Diagnostics.Debug.WriteLine("[App] No token → go to login");
                        await Shell.Current.GoToAsync("//login");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[App] Token found → go to PIN login (or main if no PIN)");
                        await Shell.Current.GoToAsync("//pinlogin");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Navigation error: {ex.Message}");
            }
        };

        return window;
    }
}
