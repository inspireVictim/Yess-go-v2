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
            if (this.FindByName<BottomNavBar>("BottomBar") is { } bottom)
                bottom.SelectedTab = BottomTab.More;
        }

        // Обработчик тапа по "Выйти"
        private async void OnLogoutTapped(object? sender, EventArgs e)
        {
            try
            {
                // 1) Чистим локальный аккаунт
                AccountStore.Instance.SignOut();

                // 2) Сбрасываем стек Shell и уходим на экран логина
                //    Предполагается, что в AppShell.xaml есть <ShellContent Route="login" .../>
                await Shell.Current.GoToAsync("///login");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выйти: {ex.Message}", "OK");
            }
        }
    }
}
