using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;
using YessGoFront.Services;

namespace YessGoFront.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginViewModel _viewModel;
        private readonly AuthNavigationHandler _authNavigationHandler;

        public LoginPage()
        {
            InitializeComponent();

            // Получаем сервисы через DI
            var authService = MauiProgram.Services.GetRequiredService<IAuthService>();
            var logger = MauiProgram.Services.GetService<ILogger<LoginViewModel>>();
            _authNavigationHandler = MauiProgram.Services.GetRequiredService<AuthNavigationHandler>();

            _viewModel = new LoginViewModel(authService, logger);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // чтобы не было двойных подписок
            _viewModel.OnLoginSuccess -= OnLoginSuccess;
            _viewModel.OnLoginSuccess += OnLoginSuccess;

            System.Diagnostics.Debug.WriteLine("[LoginPage] OnAppearing - subscribed to OnLoginSuccess");

            // Проверяем, залогинен ли пользователь
            try
            {
                var authService = MauiProgram.Services.GetService<IAuthService>();
                if (authService != null)
                {
                    var isAuthenticated = await authService.IsAuthenticatedAsync();
                    System.Diagnostics.Debug.WriteLine($"[LoginPage] OnAppearing: IsAuthenticated={isAuthenticated}");

                    // Если пользователь не залогинен - очищаем поля
                    if (!isAuthenticated)
                    {
                        System.Diagnostics.Debug.WriteLine("[LoginPage] OnAppearing: User not authenticated, clearing fields");
                        _viewModel.ClearFields();
                    }
                }
                else
                {
                    // Если сервис недоступен - очищаем поля для безопасности
                    _viewModel.ClearFields();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginPage] OnAppearing: Error checking authentication: {ex.Message}");
                // В случае ошибки всё равно очищаем поля для безопасности
                _viewModel.ClearFields();
            }
        }

        private async Task OnLoginSuccess(Services.Api.AuthResponse response)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] Login success! UserId: {response?.UserId}");
            
            // Передаем обработку в AuthNavigationHandler
            await _authNavigationHandler.HandleSuccessfulAuthAsync(response, _viewModel.RememberMe);
        }

        private async void OpenRegister_Tapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///register");
        }

        private void TogglePassword_Tapped(object? sender, EventArgs e)
        {
            if (PasswordEntry != null)
            {
                PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.OnLoginSuccess -= OnLoginSuccess;
            System.Diagnostics.Debug.WriteLine("[LoginPage] OnDisappearing - unsubscribed from OnLoginSuccess");
        }

        public async void OnRegistrationPage(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///register");
        }
    }
}
