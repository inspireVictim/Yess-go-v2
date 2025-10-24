using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace YessGoFront.Views.Controls
{
    public enum BottomTab
    {
        Home,
        Partner,
        Qr,
        Notifications,
        More
    }

    public partial class BottomNavBar : ContentView
    {
        bool _isNavigating = false;

        Image? _homeIcon, _partnerIcon, _qrIcon, _bellIcon, _moreIcon;
        Label? _homeText, _partnerText, _qrText, _bellText, _moreText;

        public BottomNavBar()
        {
            InitializeComponent();

            _homeIcon = FindByName("HomeIcon") as Image;
            _partnerIcon = FindByName("PartnerIcon") as Image;
            _qrIcon = FindByName("QrIcon") as Image;
            _bellIcon = FindByName("BellIcon") as Image;
            _moreIcon = FindByName("MoreIcon") as Image;

            _homeText = FindByName("HomeText") as Label;
            _partnerText = FindByName("PartnerText") as Label;
            _qrText = FindByName("QrText") as Label;
            _bellText = FindByName("BellText") as Label;
            _moreText = FindByName("MoreText") as Label;

            SelectedTab = BottomTab.Home;
        }

        BottomTab _selected;
        public BottomTab SelectedTab
        {
            get => _selected;
            set
            {
                _selected = value;
                UpdateVisual();
            }
        }

        string GetIcon(BottomTab tab, bool active)
        {
            return tab switch
            {
                BottomTab.Home => active ? "nav_home_press.png" : "nav_home.png",
                BottomTab.Partner => active ? "nav_partners_press.png" : "nav_partners.png",
                BottomTab.Qr => active ? "nav_qr_press.png" : "nav_qr.png",
                BottomTab.Notifications => active ? "nav_notification_press.png" : "nav_notification.png",
                BottomTab.More => active ? "nav_more_press.png" : "nav_more.png",
                _ => "nav_home.png"
            };
        }

        void UpdateVisual()
        {
            if (_homeIcon == null ||
                _partnerIcon == null ||
                _qrIcon == null ||
                _bellIcon == null ||
                _moreIcon == null ||
                _homeText == null ||
                _partnerText == null ||
                _qrText == null ||
                _bellText == null ||
                _moreText == null)
                return;

            var normal = Color.FromArgb("#6B6B6B");
            var active = Color.FromArgb("#0F6B53");

            _homeText.TextColor = normal;
            _partnerText.TextColor = normal;
            _qrText.TextColor = normal;
            _bellText.TextColor = normal;
            _moreText.TextColor = normal;

            _homeIcon.Source = GetIcon(BottomTab.Home, false);
            _partnerIcon.Source = GetIcon(BottomTab.Partner, false);
            _qrIcon.Source = GetIcon(BottomTab.Qr, false);
            _bellIcon.Source = GetIcon(BottomTab.Notifications, false);
            _moreIcon.Source = GetIcon(BottomTab.More, false);

            switch (_selected)
            {
                case BottomTab.Home:
                    _homeText.TextColor = active;
                    _homeIcon.Source = GetIcon(BottomTab.Home, true);
                    break;
                case BottomTab.Partner:
                    _partnerText.TextColor = active;
                    _partnerIcon.Source = GetIcon(BottomTab.Partner, true);
                    break;
                case BottomTab.Qr:
                    _qrText.TextColor = active;
                    _qrIcon.Source = GetIcon(BottomTab.Qr, true);
                    break;
                case BottomTab.Notifications:
                    _bellText.TextColor = active;
                    _bellIcon.Source = GetIcon(BottomTab.Notifications, true);
                    break;
                case BottomTab.More:
                    _moreText.TextColor = active;
                    _moreIcon.Source = GetIcon(BottomTab.More, true);
                    break;
            }
        }

        // ---------- Обработчики вкладок ----------
        async void OnHomeTapped(object? sender, EventArgs e)
            => await NavigateAsync(BottomTab.Home, "///main/home");

        async void OnPartnerTapped(object? sender, EventArgs e)
            => await NavigateAsync(BottomTab.Partner, "///main/partner");

        async void OnQrTapped(object? sender, EventArgs e)
            => await NavigateAsync(BottomTab.Qr, "///main/qr");

        async void OnNotificationsTapped(object? sender, EventArgs e)
            => await NavigateAsync(BottomTab.Notifications, "///main/notifications");

        async void OnMoreTapped(object? sender, EventArgs e)
            => await NavigateAsync(BottomTab.More, "///main/more");

        // ---------- Универсальный переход ----------
        async Task NavigateAsync(BottomTab target, string route)
        {
            if (_isNavigating)
                return;

            _isNavigating = true;

            try
            {
                // визуально показываем нажатие
                SelectedTab = target;

                // выполняем переход
                await Shell.Current.GoToAsync(route, true);

                // ждём, чтобы Shell применил состояние
                await Task.Delay(80);

                // финально выставляем выбранный таб
                SelectedTab = target;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BottomNavBar] Ошибка навигации: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}
