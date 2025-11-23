namespace YessGoFront.Views;

public partial class ContactsPage : ContentView
{
	public ContactsPage()
	{
		InitializeComponent();
	}

	public async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///more");
    }
}