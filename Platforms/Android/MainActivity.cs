using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using System.Diagnostics;

namespace YessGoFront;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
          ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
          LaunchMode = LaunchMode.SingleTop,
          Exported = true,
          WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Устанавливаем цвет status bar в фирменный зеленый
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
            Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#007A51"));
            
            // Делаем статус бар светлым (белый текст на темном фоне)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var window = Window;
                var decorView = window?.DecorView;
                if (decorView != null)
                {
                    var flags = (int)decorView.SystemUiVisibility;
                    flags &= ~(int)SystemUiFlags.LightStatusBar;
                    decorView.SystemUiVisibility = (StatusBarVisibility)flags;
                }
            }
        }

        // Настраиваем обработку системного жеста "назад" для предотвращения краша
        SetupBackButtonHandler();
    }

    private void SetupBackButtonHandler()
    {
        try
        {
            // Для Android API 33+ используем OnBackPressedDispatcher
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                OnBackPressedDispatcher.AddCallback(this, new BackPressedCallback(this));
                System.Diagnostics.Debug.WriteLine("[MainActivity] Back button handler set up using OnBackPressedDispatcher");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainActivity] Error setting up back button handler: {ex.Message}");
        }
    }

    // Для старых версий Android (API < 33)
    public override void OnBackPressed()
    {
        HandleBackPressed();
    }

    private void HandleBackPressed()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[MainActivity] HandleBackPressed called - handling via Shell navigation");
            
            // Получаем Shell для навигации
            var shell = Shell.Current;
            
            if (shell != null)
            {
                // Получаем текущий маршрут
                var currentState = shell.CurrentState;
                var currentRoute = currentState?.Location?.OriginalString ?? "unknown";
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Current route: {currentRoute}");
                
                // Для некоторых страниц (login, pinlogin) не пытаемся навигироваться назад
                if (currentRoute.Contains("login") || currentRoute.Contains("pinlogin"))
                {
                    System.Diagnostics.Debug.WriteLine("[MainActivity] On login/pin page, allowing default behavior");
                    // Позволяем стандартное поведение
                    if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                    {
                        base.OnBackPressed();
                    }
                    else
                    {
                        Finish();
                    }
                    return;
                }
                
                // Пробуем навигацию назад через Shell синхронно
                // Используем Task.Run для избежания блокировки UI потока
                var navigationTask = Task.Run(async () =>
                {
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine("[MainActivity] Attempting Shell navigation back");
                                await shell.GoToAsync("..");
                                System.Diagnostics.Debug.WriteLine("[MainActivity] Shell navigation back completed");
                            }
                            catch (Exception navEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MainActivity] Shell navigation error: {navEx.Message}");
                                // Если навигация не удалась, это нормально
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainActivity] Error in navigation task: {ex.Message}");
                    }
                });
                
                // Не ждем завершения навигации - предотвращаем завершение Activity
                // Shell обработает навигацию асинхронно
                System.Diagnostics.Debug.WriteLine("[MainActivity] Navigation initiated, preventing activity finish");
                return; // Не вызываем base.OnBackPressed() или Finish()
            }
            
            // Fallback: если Shell недоступен, используем стандартное поведение
            System.Diagnostics.Debug.WriteLine("[MainActivity] Shell not available, using default behavior");
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            {
                base.OnBackPressed();
            }
            else
            {
                Finish();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainActivity] Error in HandleBackPressed: {ex.Message}");
            // В случае ошибки используем стандартное поведение
            try
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                {
                    base.OnBackPressed();
                }
                else
                {
                    Finish();
                }
            }
            catch
            {
                // Игнорируем вторичные ошибки
            }
        }
    }

    // Callback для OnBackPressedDispatcher (Android API 33+)
    private class BackPressedCallback : AndroidX.Activity.OnBackPressedCallback
    {
        private readonly MainActivity _activity;

        public BackPressedCallback(MainActivity activity) : base(true)
        {
            _activity = activity;
        }

        public override void HandleOnBackPressed()
        {
            _activity.HandleBackPressed();
        }
    }
}
