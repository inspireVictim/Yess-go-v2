using System;
using Microsoft.Maui.Controls;
using YessGoFront.Services;
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
                // 1) Очистка локального аккаунта
                AccountStore.Instance.SignOut();

                // 2) Навигация на экран логина (сброс стека)
                await Shell.Current.GoToAsync("///login");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выйти: {ex.Message}", "OK");
            }
        }
    }
}
