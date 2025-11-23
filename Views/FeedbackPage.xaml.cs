namespace YessGoFront.Views;

public partial class FeedbackPage : ContentView
{
	/*public FeedbackPage()
	{
		InitializeComponent();
	}*/

    public async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///more");
    }
}