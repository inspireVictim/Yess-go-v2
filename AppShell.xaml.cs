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
            await Task.Delay(300);
            
            // Выполняем навигацию на главном потоке
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (AccountStore.Instance.IsSignedIn)
                    {
                        System.Diagnostics.Debug.WriteLine("[AppShell] User is signed in, navigating to main/home");
                        
                        // Используем GoToAsync для гарантированной навигации
                        // Три слеша (///) означают абсолютную навигацию с очисткой стека
                        await GoToAsync("///main/home", animate: false);
                        _navigationHandled = true;
                        System.Diagnostics.Debug.WriteLine("[AppShell] Navigation to main/home completed");
                        
                        // Принудительно обновляем UI после небольшой задержки
                        await Task.Delay(50);
                        System.Diagnostics.Debug.WriteLine($"[AppShell] Current route after navigation: {this.CurrentState?.Location}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[AppShell] User is not signed in, navigating to login");
                        await GoToAsync("///login", animate: false);
                        _navigationHandled = true;
                        System.Diagnostics.Debug.WriteLine("[AppShell] Navigation to login completed");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AppShell] Navigation error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[AppShell] Stack trace: {ex.StackTrace}");
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
