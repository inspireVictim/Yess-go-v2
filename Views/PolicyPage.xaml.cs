namespace YessGoFront.Views;
public partial class PolicyPage : ContentView
{
	/*public PolicyPage()
	{
		InitializeComponent();
	}*/
	public async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///more");
    }
}