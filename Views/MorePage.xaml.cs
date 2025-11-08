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

        // ✅ Обработчик тапа по "Выйти"
        private async void OnLogoutTapped(object? sender, EventArgs e)
        {
            try
            {
                // 1) Вызываем LogoutAsync для очистки токенов через API и SecureStorage
                var authService = MauiProgram.Services.GetRequiredService<IAuthService>();
                await authService.LogoutAsync();

                // 2) Очистка локального аккаунта (AccountStore)
                AccountStore.Instance.SignOut();

                // 3) Навигация на экран логина (сброс стека)
                await Shell.Current.GoToAsync("///login");
            }
            catch (Exception ex)
            {
                // Даже если произошла ошибка, всё равно очищаем локальные данные и переходим на логин
                try
                {
                    AccountStore.Instance.SignOut();
                    await Shell.Current.GoToAsync("///login");
                }
                catch
                {
                    await DisplayAlert("Ошибка", $"Не удалось выйти: {ex.Message}", "OK");
                }
            }
        }
    }
}
