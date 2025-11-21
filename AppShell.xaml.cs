using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using YessGoFront.Services.Domain;
using YessGoFront.Views;
using YessGoFront.Services;

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

                // Проверяем, есть ли сохранённый аккаунт (AccountStore)
                AccountStore.Instance.Load(); // Перечитываем данные из Preferences
                var hasSavedAccount = AccountStore.Instance.IsSignedIn;
                Debug.WriteLine($"[AppShell] OnAppearing: HasSavedAccount={hasSavedAccount}");

                // Проверяем, есть ли токен аутентификации
                var isAuthenticated = await authService.IsAuthenticatedAsync();
                Debug.WriteLine($"[AppShell] OnAppearing: IsAuthenticated={isAuthenticated}");

                // Проверяем наличие refresh_token - он необходим для автоматического обновления токенов
                var authenticationService = MauiProgram.Services.GetService<YessGoFront.Infrastructure.Auth.IAuthenticationService>();
                var hasRefreshToken = false;
                if (authenticationService != null)
                {
                    var refreshToken = await authenticationService.GetRefreshTokenAsync();
                    hasRefreshToken = !string.IsNullOrWhiteSpace(refreshToken);
                    Debug.WriteLine($"[AppShell] OnAppearing: HasRefreshToken={hasRefreshToken}");
                }

                // Если нет refresh_token - нужно перелогиниться для получения нового
                if (!hasRefreshToken)
                {
                    Debug.WriteLine("[AppShell] Decision: no refresh_token → navigating to login (need to re-login for refresh_token)");
                    // Очищаем старые токены и PIN
                    if (authenticationService != null)
                    {
                        await authenticationService.ClearTokensAsync();
                    }
                    var pinService = MauiProgram.Services?.GetService<Services.PinStorageService>();
                    if (pinService != null)
                    {
                        await pinService.ClearPinAsync();
                    }
                    AccountStore.Instance.SignOut(keepProfile: false);
                    await Shell.Current.GoToAsync("///login", animate: false);
                    return;
                }

                // Если нет сохранённого аккаунта И нет токена - идём на логин
                if (!hasSavedAccount && !isAuthenticated)
                {
                    Debug.WriteLine("[AppShell] Decision: no saved account and not authenticated → navigating to login");
                    await Shell.Current.GoToAsync("///login", animate: false);
                    return;
                }

                // Если есть сохранённый аккаунт ИЛИ токен - проверяем PIN
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
