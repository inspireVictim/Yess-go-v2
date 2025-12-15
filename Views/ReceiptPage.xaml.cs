using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace YessGoFront.Views;

[QueryProperty(nameof(TransactionId), "transactionId")]
[QueryProperty(nameof(PartnerName), "partnerName")]
[QueryProperty(nameof(AmountStr), "amount")]
[QueryProperty(nameof(UserFirstName), "userFirstName")]
[QueryProperty(nameof(UserLastName), "userLastName")]
[QueryProperty(nameof(DateStr), "date")]
public partial class ReceiptPage : ContentPage
{
    private string? _transactionId;
    private string? _partnerName;
    private string? _amountStr;
    private string? _userFirstName;
    private string? _userLastName;
    private string? _dateStr;
    private readonly SemaphoreSlim _actionLock = new(1, 1);

    public string? TransactionId
    {
        get => _transactionId;
        set
        {
            _transactionId = value;
            UpdateReceipt();
        }
    }

    public string? PartnerName
    {
        get => _partnerName;
        set
        {
            _partnerName = value;
            UpdateReceipt();
        }
    }

    public string? AmountStr
    {
        get => _amountStr;
        set
        {
            _amountStr = value;
            UpdateReceipt();
        }
    }

    public string? UserFirstName
    {
        get => _userFirstName;
        set
        {
            _userFirstName = value;
            UpdateReceipt();
        }
    }

    public string? UserLastName
    {
        get => _userLastName;
        set
        {
            _userLastName = value;
            UpdateReceipt();
        }
    }

    public string? DateStr
    {
        get => _dateStr;
        set
        {
            _dateStr = value;
            UpdateReceipt();
        }
    }

    public ReceiptPage()
    {
        InitializeComponent();
    }

    private void UpdateReceipt()
    {
        if (DateLabel == null) return;

        // Устанавливаем дату
        if (!string.IsNullOrEmpty(_dateStr) && DateTime.TryParse(Uri.UnescapeDataString(_dateStr), out var dateTime))
        {
            DateLabel.Text = dateTime.ToString("dd.MM.yyyy HH:mm");
        }
        else
        {
            DateLabel.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        }

        // Устанавливаем отправителя
        var fullName = $"{Uri.UnescapeDataString(_userFirstName ?? "")} {Uri.UnescapeDataString(_userLastName ?? "")}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = "Пользователь";
        }
        if (FromLabel != null)
        {
            FromLabel.Text = fullName;
        }

        // Устанавливаем получателя
        if (ToLabel != null)
        {
            ToLabel.Text = Uri.UnescapeDataString(_partnerName ?? "Партнер");
        }

        // Устанавливаем сумму
        if (!string.IsNullOrEmpty(_amountStr) && decimal.TryParse(Uri.UnescapeDataString(_amountStr), out var amountValue))
        {
            if (AmountLabel != null)
            {
                AmountLabel.Text = $"{amountValue:0.##} Yess!Coin";
            }
        }
        else
        {
            if (AmountLabel != null)
            {
                AmountLabel.Text = "0 Yess!Coin";
            }
        }

        // Устанавливаем ID транзакции
        if (TransactionIdLabel != null)
        {
            TransactionIdLabel.Text = Uri.UnescapeDataString(_transactionId ?? "N/A");
        }

        Debug.WriteLine($"[ReceiptPage] Receipt updated: TransactionId={_transactionId}, Amount={_amountStr}, Partner={_partnerName}");
    }

    private async void OnCloseButtonClicked(object? sender, EventArgs e)
    {
        if (!await _actionLock.WaitAsync(0))
            return;

        try
        {
            if (sender is Button btn)
                btn.IsEnabled = false;

            await OnCloseButtonClickedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReceiptPage] Error in OnCloseButtonClicked: {ex.Message}");
        }
        finally
        {
            if (sender is Button btn)
                btn.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnCloseButtonClickedAsync()
    {
        // Возвращаемся на предыдущую страницу
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..", animate: true);
        }
    }
}

