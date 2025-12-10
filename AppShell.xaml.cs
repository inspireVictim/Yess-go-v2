using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using YessGoFront.Services.Domain;
using YessGoFront.Views;
using YessGoFront.Services;

namespace YessGoFront
{
    public partial class AppShell : Shell
    {
        private bool _initialized;
        
        // Cache fields for background initialization
        private User? _cachedLocalUser;
        private bool _cachedHasPin;
        private DateTime _cacheTimestamp = DateTime.MinValue;

        public AppShell()
        {
            InitializeComponent();

            // Регистрация внутренних маршрутов (только для подстраниц, которые НЕ объявлены как ShellContent в XAML)
            // Страницы, объявленные как ShellContent в XAML, регистрировать НЕ нужно (это создает дубликаты)
            
            // Основные страницы (уже в ShellContent в XAML) - НЕ регистрируем здесь:
            // - WalletPage (в TabBar)
            // - PartnerPage (в TabBar)
            // - partnerdetails/PartnerDetailPage (ShellContent)
            // - TransactionsPage (ShellContent)
            // - FeedbackPage (ShellContent)
            // - CertificatePage (ShellContent)
            
            // Подстраницы, которые работают через стек навигации:
            Routing.RegisterRoute(nameof(Views.PartnersListPage), typeof(Views.PartnersListPage));
            Routing.RegisterRoute("PartnerDetailViewPage", typeof(Views.PartnerDetailViewPage));
            Routing.RegisterRoute(nameof(Views.BasketPage), typeof(Views.BasketPage));
            Routing.RegisterRoute(nameof(Views.Acquiring), typeof(Views.Acquiring));
            Routing.RegisterRoute(nameof(Views.Profile), typeof(Views.Profile));
            Routing.RegisterRoute(nameof(Views.PayPage), typeof(Views.PayPage));
            Routing.RegisterRoute(nameof(Views.SearchPartnersPay), typeof(Views.SearchPartnersPay));
            
            // Другие подстраницы:
            Routing.RegisterRoute(nameof(Views.TransactionDetailsPage), typeof(Views.TransactionDetailsPage));
            Routing.RegisterRoute(nameof(Views.OperationHistory), typeof(Views.OperationHistory));
            Routing.RegisterRoute(nameof(Views.PolicyPage), typeof(Views.PolicyPage));
            Routing.RegisterRoute(nameof(Views.ConditionsPage), typeof(Views.ConditionsPage));
            Routing.RegisterRoute(nameof(Views.ContactsPage), typeof(Views.ContactsPage));
            Routing.RegisterRoute(nameof(Views.PublicOfferPage), typeof(Views.PublicOfferPage));
            Routing.RegisterRoute(nameof(Views.RefundPolicyPage), typeof(Views.RefundPolicyPage));
            Routing.RegisterRoute(nameof(Views.PaymentSecurityPage), typeof(Views.PaymentSecurityPage));
            Routing.RegisterRoute(nameof(Views.DeliveryTermsPage), typeof(Views.DeliveryTermsPage));
            Routing.RegisterRoute("payment", typeof(Views.PaymentPage));
            Routing.RegisterRoute("receipt", typeof(Views.ReceiptPage));
            Routing.RegisterRoute(nameof(Views.FinikPaymentPage), typeof(Views.FinikPaymentPage));


        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);
            
            // Перехватываем системный жест "назад" для корректной обработки
            // Это предотвращает краш приложения при свайпе от левого края
            try
            {
                Debug.WriteLine($"[AppShell] Navigating: Target={args.Target?.Location}, Current={args.Current?.Location}, Source={args.Source}");
                
                // Для всех типов навигации разрешаем переход
                // Shell сам управляет навигационным стеком и предотвращает ошибки
                // Важно: не блокируем навигацию, чтобы избежать краша
                Debug.WriteLine("[AppShell] Navigation allowed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell] Error in OnNavigating: {ex.Message}");
                // Не блокируем навигацию при ошибке - это может привести к крашу
            }
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
                    // Проверяем состояние токенов для оптимизации UX
                    bool tokensAreFresh = false;
                    if (authenticationService != null)
                    {
                        var accessToken = await authenticationService.GetAccessTokenAsync();
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            var isTokenValid = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                            var remainingMinutes = Infrastructure.Auth.JwtHelper.GetTokenRemainingMinutes(accessToken);

                            if (isTokenValid && remainingMinutes > 15)
                            {
                                // Токены свежие (более 15 минут) - можем оптимизировать UX
                                tokensAreFresh = true;
                                Debug.WriteLine($"[AppShell] Tokens are fresh: {remainingMinutes} minutes remaining");
                            }
                            else if (!isTokenValid)
                            {
                                // Токены истекли - пытаемся обновить в фоне
                                var refreshToken = await authenticationService.GetRefreshTokenAsync();
                                if (!string.IsNullOrWhiteSpace(refreshToken))
                                {
                                    Debug.WriteLine("[AppShell] Access token expired, attempting to refresh in background");
                                    // Обновляем в фоне, не ждем результата
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

                    // Проверяем наличие PIN
                    var hasValidPin = await authService.HasPinAsync();
                    Debug.WriteLine($"[AppShell] OnAppearing: hasValidPin={hasValidPin}, tokensAreFresh={tokensAreFresh}");

                    if (!hasValidPin)
                    {
                        Debug.WriteLine("[AppShell] Decision: local user but NO valid PIN → navigating to PIN creation");
                        await Shell.Current.GoToAsync("///pinlogin?isCreatingPin=true", animate: false);
                    }
                    else
                    {
                        // Отправляем на PIN экран с информацией о состоянии токенов
                        var route = tokensAreFresh ? "///pinlogin?tokenStatus=fresh" : "///pinlogin";
                        Debug.WriteLine($"[AppShell] Decision: local user WITH valid PIN → navigating to PIN login (tokenStatus: {(tokensAreFresh ? "fresh" : "normal")})");
                        await Shell.Current.GoToAsync(route, animate: false);
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

        private async Task PerformBackgroundInitializationAsync(IAuthService authService)
        {
            Debug.WriteLine("[AppShell] Background: starting full auth check and DB initialization");

            try
            {
                // 1. Инициализируем БД (создаем схему если нужно) и затем seeding в фоне
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = MauiProgram.Services.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                        var logger = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILogger<Data.DatabaseInitializer>>();
                        var initializer = new Data.DatabaseInitializer(dbContext, logger);
                        
                        Debug.WriteLine("[AppShell] Background: Initializing database schema...");
                        await initializer.InitializeAsync();
                        Debug.WriteLine("[AppShell] Background: Database schema initialized successfully");
                        
                        Debug.WriteLine("[AppShell] Background: Starting database seeding...");
                        await initializer.SeedAsync();
                        Debug.WriteLine("[AppShell] Background: Database seeded successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[AppShell] Background: Database initialization/seeding failed: {ex.Message}");
                        Debug.WriteLine($"[AppShell] Background: StackTrace: {ex.StackTrace}");
                    }
                });

                // 2. Полная проверка auth (обновляем кэш)
                var authenticationService = MauiProgram.Services.GetService<YessGoFront.Infrastructure.Auth.IAuthenticationService>();
                
                // Обновляем кэш localUser с таймаутом
                User? localUser = null;
                try
                {
                    using var localUserCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                    localUser = await authService.GetLocalUserAsync();
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[AppShell] Background: GetLocalUserAsync timed out");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AppShell] Background: Error getting local user: {ex.Message}");
                }
                
                _cachedLocalUser = localUser;
                
                if (localUser != null)
                {
                    Debug.WriteLine($"[AppShell] Background: LocalUser found (UserId={localUser.Id})");
                    
                    // Обновляем кэш PIN с таймаутом
                    bool hasValidPin = false;
                    try
                    {
                        using var hasPinCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                        // HasPinAsync не принимает CancellationToken, поэтому используем Task.Run с таймаутом
                        hasValidPin = await Task.Run(async () => await authService.HasPinAsync(), hasPinCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("[AppShell] Background: HasPinAsync timed out");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[AppShell] Background: Error checking PIN: {ex.Message}");
                    }
                    
                    _cachedHasPin = hasValidPin;
                    _cacheTimestamp = DateTime.UtcNow;
                    
                    // Проверяем и обновляем токены в фоне
                    if (authenticationService != null)
                    {
                        string? accessToken = null;
                        try
                        {
                            using var accessTokenCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                            // GetAccessTokenAsync не принимает CancellationToken, поэтому используем Task.Run с таймаутом
                            accessToken = await Task.Run(async () => await authenticationService.GetAccessTokenAsync(), accessTokenCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("[AppShell] Background: GetAccessTokenAsync timed out");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[AppShell] Background: Error getting access token: {ex.Message}");
                        }
                        
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            var isTokenValid = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                            
                            if (!isTokenValid)
                            {
                                Debug.WriteLine("[AppShell] Background: Token expired, refreshing...");
                                string? refreshToken = null;
                                try
                                {
                                    using var refreshTokenCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                                    // GetRefreshTokenAsync не принимает CancellationToken, поэтому используем Task.Run с таймаутом
                                    refreshToken = await Task.Run(async () => await authenticationService.GetRefreshTokenAsync(), refreshTokenCts.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    Debug.WriteLine("[AppShell] Background: GetRefreshTokenAsync timed out");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[AppShell] Background: Error getting refresh token: {ex.Message}");
                                }
                                
                                if (!string.IsNullOrWhiteSpace(refreshToken))
                                {
                                    try
                                    {
                                        // Используем таймаут для обновления токенов (10 секунд)
                                        using var tokenCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                                        
                                        // Используем GlobalAuthService для централизованного управления токенами
                                        // Создаем scope для получения GlobalAuthService
                                        using var authScope = MauiProgram.Services.CreateScope();
                                        var globalAuthService = authScope.ServiceProvider.GetService<YessGoFront.Services.GlobalAuthService>();
                                        if (globalAuthService != null)
                                        {
                                            var tokensValid = await globalAuthService.EnsureValidTokensAsync(tokenCts.Token);
                                            if (tokensValid)
                                            {
                                                Debug.WriteLine("[AppShell] Background: Tokens refreshed successfully via GlobalAuthService");
                                            }
                                            else
                                            {
                                                Debug.WriteLine("[AppShell] Background: Failed to refresh tokens via GlobalAuthService");
                                            }
                                        }
                                        else
                                        {
                                            // Fallback на старый метод (IAuthenticationService.RefreshTokenAsync не принимает CancellationToken)
                                            await authenticationService.RefreshTokenAsync();
                                            Debug.WriteLine("[AppShell] Background: Token refreshed successfully (fallback method)");
                                        }
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        Debug.WriteLine("[AppShell] Background: Token refresh timed out");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[AppShell] Background: Token refresh failed: {ex.Message}");
                                        Debug.WriteLine($"[AppShell] Background: StackTrace: {ex.StackTrace}");
                                    }
                                }
                            }
                        }
                    }
                    
                    // Если PIN отсутствует, но пользователь есть - перенаправляем на создание PIN
                    if (!hasValidPin)
                    {
                        Debug.WriteLine("[AppShell] Background: No PIN found, should navigate to PIN creation");
                        // Навигация уже выполнена в быстром пути, но можно обновить если нужно
                    }
                }
                else
                {
                    // Пользователя нет в локальной БД - проверяем токены
                    var hasRefreshToken = false;
                    if (authenticationService != null)
                    {
                        string? refreshToken = null;
                        try
                        {
                            using var refreshTokenCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                            // GetRefreshTokenAsync не принимает CancellationToken, поэтому используем Task.Run с таймаутом
                            refreshToken = await Task.Run(async () => await authenticationService.GetRefreshTokenAsync(), refreshTokenCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("[AppShell] Background: GetRefreshTokenAsync (check) timed out");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[AppShell] Background: Error getting refresh token (check): {ex.Message}");
                        }
                        
                        hasRefreshToken = !string.IsNullOrWhiteSpace(refreshToken);
                    }

                    if (hasRefreshToken)
                    {
                        Debug.WriteLine("[AppShell] Background: No local user but has refresh token → attempting auto-login");
                        try
                        {
                            // Используем таймаут для auto-login (15 секунд)
                            using var autoLoginCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
                            var autoLoginSuccess = await authService.AutoLoginIfNoLocalUserAsync(autoLoginCts.Token);
                            
                            if (autoLoginSuccess)
                            {
                                bool hasValidPin = false;
                                try
                                {
                                    using var hasPinCts2 = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                                    // HasPinAsync не принимает CancellationToken, поэтому используем Task.Run с таймаутом
                                    hasValidPin = await Task.Run(async () => await authService.HasPinAsync(), hasPinCts2.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    Debug.WriteLine("[AppShell] Background: HasPinAsync (after auto-login) timed out");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[AppShell] Background: Error checking PIN after auto-login: {ex.Message}");
                                }
                                
                                _cachedHasPin = hasValidPin;
                                _cacheTimestamp = DateTime.UtcNow;
                                
                                Debug.WriteLine($"[AppShell] Background: Auto-login successful, hasPin={hasValidPin}");
                            }
                            else
                            {
                                Debug.WriteLine("[AppShell] Background: Auto-login failed");
                                if (authenticationService != null)
                                {
                                    await authenticationService.ClearTokensAsync();
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("[AppShell] Background: Auto-login timed out");
                            if (authenticationService != null)
                            {
                                await authenticationService.ClearTokensAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[AppShell] Background: Auto-login error: {ex.Message}");
                            Debug.WriteLine($"[AppShell] Background: StackTrace: {ex.StackTrace}");
                            if (authenticationService != null)
                            {
                                await authenticationService.ClearTokensAsync();
                            }
                        }
                    }
                }

                // 3. Запускаем BalanceRefreshService после инициализации
                try
                {
                    var balanceRefreshService = MauiProgram.Services.GetService<YessGoFront.Services.BalanceRefreshService>();
                    if (balanceRefreshService != null)
                    {
                        balanceRefreshService.Start(TimeSpan.FromSeconds(30));
                        Debug.WriteLine("[AppShell] Background: Balance refresh service started");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AppShell] Background: Failed to start balance refresh service: {ex.Message}");
                }

                Debug.WriteLine("[AppShell] Background: Full initialization completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell] Background: Error during background initialization: {ex.Message}");
                Debug.WriteLine($"[AppShell] Background: StackTrace: {ex.StackTrace}");
            }
        }
    }
}
