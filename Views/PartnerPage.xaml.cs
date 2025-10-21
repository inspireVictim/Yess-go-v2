using YessGoFront.Views.Controls;

namespace YessGoFront.Views;

public partial class PartnerPage : ContentPage
{
    public PartnerPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BottomBar.SelectedTab = BottomTab.Partner;
    }
}
