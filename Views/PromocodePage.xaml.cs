using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using YessGoFront.ViewModels;

namespace YessGoFront.Views
{
    public partial class PromocodePage : ContentPage
    {
        private readonly SemaphoreSlim _actionLock = new(1, 1);

        public PromocodePage()
        {
            InitializeComponent();
            BindingContext = MauiProgram.Services.GetRequiredService<PromocodeViewModel>();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PromocodeViewModel viewModel)
            {
                viewModel.LoadPromoCodeHistoryCommand.ExecuteAsync(null);
            }
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
                System.Diagnostics.Debug.WriteLine($"[PromocodePage] Error in OnBackTapped: {ex.Message}");
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private async Task OnBackTappedAsync()
        {
            // Возвращаемся на страницу More
            await Shell.Current.GoToAsync("//main/more");
        }

    }
}
