using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace YessGoFront.Views;

[QueryProperty(nameof(AmountString), "amount")]
public partial class Acquiring : ContentPage
{
    private string? _amountString;
    private decimal _amount;

    public string? AmountString
    {
        get => _amountString;
        set
        {
            _amountString = value;
            // Парсим строку в decimal
            if (!string.IsNullOrWhiteSpace(_amountString) && decimal.TryParse(_amountString, out var parsedAmount))
            {
                _amount = parsedAmount;
                // Обновляем Label с суммой после инициализации
                UpdateAmountLabel();
            }
        }
    }

    public decimal Amount => _amount;

    public Acquiring()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Обновляем Label при появлении страницы, если сумма уже установлена
        UpdateAmountLabel();
    }

    private void UpdateAmountLabel()
    {
        // Используем NameScopeExtensions для поиска элемента
        var amountLabel = NameScopeExtensions.FindByName<Label>(this, "AmountLabel");
        if (amountLabel != null)
        {
            amountLabel.Text = $"{_amount:0.##} KGS";
        }
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null)
            {
                return;
            }

            // Возвращаемся назад
            if (Shell.Current.Navigation != null && Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync(animated: true);
            }
            else
            {
                await Shell.Current.GoToAsync("..", animate: true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Acquiring] Navigation error: {ex.Message}");
        }
    }
}