using YessGoFront.Views.Controls;

namespace YessGoFront.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BottomBar.SelectedTab = BottomTab.Notifications; // ← это убирает “двойной тап”
    }
}
