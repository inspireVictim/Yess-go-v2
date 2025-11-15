using System;
using System.Text.RegularExpressions;
using Microsoft.Maui.ApplicationModel;

namespace YessGoFront.Components;

public partial class PhoneEntry : Border
{
    public event EventHandler<string>? PhoneChanged;

    public string PhoneNumber { get; private set; } = "";     // только 9 цифр
    public string FullPhoneNumber => "+996" + PhoneNumber;    // полный номер
    public bool IsValid => PhoneNumber.Length == 9;

    private bool _isUpdating;

    public PhoneEntry()
    {
        InitializeComponent();
        PhoneNumberEntryBinding.Text = "";
    }

    private void OnPhoneChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        if (_isUpdating)
            return;

        string oldRaw = e.OldTextValue ?? "";
        string newRaw = e.NewTextValue ?? "";

        // Извлекаем цифры
        string digitsOld = ExtractDigits(oldRaw);
        string digitsNew = ExtractDigits(newRaw);

        if (digitsOld == digitsNew)
            return;

        // Ограничиваем 9
        if (digitsNew.Length > 9)
            digitsNew = digitsNew[..9];

        string formatted = FormatDigits(digitsNew);

        // вычисляем позицию курсора (количество цифр слева)
        int cursorPos = entry.CursorPosition;
        int digitsBeforeCursor = CountDigits(newRaw[..Math.Min(cursorPos, newRaw.Length)]);

        // Обновление UI
        _isUpdating = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            entry.Text = formatted;

            // восстановление позиции курсора
            entry.CursorPosition = GetCursorPositionByDigits(formatted, digitsBeforeCursor);
            entry.SelectionLength = 0;

            PhoneNumber = digitsNew;
            PhoneChanged?.Invoke(this, FullPhoneNumber);

            if (ValidationIndicator != null)
                ValidationIndicator.IsVisible = IsValid;

            _isUpdating = false;
        });
    }

    // ------------------------------------------
    // Утилиты
    // ------------------------------------------

    private static string ExtractDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        string digits = Regex.Replace(input, @"\D", "");

        // Убираем +996 или 996
        if (digits.StartsWith("996"))
            digits = digits[3..];

        // Убираем ведущий 0
        if (digits.StartsWith("0"))
            digits = digits[1..];

        return digits;
    }

    private static int CountDigits(string s)
    {
        int count = 0;
        foreach (var ch in s)
            if (char.IsDigit(ch))
                count++;
        return count;
    }

    private static int GetCursorPositionByDigits(string formatted, int digitsBefore)
    {
        if (digitsBefore <= 0)
            return 0;

        int count = 0;
        for (int i = 0; i < formatted.Length; i++)
        {
            if (char.IsDigit(formatted[i]))
            {
                count++;
                if (count == digitsBefore)
                    return i + 1;
            }
        }

        return formatted.Length;
    }

    private static string FormatDigits(string digits)
    {
        if (digits.Length <= 3)
            return digits;

        if (digits.Length <= 6)
            return $"{digits[..3]} {digits[3..]}";

        return $"{digits[..3]} {digits[3..6]} {digits[6..]}";
    }
}
