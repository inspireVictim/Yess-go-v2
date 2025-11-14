using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;
using YessGoFront.Services;

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
            // Сохраняем данные пользователя в AccountStore
            if (response.User != null)
            {
                var accountStore = AccountStore.Instance;
                accountStore.SignIn(
                    email: response.User.Email ?? string.Empty,
                    firstName: response.User.FirstName,
                    lastName: response.User.LastName,
                    remember: true,
                    phone: response.User.Phone
                );
            }

            // Проверяем, есть ли PIN-код
            var domainAuthService = MauiProgram.Services.GetRequiredService<Services.Domain.IAuthService>();
            var hasPin = await domainAuthService.HasPinAsync();

            // Если PIN-кода нет - переходим на страницу создания PIN
            if (!hasPin)
            {
                System.Diagnostics.Debug.WriteLine("[RegisterPage] No PIN found, navigating to PIN creation page");
                await Shell.Current.GoToAsync("///pinlogin?isCreatingPin=true", animate: true);
            }
            else
            {
                // Если PIN-код есть - переходим на главную страницу
                System.Diagnostics.Debug.WriteLine("[RegisterPage] PIN exists, navigating to main/home");
                await Shell.Current.GoToAsync("///main/home", animate: true);
            }
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
