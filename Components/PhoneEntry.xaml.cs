using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace YessGoFront.Components;

public partial class PhoneEntry : Border
{
    public static readonly BindableProperty PhoneNumberProperty =
        BindableProperty.Create(
            nameof(PhoneNumber),
            typeof(string),
            typeof(PhoneEntry),
            string.Empty,
            BindingMode.TwoWay,
            propertyChanged: OnPhoneNumberChangedExternally);

    public static readonly BindableProperty FullPhoneNumberProperty =
        BindableProperty.Create(
            nameof(FullPhoneNumber),
            typeof(string),
            typeof(PhoneEntry),
            string.Empty,
            BindingMode.TwoWay,
            propertyChanged: OnFullPhoneNumberChangedExternally);

    public string PhoneNumber
    {
        get => (string)GetValue(PhoneNumberProperty);
        set => SetValue(PhoneNumberProperty, value);
    }

    public string FullPhoneNumber
    {
        get => (string)GetValue(FullPhoneNumberProperty);
        set => SetValue(FullPhoneNumberProperty, value);
    }

    public bool IsValid { get; private set; }

    private bool _isInternalUpdate = false;
    private CancellationTokenSource _cts = new();

    public PhoneEntry()
    {
        InitializeComponent();

        PhoneNumberEntryBinding.BindingContext = this;
        PhoneNumberEntryBinding.SetBinding(Entry.TextProperty, nameof(PhoneNumber));
    }

    // --------------------------
    // MAIN INPUT HANDLER (Android-safe)
    // --------------------------
    private async void OnPhoneNumberChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInternalUpdate)
            return;

        var raw = e.NewTextValue ?? string.Empty;

        // Нормализация
        var cleaned = NormalizeNumber(raw);
        var formatted = FormatNumber(cleaned);

        // Обновляем внутренние свойства
        _isInternalUpdate = true;
        PhoneNumber = cleaned;
        FullPhoneNumber = "+996" + cleaned;
        IsValid = cleaned.Length == 9;
        ValidationIndicator.IsVisible = IsValid;
        _isInternalUpdate = false;

        //
        // 🔥 VERY IMPORTANT: async update to avoid EmojiCompat crash
        //
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            // Дать Android IME завершить обработку
            await Task.Delay(1, token);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!token.IsCancellationRequested &&
                    PhoneNumberEntryBinding.Text != formatted)
                {
                    _isInternalUpdate = true;
                    PhoneNumberEntryBinding.Text = formatted;
                    _isInternalUpdate = false;
                }
            });
        }
        catch (TaskCanceledException)
        {
            // игнорируем
        }
    }

    // --------------------------
    // NORMALIZATION
    // --------------------------
    private static string NormalizeNumber(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var digits = Regex.Replace(raw, @"[^\d]", "");

        // ВСЕГДА убираем ведущий 996
        if (digits.StartsWith("996"))
            digits = digits[3..];

        // Убираем ведущий 0
        if (digits.StartsWith("0"))
            digits = digits[1..];

        // Ограничиваем 9 цифрами
        if (digits.Length > 9)
            digits = digits[..9];

        return digits;
    }

    // --------------------------
    // FORMATTER
    // --------------------------
    private static string FormatNumber(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        return digits.Length switch
        {
            <= 3 => digits,
            <= 5 => $"{digits[..3]} {digits[3..]}",
            <= 7 => $"{digits[..3]} {digits[3..5]} {digits[5..]}",
            _ => $"{digits[..3]} {digits[3..5]} {digits[5..7]} {digits[7..]}"
        };
    }

    // --------------------------
    // PROPERTY CHANGED (EXTERNAL SET)
    // --------------------------
    private static void OnPhoneNumberChangedExternally(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PhoneEntry control || control._isInternalUpdate)
            return;

        control._isInternalUpdate = true;

        var digits = NormalizeNumber(newValue?.ToString() ?? string.Empty);
        var formatted = FormatNumber(digits);

        control.PhoneNumberEntryBinding.Text = formatted;
        control.FullPhoneNumber = "+996" + digits;

        control.IsValid = digits.Length == 9;
        control.ValidationIndicator.IsVisible = control.IsValid;

        control._isInternalUpdate = false;
    }

    private static void OnFullPhoneNumberChangedExternally(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PhoneEntry control || control._isInternalUpdate)
            return;

        control._isInternalUpdate = true;

        var digits = NormalizeNumber(newValue?.ToString() ?? string.Empty);
        control.PhoneNumber = digits;

        control._isInternalUpdate = false;
    }
}
