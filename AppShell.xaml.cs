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
            Routing.RegisterRoute(nameof(PolicyPage), typeof(PolicyPage));
            Routing.RegisterRoute(nameof(ConditionsPage), typeof(ConditionsPage));
            Routing.RegisterRoute(nameof(ContactsPage), typeof(ContactsPage));
            Routing.RegisterRoute(nameof(PublicOfferPage), typeof(PublicOfferPage));
            Routing.RegisterRoute(nameof(RefundPolicyPage), typeof(RefundPolicyPage));
            Routing.RegisterRoute(nameof(PaymentSecurityPage), typeof(PaymentSecurityPage));
            Routing.RegisterRoute(nameof(DeliveryTermsPage), typeof(DeliveryTermsPage));
            Routing.RegisterRoute(nameof(FeedbackPage), typeof(FeedbackPage));
            Routing.RegisterRoute(nameof(CertificatePage), typeof(CertificatePage));


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

                // Получаем сервис аутентификации один раз для использования во всех блоках
                var authenticationService = MauiProgram.Services.GetService<YessGoFront.Infrastructure.Auth.IAuthenticationService>();

                // 1. Проверяем, есть ли пользователь в локальной SQLite БД
                var localUser = await authService.GetLocalUserAsync();
                Debug.WriteLine($"[AppShell] OnAppearing: LocalUser exists={localUser != null} (UserId={localUser?.Id ?? 0})");

                if (localUser != null)
                {
                    // Пользователь есть в локальной БД - всегда требуем PIN или биометрию для безопасности
                    Debug.WriteLine("[AppShell] Decision: local user found → always require PIN/biometric");
                    
                    // Пытаемся обновить токен в фоне, если он истек (но не пропускаем экран PIN)
                    if (authenticationService != null)
                    {
                        var accessToken = await authenticationService.GetAccessTokenAsync();
                        
                        // Если токен истек, пытаемся обновить через refresh token (в фоне)
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            var isTokenValid = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                            if (!isTokenValid)
                            {
                                var refreshToken = await authenticationService.GetRefreshTokenAsync();
                                if (!string.IsNullOrWhiteSpace(refreshToken))
                                {
                                    Debug.WriteLine("[AppShell] Access token expired, attempting to refresh in background");
                                    // Обновляем в фоне, не ждем результата - не блокируем навигацию
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await authenticationService.RefreshTokenAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"[AppShell] Background token refresh failed: {ex.Message}");
                                        }
                                    });
                                }
                            }
                        }
                    }
                    
                    // Всегда проверяем PIN - даже если токен валиден, требуем PIN/биометрию
                    var hasValidPin = await authService.HasPinAsync();
                    Debug.WriteLine($"[AppShell] OnAppearing: hasValidPin={hasValidPin}");

                    if (!hasValidPin)
                    {
                        Debug.WriteLine("[AppShell] Decision: local user but NO valid PIN → navigating to PIN creation");
                        await Shell.Current.GoToAsync("///pinlogin?isCreatingPin=true", animate: false);
                    }
                    else
                    {
                        Debug.WriteLine("[AppShell] Decision: local user WITH valid PIN → navigating to PIN login (always require PIN/biometric)");
                        await Shell.Current.GoToAsync("///pinlogin", animate: false);
                    }
                    return;
                }

                // 2. Пользователя нет в локальной БД - проверяем, есть ли токены (пользователь есть на сервере)
                var hasRefreshToken = false;
                if (authenticationService != null)
                {
                    var refreshToken = await authenticationService.GetRefreshTokenAsync();
                    hasRefreshToken = !string.IsNullOrWhiteSpace(refreshToken);
                    Debug.WriteLine($"[AppShell] OnAppearing: HasRefreshToken={hasRefreshToken}");
                }

                if (hasRefreshToken)
                {
                    // Есть токены на сервере, но нет локального пользователя - выполняем автоматический вход
                    Debug.WriteLine("[AppShell] Decision: no local user but has refresh token → attempting auto-login");
                    var autoLoginSuccess = await authService.AutoLoginIfNoLocalUserAsync();
                    
                    if (autoLoginSuccess)
                    {
                        Debug.WriteLine("[AppShell] Auto-login successful, checking PIN");
                        var hasValidPin = await authService.HasPinAsync();
                        
                        if (!hasValidPin)
                        {
                            Debug.WriteLine("[AppShell] Decision: auto-login successful but NO valid PIN → navigating to PIN creation");
                            await Shell.Current.GoToAsync("///pinlogin?isCreatingPin=true", animate: false);
                        }
                        else
                        {
                            Debug.WriteLine("[AppShell] Decision: auto-login successful WITH valid PIN → navigating to PIN login");
                            await Shell.Current.GoToAsync("///pinlogin", animate: false);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[AppShell] Auto-login failed → navigating to login");
                        // Очищаем токены, если автоматический вход не удался
                        if (authenticationService != null)
                        {
                            await authenticationService.ClearTokensAsync();
                        }
                        await Shell.Current.GoToAsync("///login", animate: false);
                    }
                    return;
                }

                // 3. Нет ни локального пользователя, ни токенов - показываем экран логина
                Debug.WriteLine("[AppShell] Decision: no local user and no tokens → navigating to login");
                await Shell.Current.GoToAsync("///login", animate: false);
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
