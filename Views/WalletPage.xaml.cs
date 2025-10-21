using Microsoft.Maui.Controls;

namespace YessGoFront.Views
{
    public partial class WalletPage : ContentPage
    {
        public WalletPage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            // 1) если есть стек навигации — используем PopAsync
            if (Navigation?.NavigationStack?.Count > 1)
            {
                await Navigation.PopAsync();
                return;
            }

            // 2) иначе пробуем Shell относительный back
            try
            {
                await Shell.Current.GoToAsync("..");
                return;
            }
            catch { /* игнор */ }

            // 3) крайний случай: уходим на корень home (твоя вкладка)
            await Shell.Current.GoToAsync("//home");
        }

        private async void OnTopUpClicked(object? sender, EventArgs e)
        {
            await DisplayAlert("Пополнение", "Здесь будет логика пополнения.", "OK");
        }

        private async void OnAboutCoinsClicked(object? sender, EventArgs e)
        {
            await DisplayAlert("Yess!Coin", "Здесь откроем страницу с объяснением.", "OK");
        }
    }
}
