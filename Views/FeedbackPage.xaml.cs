using System;
using System.Threading;
using System.Threading.Tasks;

namespace YessGoFront.Views;

public partial class FeedbackPage : ContentPage
{
    private readonly SemaphoreSlim _actionLock = new(1, 1);

	public FeedbackPage()
	{
		InitializeComponent();
	}

    public async void OnBackTapped(object sender, EventArgs e)
    {
        if (!await _actionLock.WaitAsync(0))
            return;

        try
        {
            await OnBackTappedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FeedbackPage] Error in OnBackTapped: {ex.Message}");
        }
        finally
        {
            _actionLock.Release();
        }
    }

    private async Task OnBackTappedAsync()
    {
        await Shell.Current.GoToAsync("///more");
    }
}