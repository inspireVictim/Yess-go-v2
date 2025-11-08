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
                
                // Успешный логин - выполняем навигацию напрямую
                System.Diagnostics.Debug.WriteLine("[LoginPage] Navigating to main/home...");
                
                // Выполняем навигацию на главном потоке
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        var shell = Shell.Current;
                        if (shell != null && shell is AppShell appShell)
                        {
                            // Пробуем установить CurrentItem напрямую для более надежной навигации
                            try
                            {
                                var tabBar = shell.Items.FirstOrDefault(x => x.Route == "main") as TabBar;
                                if (tabBar != null)
                                {
                                    var homeTab = tabBar.Items.FirstOrDefault(x => x.Route == "home");
                                    if (homeTab != null)
                                    {
                                        System.Diagnostics.Debug.WriteLine("[LoginPage] Setting CurrentItem directly");
                                        shell.CurrentItem = tabBar;
                                        tabBar.CurrentItem = homeTab;
                                        
                                        await Task.Delay(200);
                                        
                                        // Принудительно обновляем UI
                                        shell.ForceLayout();
                                        await Task.Delay(100);
                                        
                                        var currentRoute = shell.CurrentState?.Location?.ToString() ?? "unknown";
                                        System.Diagnostics.Debug.WriteLine($"[LoginPage] CurrentItem set. Route: {currentRoute}");
                                        
                                        // Если это сработало, выходим
                                        if (currentRoute.Contains("main/home"))
                                        {
                                            System.Diagnostics.Debug.WriteLine("[LoginPage] Navigation successful via CurrentItem");
                                            return;
                                        }
                                    }
                                }
                            }
                            catch (Exception itemEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[LoginPage] CurrentItem approach failed: {itemEx.Message}");
                            }
                            
                            // Fallback: используем GoToAsync
                            System.Diagnostics.Debug.WriteLine("[LoginPage] Using GoToAsync as fallback");
                            await shell.GoToAsync("///main/home", animate: false);
                            await Task.Delay(300);
                            
                            // Принудительно обновляем UI
                            shell.ForceLayout();
                            await Task.Delay(100);
                            
                            // Проверяем результат
                            var route = shell.CurrentState?.Location?.ToString() ?? "unknown";
                            System.Diagnostics.Debug.WriteLine($"[LoginPage] Navigation via GoToAsync completed. Route: {route}");
                            
                            // Если всё ещё не работает, перезагружаем Shell
                            if (!route.Contains("main/home"))
                            {
                                System.Diagnostics.Debug.WriteLine("[LoginPage] All navigation methods failed, reloading Shell");
                                Application.Current.MainPage = new AppShell();
                                await Task.Delay(600);
                                
                                var newShell = Shell.Current;
                                if (newShell != null)
                                {
                                    await newShell.GoToAsync("///main/home", animate: false);
                                    await Task.Delay(200);
                                    System.Diagnostics.Debug.WriteLine($"[LoginPage] Shell reloaded. Route: {newShell.CurrentState?.Location}");
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[LoginPage] Shell.Current is null, creating new Shell");
                            Application.Current.MainPage = new AppShell();
                            await Task.Delay(500);
                            
                            var newShell = Shell.Current;
                            if (newShell != null)
                            {
                                await newShell.GoToAsync("///main/home", animate: false);
                            }
                        }
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoginPage] Navigation error: {navEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[LoginPage] Stack trace: {navEx.StackTrace}");
                        
                        // Последняя попытка - перезагрузить Shell
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[LoginPage] Attempting Shell reload as fallback");
                            Application.Current.MainPage = new AppShell();
                        }
                        catch (Exception shellEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoginPage] Shell reload failed: {shellEx.Message}");
                        }
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
