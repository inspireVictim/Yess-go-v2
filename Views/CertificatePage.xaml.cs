namespace YessGoFront.Views;

public partial class CertificatePage : ContentView
{
	

	public async void OnBackTapped(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("///more");
    }
}