using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using YessGoFront.Services;
using YessGoFront.Views;

namespace YessGoFront
{
    public partial class AppShell : Shell
    {
        private bool _navigationHandled = false;

        public AppShell()
        {
            InitializeComponent();

            // регистрация маршрутов внутренних страниц
            Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));
            Routing.RegisterRoute(nameof(PartnersListPage), typeof(PartnersListPage));
            // partnerdetails зарегистрирован в AppShell.xaml, поэтому здесь не нужен

            // Подписываемся на событие Navigated для отслеживания успешной навигации
            this.Navigated += OnShellNavigated;
            
            // Используем событие Loaded для гарантии полной инициализации Shell перед навигацией
            this.Loaded += OnShellLoaded;
        }

        private async void OnShellLoaded(object? sender, EventArgs e)
        {
            // Отписываемся от события, чтобы не вызывать повторно
            this.Loaded -= OnShellLoaded;
            
            if (_navigationHandled)
                return;
            
            // Увеличиваем задержку для гарантии полной инициализации Shell и готовности UI
            await Task.Delay(500);
            
            // Выполняем навигацию на главном потоке
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    // Проверяем наличие токена аутентификации
                    var authService = MauiProgram.Services?.GetService<Infrastructure.Auth.IAuthenticationService>();
                    var hasToken = authService != null && await authService.IsAuthenticatedAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[AppShell] Has token: {hasToken}, IsSignedIn: {AccountStore.Instance.IsSignedIn}");
                    
                    if (hasToken || AccountStore.Instance.IsSignedIn)
                    {
                        // Пользователь авторизован - проверяем наличие PIN-кода
                        var domainAuthService = MauiProgram.Services?.GetService<Services.Domain.IAuthService>();
                        if (domainAuthService != null)
                        {
                            var hasPin = await domainAuthService.HasPinAsync();
                            System.Diagnostics.Debug.WriteLine($"[AppShell] Has PIN: {hasPin}");
                            
                            if (hasPin)
                            {
                                // Есть PIN - переходим на страницу ввода PIN
                                System.Diagnostics.Debug.WriteLine("[AppShell] User authenticated, navigating to PIN login");
                                await GoToAsync("///pinlogin", animate: false);
                            }
                            else
                            {
                                // Нет PIN - переходим на главную страницу
                                System.Diagnostics.Debug.WriteLine("[AppShell] User authenticated, no PIN, navigating to main/home");
                                await GoToAsync("///main/home", animate: false);
                            }
                        }
                        else
                        {
                            // Если сервис недоступен, просто переходим на главную
                            System.Diagnostics.Debug.WriteLine("[AppShell] User authenticated, service unavailable, navigating to main/home");
                            await GoToAsync("///main/home", animate: false);
                        }
                        
                        _navigationHandled = true;
                    }
                    else
                    {
                        // Нет токена - переходим на страницу входа
                        System.Diagnostics.Debug.WriteLine("[AppShell] User not authenticated, navigating to login");
                        await GoToAsync("///login", animate: false);
                        _navigationHandled = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AppShell] Navigation error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[AppShell] Stack trace: {ex.StackTrace}");
                    // В случае ошибки переходим на страницу входа
                    try
                    {
                        await GoToAsync("///login", animate: false);
                        _navigationHandled = true;
                    }
                    catch { }
                }
            });
        }

        private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[AppShell] Navigated to: {e.Current?.Location}");
            _navigationHandled = true;
        }
    }
}
