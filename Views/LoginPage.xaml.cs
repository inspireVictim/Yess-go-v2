using System;
using Microsoft.Maui.Controls;
using YessGoFront.Services;

namespace YessGoFront.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OpenRegister_Tapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///register");
        }

        private void TogglePassword_Tapped(object? sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        }

        private async void Login_Clicked(object? sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim();
            var pass = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            {
                await DisplayAlert("Ошибка", "Введите E-mail и пароль.", "OK");
                return;
            }

            // ДЕМО: успешно логиним любого пользователя
            AccountStore.Instance.SignIn(email, firstName: null, lastName: null, remember: RememberCheck.IsChecked);

            // В проде: запрос к API + сохранение токена

            await Shell.Current.GoToAsync("///main");
        }
    }
}
