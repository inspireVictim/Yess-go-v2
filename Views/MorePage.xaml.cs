using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YessGoFront.Services;
using YessGoFront.Services.Domain;
using YessGoFront.Views.Controls;

namespace YessGoFront.Views
{
    public partial class MorePage : ContentPage
    {
        public MorePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ✅ Используем новый метод из BottomNavBar
            if (this.FindByName<BottomNavBar>("BottomBar") is { } bottom)
                bottom.UpdateSelectedTab("More");
        }

        // ✅ Обработчик тапа по "История операции"
        private async void OnHistoryTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(TransactionsPage));
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
    }
}
