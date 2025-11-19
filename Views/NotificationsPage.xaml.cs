using YessGoFront.Views.Controls;
using YessGoFront.ViewModels;

namespace YessGoFront.Views;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage(NotificationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Update bottom navigation bar
        if (BottomBar != null)
        {
            BottomBar.UpdateSelectedTab("Notifications");
        }

        // Load notifications
        if (BindingContext is NotificationsViewModel vm)
        {
            vm.LoadNotificationsCommand.Execute(null);
        }
    }
}
