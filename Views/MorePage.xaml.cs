using YessGoFront.Views.Controls;

namespace YessGoFront.Views;

public partial class MorePage : ContentPage
{
    public MorePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BottomBar.SelectedTab = BottomTab.More;
    }
}
