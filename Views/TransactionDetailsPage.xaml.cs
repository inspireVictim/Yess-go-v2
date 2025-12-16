using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;

namespace YessGoFront.Views
{
    [QueryProperty(nameof(TransactionId), "id")]
    public partial class TransactionDetailsPage : ContentPage
    {
        public string? TransactionId { get; set; }
        private bool _isAppearing = false;
        private readonly SemaphoreSlim _actionLock = new(1, 1);

        public TransactionDetailsPage()
        {
            InitializeComponent();

            var walletService = MauiProgram.Services.GetRequiredService<IWalletService>();
            var partnersApi = MauiProgram.Services.GetService<IPartnersApiService>();
            BindingContext = new TransactionDetailsViewModel(walletService, partnersApi);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isAppearing)
                return;

            _isAppearing = true;
            try
            {
                await OnAppearingAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TransactionDetailsPage] Error in OnAppearing: {ex.Message}");
            }
            finally
            {
                _isAppearing = false;
            }
        }

        protected virtual async Task OnAppearingAsync()
        {
            if (BindingContext is TransactionDetailsViewModel vm && !string.IsNullOrWhiteSpace(TransactionId))
            {
                await vm.LoadCommand.ExecuteAsync(TransactionId);
            }
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            if (!await _actionLock.WaitAsync(0))
                return;

            try
            {
                if (sender is Button btn)
                    btn.IsEnabled = false;

                await OnBackClickedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TransactionDetailsPage] Error in OnBackClicked: {ex.Message}");
            }
            finally
            {
                if (sender is Button btn)
                    btn.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnBackClickedAsync()
        {
            await Shell.Current.GoToAsync("..", true);
        }
    }
}
