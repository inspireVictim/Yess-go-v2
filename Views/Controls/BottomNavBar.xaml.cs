using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YessGoFront.Views.Controls
{
    public partial class BottomNavBar : ContentView
    {
        private string _selectedTab = "Home";
        private readonly SemaphoreSlim _navigationLock = new(1, 1); // Защита от повторных нажатий

        public BottomNavBar()
        {
            InitializeComponent();
        }

        // ✅ Вызывается из страниц (MainPage, MorePage и т.д.)
        public void UpdateSelectedTab(string tab)
        {
            _selectedTab = tab;

            // Сбрасываем все иконки и подписи в неактивное состояние
            HomeIcon.Source = "nav_home.png";
            PartnerIcon.Source = "nav_partners.png";
            BellIcon.Source = "nav_notification.png";
            MoreIcon.Source = "nav_more.png";

            HomeText.TextColor = Color.FromArgb("#9E9E9E");
            PartnerText.TextColor = Color.FromArgb("#9E9E9E");
            BellText.TextColor = Color.FromArgb("#9E9E9E");
            MoreText.TextColor = Color.FromArgb("#9E9E9E");

            // Активная вкладка
            switch (tab)
            {
                case "Home":
                    HomeIcon.Source = "nav_home_press.png";
                    HomeText.TextColor = Color.FromArgb("#146B4D");
                    break;

                case "Partners":
                    PartnerIcon.Source = "nav_partners_press.png";
                    PartnerText.TextColor = Color.FromArgb("#146B4D");
                    break;

                case "Notifications":
                    BellIcon.Source = "nav_notification_press.png";
                    BellText.TextColor = Color.FromArgb("#146B4D");
                    break;

                case "More":
                    MoreIcon.Source = "nav_more_press.png";
                    MoreText.TextColor = Color.FromArgb("#146B4D");
                    break;
            }
        }

        // 🏠 Главная
        private async void OnHomeTapped(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _navigationLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnHomeTappedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BottomNavBar] Error in OnHomeTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _navigationLock.Release();
            }
        }

        private async Task OnHomeTappedAsync()
        {
            UpdateSelectedTab("Home");
            await Shell.Current.GoToAsync("//main/home");
        }

        // 🤝 Партнёры
        private async void OnPartnerTapped(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _navigationLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnPartnerTappedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BottomNavBar] Error in OnPartnerTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _navigationLock.Release();
            }
        }

        private async Task OnPartnerTappedAsync()
        {
            UpdateSelectedTab("Partners");
            await Shell.Current.GoToAsync("//main/partner");
        }

        // 📱 QR
        private async void OnQrTapped(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _navigationLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnQrTappedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BottomNavBar] Error in OnQrTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _navigationLock.Release();
            }
        }

        private async Task OnQrTappedAsync()
        {
            UpdateSelectedTab("QR");
            await Shell.Current.GoToAsync("//main/qr");
        }

        // 🔔 Уведомления
        private async void OnNotificationsTapped(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _navigationLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnNotificationsTappedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BottomNavBar] Error in OnNotificationsTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _navigationLock.Release();
            }
        }

        private async Task OnNotificationsTappedAsync()
        {
            UpdateSelectedTab("Notifications");
            await Shell.Current.GoToAsync("//main/notifications");
        }

        // ⋯ Ещё
        private async void OnMoreTapped(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _navigationLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnMoreTappedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BottomNavBar] Error in OnMoreTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _navigationLock.Release();
            }
        }

        private async Task OnMoreTappedAsync()
        {
            UpdateSelectedTab("More");
            await Shell.Current.GoToAsync("//main/more");
        }
    }
}
