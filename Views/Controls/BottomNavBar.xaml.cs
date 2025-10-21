using Microsoft.Maui.Controls;

namespace YessGoFront.Views.Controls;

public enum BottomTab
{
    Home,
    Partner,
    QR,
    Notifications,
    More
}

public partial class BottomNavBar : ContentView
{
    public static readonly BindableProperty SelectedTabProperty =
        BindableProperty.Create(
            nameof(SelectedTab),
            typeof(BottomTab),
            typeof(BottomNavBar),
            BottomTab.Home,
            BindingMode.TwoWay,
            propertyChanged: OnSelectedTabChanged);

    public BottomTab SelectedTab
    {
        get => (BottomTab)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public BottomNavBar()
    {
        InitializeComponent();
        UpdateIcons();
    }

    private static void OnSelectedTabChanged(BindableObject bindable, object oldValue, object newValue)
        => ((BottomNavBar)bindable).UpdateIcons();

    private void UpdateIcons()
    {
        // обычные иконки
        HomeIcon.Source = "nav_home";
        PartnerIcon.Source = "nav_partners";
        NotificationIcon.Source = "nav_notification";
        MoreIcon.Source = "nav_more";
        // QR по макету без press-варианта, остаётся зелёным в центре

        // активная вкладка
        switch (SelectedTab)
        {
            case BottomTab.Home:
                HomeIcon.Source = "nav_home_press";
                break;
            case BottomTab.Partner:
                PartnerIcon.Source = "nav_partners_press";
                break;
            case BottomTab.Notifications:
                NotificationIcon.Source = "nav_notification_press";
                break;
            case BottomTab.More:
                MoreIcon.Source = "nav_more_press";
                break;
            case BottomTab.QR:
                // центр без изменения
                break;
        }
    }

    // Порядок важен: 1) меняем SelectedTab (иконки), 2) навигируем
    private async void OnHomeTapped(object? s, TappedEventArgs e)
    { if (SelectedTab != BottomTab.Home) SelectedTab = BottomTab.Home; await Shell.Current.GoToAsync("//home"); }

    private async void OnPartnerTapped(object? s, TappedEventArgs e)
    { if (SelectedTab != BottomTab.Partner) SelectedTab = BottomTab.Partner; await Shell.Current.GoToAsync("//partner"); }

    private async void OnQrTapped(object? s, TappedEventArgs e)
    { if (SelectedTab != BottomTab.QR) SelectedTab = BottomTab.QR; await Shell.Current.GoToAsync("//qr"); }

    private async void OnNotificationsTapped(object? s, TappedEventArgs e)
    { if (SelectedTab != BottomTab.Notifications) SelectedTab = BottomTab.Notifications; await Shell.Current.GoToAsync("//notifications"); }

    private async void OnMoreTapped(object? s, TappedEventArgs e)
    { if (SelectedTab != BottomTab.More) SelectedTab = BottomTab.More; await Shell.Current.GoToAsync("//more"); }
}
