using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;

namespace YessGoFront.Views
{
    public partial class RegisterPage : ContentPage
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterPage()
        {
            InitializeComponent();

            // Получаем сервисы через DI
            var authService = MauiProgram.Services.GetRequiredService<IAuthService>();
            var logger = MauiProgram.Services.GetService<Microsoft.Extensions.Logging.ILogger<RegisterViewModel>>();

            _viewModel = new RegisterViewModel(authService, logger);
            BindingContext = _viewModel;

            // Подписываемся на событие успешной регистрации
            _viewModel.OnRegisterSuccess += OnRegisterSuccess;
        }

        private async Task OnRegisterSuccess(Services.Api.AuthResponse response)
        {
            // Успешная регистрация - переходим на главную страницу
            await Shell.Current.GoToAsync("///main");
        }

        private async void OpenLogin_Tapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///login");
        }

        private void TogglePassword_Tapped(object? sender, EventArgs e)
        {
            if (PasswordEntry != null)
            {
                PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            }
        }

        private void ToggleConfirmPassword_Tapped(object? sender, EventArgs e)
        {
            if (ConfirmPasswordEntry != null)
            {
                ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.OnRegisterSuccess -= OnRegisterSuccess;
        }
    }
}
