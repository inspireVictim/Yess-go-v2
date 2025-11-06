using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;

namespace YessGoFront.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginViewModel _viewModel;

        public LoginPage()
        {
            InitializeComponent();

            // Получаем сервисы через DI
            var authService = MauiProgram.Services.GetRequiredService<IAuthService>();
            var logger = MauiProgram.Services.GetService<Microsoft.Extensions.Logging.ILogger<LoginViewModel>>();

            _viewModel = new LoginViewModel(authService, logger);
            BindingContext = _viewModel;

            // Подписываемся на событие успешного логина
            _viewModel.OnLoginSuccess += OnLoginSuccess;
        }

        private async Task OnLoginSuccess(Services.Api.AuthResponse response)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Login success! UserId: {response.UserId}");
                
                // Проверяем, что токен действительно сохранён
                var authService = MauiProgram.Services.GetRequiredService<Infrastructure.Auth.IAuthenticationService>();
                var savedToken = await authService.GetAccessTokenAsync();
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Token saved: {!string.IsNullOrEmpty(savedToken)}");
                
                // Успешный логин - переходим на главную страницу
                System.Diagnostics.Debug.WriteLine("[LoginPage] Navigating to main...");
                
                // Используем абсолютный путь для навигации
                await Shell.Current.GoToAsync("//main/home", animate: true);
                
                System.Diagnostics.Debug.WriteLine("[LoginPage] Navigation completed!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Navigation error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Stack trace: {ex.StackTrace}");
                
                // Попробуем альтернативный способ - перезагрузить Shell
                try
                {
                    Application.Current.MainPage = new AppShell();
                    await Shell.Current.GoToAsync("//main/home");
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoginPage] Alternative navigation also failed: {ex2.Message}");
                }
            }
        }

        private async void OpenRegister_Tapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///register");
        }

        private void TogglePassword_Tapped(object? sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.OnLoginSuccess -= OnLoginSuccess;
        }
    }
}
