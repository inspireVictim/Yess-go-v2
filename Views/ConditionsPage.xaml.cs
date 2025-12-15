using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YessGoFront.Views;

public partial class ConditionsPage : ContentPage
{
    private readonly SemaphoreSlim _actionLock = new(1, 1);

    public ConditionsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Обновляем нижний навбар
        if (this.FindByName<Controls.BottomNavBar>("BottomBar") is { } bottom)
            bottom.UpdateSelectedTab("More");
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
            System.Diagnostics.Debug.WriteLine($"[ConditionsPage] Error in OnBackTapped: {ex.Message}");
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