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
            var phoneRaw = PhoneEntry.Text;

            // Базовые проверки
            if (string.IsNullOrWhiteSpace(first) ||
                string.IsNullOrWhiteSpace(last) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(phoneRaw))
            {
                await DisplayAlert("Ошибка", "Заполните все поля.", "OK");
                return;
            }

            // Нормализуем и валидируем телефон
            var phone = NormalizePhone(phoneRaw);
            if (!IsPhoneValid(phone))
            {
                PhoneError.Text = "Введите корректный номер телефона (например, +996555123456).";
                PhoneError.IsVisible = true;
                return;
            }
            PhoneError.IsVisible = false;

            // ДЕМО: регистрация прошла — сразу логиним (с телефоном)
            AccountStore.Instance.SignIn(email, first, last, remember: true, phone: phone);

            await Shell.Current.GoToAsync("///main");
        }

        // Оставляем только '+' и цифры
        private static string NormalizePhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim();
            s = s.StartsWith("+") ? s : "+" + s;

            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsDigit(ch) || ch == '+') sb.Append(ch);
            }
            return sb.ToString();
        }

        // Простая проверка формата E.164: + и 7..15 цифр всего
        private static bool IsPhoneValid(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            if (!phone.StartsWith("+")) return false;

            int digits = 0;
            for (int i = 0; i < phone.Length; i++)
            {
                if (char.IsDigit(phone[i])) digits++;
                else if (i != 0) return false; // допускаем только '+' на первом символе
            }
            return digits >= 7 && digits <= 15;
        }
    }
}
