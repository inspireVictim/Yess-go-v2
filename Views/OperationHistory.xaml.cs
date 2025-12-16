using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YessGoFront.Views;

public partial class OperationHistory : ContentPage
{
    private readonly SemaphoreSlim _actionLock = new(1, 1);

	public OperationHistory()
	{
		InitializeComponent();
	}

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        if (!await _actionLock.WaitAsync(0))
            return;

        try
        {
            await OnBackTappedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OperationHistory] Error in OnBackTapped: {ex.Message}");
        }
        finally
        {
            _actionLock.Release();
        }
    }

    private async Task OnBackTappedAsync()
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