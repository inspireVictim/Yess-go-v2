using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
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
        private bool _isNavigating = false; // Защита от повторных вызовов навигации

        public LoginPage()
        {
            InitializeComponent();

            // Получаем сервисы через DI
            var authService = MauiProgram.Services.GetRequiredService<IAuthService>();
            var logger = MauiProgram.Services.GetService<ILogger<LoginViewModel>>();

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
            // Защита от повторных вызовов навигации
            if (_isNavigating)
            {
                System.Diagnostics.Debug.WriteLine("[LoginPage] Navigation already in progress, ignoring duplicate call");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Login success! UserId: {response.UserId}");

                // Проверяем, что токен реально сохранён
                var authService = MauiProgram.Services
                    .GetRequiredService<YessGoFront.Infrastructure.Auth.IAuthenticationService>();

                var savedToken = await authService.GetAccessTokenAsync();
                System.Diagnostics.Debug.WriteLine($"[LoginPage] Token saved: {!string.IsNullOrEmpty(savedToken)}");

                // Обновляем AccountStore (учитываем rememberMe!)
                var user = response.User;
                var email = user?.Email ?? user?.Phone ?? string.Empty;
                var firstName = user?.FirstName ?? string.Empty;
                var lastName = user?.LastName ?? string.Empty;
                var phone = user?.Phone ?? string.Empty;
                var rememberMe = _viewModel.RememberMe;

                AccountStore.Instance.SignIn(
                    email,
                    firstName,
                    lastName,
                    rememberMe,
                    phone
                );

                var isSignedIn = AccountStore.Instance.IsSignedIn;
                System.Diagnostics.Debug.WriteLine($"[LoginPage] AccountStore updated. IsSignedIn: {isSignedIn}, Email: {AccountStore.Instance.Email}, RememberMe: {rememberMe}");

                if (!isSignedIn)
                {
                    System.Diagnostics.Debug.WriteLine("[LoginPage] WARNING: IsSignedIn is false after SignIn! This should not happen.");
                }

                // Проверяем, есть ли валидный PIN с таймаутом
                var domainAuthService = MauiProgram.Services.GetRequiredService<IAuthService>();
                bool hasPin = false;
                try
                {
                    // Используем таймаут для проверки PIN (10 секунд)
                    // HasPinAsync не принимает CancellationToken, поэтому используем Task.Run с таймаутом
                    using var pinCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    hasPin = await Task.Run(async () => await domainAuthService.HasPinAsync(), pinCts.Token);
                    System.Diagnostics.Debug.WriteLine($"[LoginPage] OnLoginSuccess: hasValidPin={hasPin}");
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("[LoginPage] HasPinAsync timed out, assuming no PIN");
                    hasPin = false; // При таймауте предполагаем, что PIN нет
                }
                catch (Exception pinEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoginPage] Error checking PIN: {pinEx.Message}");
                    hasPin = false; // При ошибке предполагаем, что PIN нет
                }

                // Навигацию делаем на главном потоке
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (_isNavigating)
                    {
                        System.Diagnostics.Debug.WriteLine("[LoginPage] Navigation already in progress, skipping");
                        return;
                    }

                    _isNavigating = true;
                    try
                    {
                        var shell = Shell.Current;
                        if (shell != null)
                        {
                            if (!hasPin)
                            {
                                // Если валидного PIN-кода нет - перейти на создание PIN
                                System.Diagnostics.Debug.WriteLine("[LoginPage] No valid PIN found, navigating to PIN creation page");
                                await shell.GoToAsync("///pinlogin?isCreatingPin=true", animate: true);
                            }
                            else
                            {
                                // Если есть валидный PIN - сразу на экран ввода PIN
                                System.Diagnostics.Debug.WriteLine("[LoginPage] Valid PIN exists, navigating to PIN login page");
                                await shell.GoToAsync("///pinlogin", animate: true);
                            }
                        }
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoginPage] Navigation error: {navEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[LoginPage] Stack trace: {navEx.StackTrace}");
                    }
                    finally
                    {
                        _isNavigating = false;
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

        public async void OnRegistrationPage(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///register");
        }
    }
}
