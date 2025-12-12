using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using YessGoFront.Data;
using YessGoFront.Infrastructure.Ui;
using YessGoFront.Services;
using YessGoFront.Services.Domain;

namespace YessGoFront;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // API health check отложен для оптимизации запуска
    }

    /// <summary>
    /// Создаём окно с AppShell без дополнительной навигации.
    /// Вся логика переходов теперь в AppShell (Loaded + токен + PIN).
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override void OnResume()
    {
        base.OnResume();

        // Обновляем токены и баланс при возврате приложения в фокус
        Task.Run(async () =>
        {
            try
            {
                var scopeFactory = MauiProgram.Services.GetService<IServiceScopeFactory>();
                if (scopeFactory != null)
                {
                    using var scope = scopeFactory.CreateScope();
                    
                    // 1. Сначала обновляем токены, чтобы сессия не терялась
                    try
                    {
                        var globalAuthService = scope.ServiceProvider.GetService<GlobalAuthService>();
                        if (globalAuthService != null)
                        {
                            System.Diagnostics.Debug.WriteLine("[App] OnResume: Checking and refreshing tokens...");
                            
                            // Используем таймаут для обновления токенов (10 секунд)
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                            var tokensValid = await globalAuthService.EnsureValidTokensAsync(cts.Token);
                            
                            if (tokensValid)
                            {
                                System.Diagnostics.Debug.WriteLine("[App] OnResume: Tokens are valid or refreshed successfully");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[App] OnResume: Failed to refresh tokens, user may need to login");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine("[App] OnResume: Token refresh timed out");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App] OnResume: Error refreshing tokens: {ex.Message}");
                    }
                    
                    // 2. Затем обновляем баланс
                    try
                    {
                        var walletService = scope.ServiceProvider.GetService<IWalletService>();
                        if (walletService != null)
                        {
                            // Используем таймаут для запроса баланса (10 секунд)
                            using var balanceCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                            var balance = await walletService.GetBalanceAsync(balanceCts.Token);
                            BalanceStore.Instance.Balance = balance;
                            System.Diagnostics.Debug.WriteLine($"[App] OnResume: Balance refreshed: {balance}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine("[App] OnResume: Balance refresh timed out");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App] OnResume: Error refreshing balance: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] OnResume: Unexpected error: {ex.Message}");
            }
        });
    }
}
