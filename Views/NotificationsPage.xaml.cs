using YessGoFront.Views.Controls;

namespace YessGoFront.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // ✅ Обновлённый вызов для новой навигационной панели
        if (this.FindByName<BottomNavBar>("BottomBar") is { } bottom)
            bottom.UpdateSelectedTab("Notifications");
    }
}
