using YessGoFront.Views.Controls;

namespace YessGoFront.Views;

public partial class RefundPolicyPage : ContentPage
{
    public RefundPolicyPage()
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

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }
            
            await Shell.Current.GoToAsync("///main/more", animate: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RefundPolicyPage] Ошибка навигации: {ex.Message}");
        }
        finally
        {
            if (sender is Button button)
            {
                button.IsEnabled = true;
            }
        }
    }
}

