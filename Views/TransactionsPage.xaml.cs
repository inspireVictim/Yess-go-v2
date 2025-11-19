using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;

namespace YessGoFront.Views
{
    public partial class TransactionsPage : ContentPage
    {
        public TransactionsPage()
        {
            InitializeComponent();

            var walletService = MauiProgram.Services.GetRequiredService<IWalletService>();
            BindingContext = new TransactionsViewModel(walletService);

            if (BindingContext is TransactionsViewModel vm)
            {
                _ = vm.LoadTransactionsCommand.ExecuteAsync(null);
            }
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//main");
        }

        private async void OnBackTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//main");
        }

        private void OnAllFilterClicked(object? sender, EventArgs e)
        {
            if (BindingContext is TransactionsViewModel vm)
            {
                vm.CurrentFilter = TransactionsFilterType.All;
            }
        }

        private void OnIncomeFilterClicked(object? sender, EventArgs e)
        {
            if (BindingContext is TransactionsViewModel vm)
            {
                vm.CurrentFilter = TransactionsFilterType.Income;
            }
        }

        private void OnExpenseFilterClicked(object? sender, EventArgs e)
        {
            if (BindingContext is TransactionsViewModel vm)
            {
                vm.CurrentFilter = TransactionsFilterType.Expense;
            }
        }

        private async void OnTransactionTapped(object? sender, EventArgs e)
        {
            if (sender is not VisualElement element)
                return;

            if ((element as IGestureRecognizers)?.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is string id)
            {
                await Shell.Current.GoToAsync($"transactiondetails?id={Uri.EscapeDataString(id)}");
            }
        }
    }
}
