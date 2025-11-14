using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using YessGoFront.Services;
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
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Подписываемся на событие успешного логина при каждом появлении страницы
            // Сначала отписываемся, чтобы избежать двойной подписки
            _viewModel.OnLoginSuccess -= OnLoginSuccess;
            _viewModel.OnLoginSuccess += OnLoginSuccess;
            System.Diagnostics.Debug.WriteLine("[LoginPage] OnAppearing - subscribed to OnLoginSuccess");
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
                
                // Обновляем AccountStore для отслеживания состояния входа
                var user = response.User;
                var email = user?.Email ?? user?.Phone ?? string.Empty;
                var firstName = user?.FirstName ?? string.Empty;
                var lastName = user?.LastName ?? string.Empty;
                var phone = user?.Phone ?? string.Empty;
                var rememberMe = _viewModel.RememberMe;
                
                AccountStore.Instance.SignIn(email, firstName, lastName, rememberMe, phone);
                
                // Проверяем, что AccountStore действительно обновлён
                var isSignedIn = AccountStore.Instance.IsSignedIn;
                System.Diagnostics.Debug.WriteLine($"[LoginPage] AccountStore updated. IsSignedIn: {isSignedIn}, Email: {AccountStore.Instance.Email}, RememberMe: {rememberMe}");
                
                if (!isSignedIn)
                {
                    System.Diagnostics.Debug.WriteLine("[LoginPage] WARNING: IsSignedIn is false after SignIn! This should not happen.");
                }
                
                // Проверяем, есть ли PIN-код
                var domainAuthService = MauiProgram.Services.GetRequiredService<Services.Domain.IAuthService>();
                var hasPin = await domainAuthService.HasPinAsync();
                
                // Выполняем навигацию на главном потоке
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        var shell = Shell.Current;
                        if (shell != null)
                        {
                            if (!hasPin)
                            {
                                // Если PIN-кода нет - переходим на страницу создания PIN
                                System.Diagnostics.Debug.WriteLine("[LoginPage] No PIN found, navigating to PIN creation page");
                                await shell.GoToAsync("///pinlogin?isCreatingPin=true", animate: true);
                            }
                            else
                            {
                                // Если PIN-код есть - переходим на главную страницу
                                System.Diagnostics.Debug.WriteLine("[LoginPage] PIN exists, navigating to main/home...");
                                await shell.GoToAsync("///main/home", animate: true);
                            }
                        }
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoginPage] Navigation error: {navEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[LoginPage] Stack trace: {navEx.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Error in OnLoginSuccess: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Stack trace: {ex.StackTrace}");
            }
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
    }
}
