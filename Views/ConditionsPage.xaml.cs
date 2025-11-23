namespace YessGoFront.Views;

public partial class ConditionsPage : ContentView
{
	public ConditionsPage()
	{
		InitializeComponent();
	}

	public async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///more");
    }

}