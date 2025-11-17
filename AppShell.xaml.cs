using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using YessGoFront.Services.Domain;
using YessGoFront.Views;

namespace YessGoFront
{
    public partial class AppShell : Shell
    {
        private bool _initialized;

        public AppShell()
        {
            InitializeComponent();

            // Регистрация внутренних маршрутов
            Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));
            Routing.RegisterRoute(nameof(PartnersListPage), typeof(PartnersListPage));
            Routing.RegisterRoute(nameof(PartnerPage), typeof(PartnerPage));
            Routing.RegisterRoute(nameof(PartnerDetailPage), typeof(PartnerDetailPage));
            Routing.RegisterRoute(nameof(TransactionsPage), typeof(TransactionsPage));
            Routing.RegisterRoute(nameof(TransactionDetailsPage), typeof(TransactionDetailsPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_initialized)
                return;

            _initialized = true;

            Debug.WriteLine("[AppShell] OnAppearing: starting startup auth/PIN check");

            try
            {
                var authService = MauiProgram.Services.GetService<IAuthService>();

                if (authService == null)
                {
                    Debug.WriteLine("[AppShell] OnAppearing: IAuthService is null → navigating to login");
                    await Shell.Current.GoToAsync("///login", animate: false);
                    return;
                }

                var isAuthenticated = await authService.IsAuthenticatedAsync();
                Debug.WriteLine($"[AppShell] OnAppearing: IsAuthenticated={isAuthenticated}");

                if (!isAuthenticated)
                {
                    Debug.WriteLine("[AppShell] Decision: unauthenticated → navigating to login");
                    await Shell.Current.GoToAsync("///login", animate: false);
                    return;
                }

                var hasValidPin = await authService.HasPinAsync();
                Debug.WriteLine($"[AppShell] OnAppearing: hasValidPin={hasValidPin}");

                if (!hasValidPin)
                {
                    Debug.WriteLine("[AppShell] Decision: authenticated but NO valid PIN → navigating to PIN creation");
                    await Shell.Current.GoToAsync("///pinlogin?isCreatingPin=true", animate: false);
                }
                else
                {
                    Debug.WriteLine("[AppShell] Decision: authenticated WITH valid PIN → navigating to PIN login");
                    await Shell.Current.GoToAsync("///pinlogin", animate: false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell] OnAppearing: error during startup routing: {ex.Message}");
                Debug.WriteLine($"[AppShell] StackTrace: {ex.StackTrace}");

                // Fallback: отправляем на экран логина
                try
                {
                    await Shell.Current.GoToAsync("///login", animate: false);
                }
                catch
                {
                    // Игнорируем вторичную ошибку навигации
                }
            }
        }
    }
}
