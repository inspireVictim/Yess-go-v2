using Microsoft.Maui.Controls;

namespace YessGoFront.Views;

public partial class OperationHistory : ContentPage
{
	public OperationHistory()
	{
		InitializeComponent();
	}

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..", animate: true);
        }
        catch
        {
            // Fallback навигация
            try
            {
                await Shell.Current.GoToAsync("///main/more", animate: true);
            }
            catch { }
        }
    }
}