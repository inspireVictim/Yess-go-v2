using System;
using Microsoft.Maui.Controls;
using YessGoFront.Services;

namespace YessGoFront.Views
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OpenLogin_Tapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///login");
        }

        private async void Register_Clicked(object? sender, EventArgs e)
        {
            var first = FirstNameEntry.Text?.Trim();
            var last = LastNameEntry.Text?.Trim();
            var email = EmailEntry.Text?.Trim();
            var pass = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(first) ||
                string.IsNullOrWhiteSpace(last) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(pass))
            {
                await DisplayAlert("Ошибка", "Заполните все поля.", "OK");
                return;
            }

            // ДЕМО: считаем регистрацию успешной и сразу логиним
            AccountStore.Instance.SignIn(email, first, last, remember: true);

            await Shell.Current.GoToAsync("///main");
        }
    }
}
