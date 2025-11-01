using System;
using Microsoft.Maui.Controls;
using YessGoFront.Services;

namespace YessGoFront.Views
{
    public partial class WalletPage : ContentPage
    {
        public WalletPage()
        {
            InitializeComponent();
            // Привязываем страницу к общему хранилищу баланса
            BindingContext = BalanceStore.Instance;
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///main/partner");
        }

        private void OnOtherCheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            // Разрешаем ввод «другой суммы», если выбран соответствующий пункт
            if (entryOther != null)
                entryOther.IsEnabled = rbOther?.IsChecked == true;
        }

        private async void OnAboutCoinsClicked(object? sender, EventArgs e)
        {
            await DisplayAlert("Yess!Coin", "Йесскоины — внутренняя валюта, накапливается за покупки у партнёров.", "OK");
        }

        private async void OnTopUpClicked(object? sender, EventArgs e)
        {
            try
            {
                decimal amount = 0;

                if (rbOther?.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(entryOther?.Text) || !decimal.TryParse(entryOther.Text, out amount) || amount <= 0)
                    {
                        await DisplayAlert("Ошибка", "Введите корректную сумму.", "OK");
                        return;
                    }
                }
                else
                {
                    // Считываем Value из отмеченной радиокнопки
                    amount = GetCheckedPresetAmount();
                    if (amount <= 0)
                    {
                        await DisplayAlert("Ошибка", "Выберите сумму пополнения.", "OK");
                        return;
                    }
                }

                // Обновляем общий баланс
                decimal coefficient = 2m;
                BalanceStore.Instance.Balance += amount * coefficient;

                await DisplayAlert("Готово", $"Баланс пополнен на {amount:0.##} KGS.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private decimal GetCheckedPresetAmount()
        {
            if (rb1000?.IsChecked == true) return 1000m;
            if (rb800?.IsChecked == true) return 800m;
            if (rb600?.IsChecked == true) return 600m;
            if (rb500?.IsChecked == true) return 500m;
            if (rb300?.IsChecked == true) return 300m;
            if (rb100?.IsChecked == true) return 100m;
            return 0m;
        }



    }
}
