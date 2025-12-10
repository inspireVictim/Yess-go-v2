using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YessGoFront.Services;
using YessGoFront.Services.Domain;
using YessGoFront.Views.Controls;

namespace YessGoFront.Views
{
    public partial class MorePage : ContentPage
    {
        /*public MorePage()
        {
            InitializeComponent();
        }*/
        private UserProfileViewModel? _userProfileViewModel;

        public MorePage()
        {
            InitializeComponent();
            _userProfileViewModel = new UserProfileViewModel();
            BindingContext = _userProfileViewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // ✅ Используем новый метод из BottomNavBar
            if (this.FindByName<BottomNavBar>("BottomBar") is { } bottom)
                bottom.UpdateSelectedTab("More");

            // Загружаем данные пользователя из локальной БД
            if (_userProfileViewModel != null)
            {
                await _userProfileViewModel.LoadUserAsync();
            }
        }

        // ✅ Обработчик тапа по карточке профиля
        private async void OnProfileTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(Profile), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к Profile: {ex.Message}");
                try
                {
                    await Shell.Current.GoToAsync("Profile", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "История операции"
        private async void OnHistoryTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("///TransactionsPage", animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к TransactionsPage: {ex.Message}");
                try
                {
                    await Shell.Current.GoToAsync("//TransactionsPage", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "Обратная связь"
        private async void OnFeedbackTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///FeedbackPage");
        }

        // ✅ Обработчик тапа по "Сертификат"
        private async void OnCertificateTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///CertificatePage");
        }

        // ✅ Обработчик тапа по "Ввести промокод"
        private async void OnPromocodeTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///PromocodePage");
        }

        // ✅ Обработчик тапа по "Реферальная ссылка"
        private async void OnReferalTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///referal");
        }

        // ✅ Обработчик тапа по "Политика конфиденциальности"
        private async void OnPolicyTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(PolicyPage), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к PolicyPage: {ex.Message}");
                // Попытка использовать альтернативный маршрут
                try
                {
                    await Shell.Current.GoToAsync("PolicyPage", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "Условия использования"
        private async void OnConditionsTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(ConditionsPage), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к ConditionsPage: {ex.Message}");
                // Попытка использовать альтернативный маршрут
                try
                {
                    await Shell.Current.GoToAsync("ConditionsPage", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "Публичная оферта"
        private async void OnPublicOfferTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(PublicOfferPage), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к PublicOfferPage: {ex.Message}");
                try
                {
                    await Shell.Current.GoToAsync("PublicOfferPage", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "Политика возврата"
        private async void OnRefundPolicyTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(RefundPolicyPage), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к RefundPolicyPage: {ex.Message}");
                try
                {
                    await Shell.Current.GoToAsync("RefundPolicyPage", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "Безопасность платежей"
        private async void OnPaymentSecurityTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(PaymentSecurityPage), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к PaymentSecurityPage: {ex.Message}");
                try
                {
                    await Shell.Current.GoToAsync("PaymentSecurityPage", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "Условия доставки"
        private async void OnDeliveryTermsTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(DeliveryTermsPage), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к DeliveryTermsPage: {ex.Message}");
                try
                {
                    await Shell.Current.GoToAsync("DeliveryTermsPage", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "Контакты"
        private async void OnContactsTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(ContactsPage), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Ошибка навигации к ContactsPage: {ex.Message}");
                // Попытка использовать альтернативный маршрут
                try
                {
                    await Shell.Current.GoToAsync("ContactsPage", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
        }

        // ✅ Обработчик тапа по "Выйти"
        private async void OnLogoutTapped(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MorePage] Logout started");

                // 1) Вызываем LogoutAsync для очистки токенов через API, SecureStorage и PIN
                var authService = MauiProgram.Services.GetRequiredService<IAuthService>();
                await authService.LogoutAsync();
                System.Diagnostics.Debug.WriteLine("[MorePage] LogoutAsync completed - tokens and PIN cleared");

                // 2) Очистка локального аккаунта (AccountStore) - полностью удаляем все данные
                AccountStore.Instance.SignOut(keepProfile: false); // keepProfile=false удаляет все данные
                System.Diagnostics.Debug.WriteLine("[MorePage] AccountStore cleared");

                // 3) Дополнительная очистка PIN на всякий случай
                var pinService = MauiProgram.Services?.GetService<PinStorageService>();
                if (pinService != null)
                {
                    await pinService.ClearPinAsync();
                    System.Diagnostics.Debug.WriteLine("[MorePage] PIN cleared (additional cleanup)");
                }

                // 4) Навигация на экран логина (сброс стека)
                // Поля на LoginPage будут автоматически очищены при OnAppearing, так как пользователь не залогинен
                await Shell.Current.GoToAsync("///login", animate: true);
                System.Diagnostics.Debug.WriteLine("[MorePage] Navigated to login page");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MorePage] Error during logout: {ex.Message}");
                
                // Даже если произошла ошибка, всё равно очищаем локальные данные и переходим на логин
                try
                {
                    // Дополнительно очищаем PIN на случай если LogoutAsync не сработал
                    var pinService = MauiProgram.Services?.GetService<PinStorageService>();
                    if (pinService != null)
                    {
                        await pinService.ClearPinAsync();
                    }
                    
                    // Очищаем AccountStore
                    AccountStore.Instance.SignOut(keepProfile: false);
                    
                    // Навигация на логин
                    await Shell.Current.GoToAsync("///login", animate: true);
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Fallback logout error: {fallbackEx.Message}");
                    await DisplayAlert("Ошибка", $"Не удалось выйти: {ex.Message}", "OK");
                }
            }
        }

        // Простая ViewModel для профиля пользователя в MorePage
        private class UserProfileViewModel : INotifyPropertyChanged
        {
            private string _displayName = "Пользователь";

            public string DisplayName
            {
                get => _displayName;
                set
                {
                    if (_displayName != value)
                    {
                        _displayName = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public async Task LoadUserAsync()
            {
                try
                {
                    var authService = MauiProgram.Services?.GetService<IAuthService>();
                    if (authService == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[MorePage] IAuthService not found");
                        return;
                    }

                    var localUser = await authService.GetLocalUserAsync();
                    if (localUser != null)
                    {
                        // DisplayName всегда показывает ФИО из БД
                        var displayName = localUser.Name;
                        System.Diagnostics.Debug.WriteLine($"[MorePage] LoadUserAsync: Initial displayName from DB = '{displayName}'");
                        
                        // Проверяем, нужно ли загружать профиль из API:
                        // 1. Если Name пустое
                        // 2. Если Name равно "Пользователь" (дефолтное значение)
                        // 3. Если Name не содержит пробел (вероятно, это не полное имя)
                        var shouldLoadFromApi = string.IsNullOrWhiteSpace(displayName) ||
                                               displayName.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase) ||
                                               !displayName.Contains(' ');
                        
                        if (shouldLoadFromApi)
                        {
                            System.Diagnostics.Debug.WriteLine("[MorePage] Name is empty/invalid in DB, loading profile from API...");
                            try
                            {
                                // Используем таймаут для загрузки профиля (10 секунд)
                                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                                var userProfile = await authService.GetUserProfileAsync(cts.Token);
                                
                                if (userProfile != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MorePage] Got profile from API: FirstName='{userProfile.FirstName}', LastName='{userProfile.LastName}'");
                                    
                                    // Формируем ФИО из FirstName и LastName напрямую (не используем DisplayName, так как он может вернуть телефон)
                                    var firstName = userProfile.FirstName?.Trim() ?? string.Empty;
                                    var lastName = userProfile.LastName?.Trim() ?? string.Empty;
                                    var fullName = $"{firstName} {lastName}".Trim();
                                    
                                    if (!string.IsNullOrWhiteSpace(fullName))
                                    {
                                        displayName = fullName;
                                        System.Diagnostics.Debug.WriteLine($"[MorePage] ✅ Loaded Name from API: FirstName={firstName}, LastName={lastName}, FullName={fullName}");
                                        
                                        // Перезагружаем пользователя из БД, чтобы получить обновленное имя
                                        var updatedUser = await authService.GetLocalUserAsync();
                                        if (updatedUser != null && !string.IsNullOrWhiteSpace(updatedUser.Name) && 
                                            !updatedUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                                        {
                                            displayName = updatedUser.Name;
                                            System.Diagnostics.Debug.WriteLine($"[MorePage] ✅ Updated displayName from DB after profile load: {displayName}");
                                        }
                                    }
                                    else
                                    {
                                        // Если API не вернул имя, но в БД есть что-то валидное - используем его
                                        if (!string.IsNullOrWhiteSpace(localUser.Name) && 
                                            !localUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                                        {
                                            displayName = localUser.Name;
                                            System.Diagnostics.Debug.WriteLine($"[MorePage] Using Name from DB as fallback: {displayName}");
                                        }
                                        else
                                        {
                                            displayName = "Пользователь";
                                            System.Diagnostics.Debug.WriteLine("[MorePage] ❌ FirstName and LastName are empty in API response");
                                        }
                                    }
                                }
                                else
                                {
                                    // Если API вернул null, но в БД есть валидное имя - используем его
                                    if (!string.IsNullOrWhiteSpace(localUser.Name) && 
                                        !localUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                                    {
                                        displayName = localUser.Name;
                                        System.Diagnostics.Debug.WriteLine($"[MorePage] Using Name from DB as fallback (API returned null): {displayName}");
                                    }
                                    else
                                    {
                                        displayName = "Пользователь";
                                        System.Diagnostics.Debug.WriteLine("[MorePage] ❌ API returned null profile");
                                    }
                                }
                            }
                            catch (System.OperationCanceledException)
                            {
                                System.Diagnostics.Debug.WriteLine("[MorePage] GetUserProfileAsync timed out");
                                // Если таймаут, но в БД есть валидное имя - используем его
                                if (!string.IsNullOrWhiteSpace(localUser.Name) && 
                                    !localUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                                {
                                    displayName = localUser.Name;
                                    System.Diagnostics.Debug.WriteLine($"[MorePage] Using Name from DB as fallback (timeout): {displayName}");
                                }
                                else
                                {
                                    displayName = "Пользователь";
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MorePage] ❌ Failed to load profile from API: {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"[MorePage] Stack trace: {ex.StackTrace}");
                                
                                // Если API не загрузился, но в БД есть валидное имя - используем его
                                if (!string.IsNullOrWhiteSpace(localUser.Name) && 
                                    !localUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                                {
                                    displayName = localUser.Name;
                                    System.Diagnostics.Debug.WriteLine($"[MorePage] Using Name from DB as fallback (API error): {displayName}");
                                }
                                else
                                {
                                    displayName = "Пользователь";
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[MorePage] ✅ Using Name from DB: {displayName}");
                        }
                        
                        DisplayName = displayName;
                        System.Diagnostics.Debug.WriteLine($"[MorePage] Loaded user: DisplayName={DisplayName}, Phone={localUser.Phone}");
                    }
                    else
                    {
                        // Если нет локального пользователя, пытаемся получить данные из API
                        string displayName = "Пользователь";
                        
                        try
                        {
                            // Используем таймаут для загрузки профиля (10 секунд)
                            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                            var userProfile = await authService.GetUserProfileAsync(cts.Token);
                            
                            if (userProfile != null)
                            {
                                // Формируем ФИО из FirstName и LastName напрямую
                                var firstName = userProfile.FirstName?.Trim() ?? string.Empty;
                                var lastName = userProfile.LastName?.Trim() ?? string.Empty;
                                var fullName = $"{firstName} {lastName}".Trim();
                                
                                if (!string.IsNullOrWhiteSpace(fullName))
                                {
                                    displayName = fullName;
                                    System.Diagnostics.Debug.WriteLine($"[MorePage] ✅ Loaded Name from API (no local user): FirstName={firstName}, LastName={lastName}, FullName={fullName}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("[MorePage] ❌ FirstName and LastName are empty in API response (no local user)");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[MorePage] ❌ API returned null profile (no local user)");
                            }
                        }
                        catch (System.OperationCanceledException)
                        {
                            System.Diagnostics.Debug.WriteLine("[MorePage] GetUserProfileAsync timed out (no local user)");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MorePage] ❌ Failed to load profile from API (no local user): {ex.Message}");
                            // Оставляем "Пользователь"
                        }
                        
                        DisplayName = displayName;
                        System.Diagnostics.Debug.WriteLine($"[MorePage] No local user found: DisplayName={DisplayName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Error loading user: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[MorePage] Stack trace: {ex.StackTrace}");
                    DisplayName = "Пользователь";
                }
            }
        }
    }
}
