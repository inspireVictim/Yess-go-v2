using System;
using System.Threading;
using System.Threading.Tasks;
using YessGoFront.Views.Controls;

namespace YessGoFront.Views;

public partial class PublicOfferPage : ContentPage
{
    private readonly SemaphoreSlim _actionLock = new(1, 1);

    public PublicOfferPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Update bottom navigation bar
        if (this.FindByName<BottomNavBar>("BottomBar") is BottomNavBar bottomBar)
        {
            bottomBar.UpdateSelectedTab("More");
        }
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
            System.Diagnostics.Debug.WriteLine($"[PublicOfferPage] Error in OnBackTapped: {ex.Message}");
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

