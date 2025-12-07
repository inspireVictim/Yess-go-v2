using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YessGoFront.Models;
using YessGoFront.Services.Api;

namespace YessGoFront.Views;

public partial class SearchPartnersPay : ContentPage, INotifyPropertyChanged
{
    private readonly IPartnersApiService? _partnersApiService;
    private CancellationTokenSource? _searchCts;
    private PartnerDto? _selectedPartner;
    private decimal _amount = 0;
    private bool _isNavigating = false;
    private bool _hasPartners = false;
    private bool _hasValidAmount = false;

    public ObservableCollection<PartnerDto> Partners { get; } = new();
    
    public bool HasPartners
    {
        get => _hasPartners;
        set
        {
            if (_hasPartners != value)
            {
                _hasPartners = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool HasValidAmount
    {
        get => _hasValidAmount;
        set
        {
            if (_hasValidAmount != value)
            {
                _hasValidAmount = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public SearchPartnersPay()
    {
        InitializeComponent();
        
        // Получаем сервис из DI
        _partnersApiService = MauiProgram.Services?.GetService<IPartnersApiService>();
        
        BindingContext = this;
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;

        try
        {
            // Если открыта форма оплаты, возвращаемся к поиску
            if (PaymentFormState.IsVisible)
            {
                ShowSearchState();
            }
            else
            {
                // Иначе возвращаемся на предыдущую страницу
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("..", animate: true);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Navigation error: {ex.Message}");
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim() ?? string.Empty;

        // Отменяем предыдущий поиск
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        if (string.IsNullOrWhiteSpace(query))
        {
            Partners.Clear();
            HasPartners = false;
            NoResultsLabel.IsVisible = false;
            LoadingIndicator.IsVisible = false;
            return;
        }

        // Показываем индикатор загрузки
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        NoResultsLabel.IsVisible = false;

        try
        {
            // Дебаунс: ждем 400мс после последнего ввода
            await Task.Delay(400, ct);

            if (ct.IsCancellationRequested)
                return;

            if (_partnersApiService == null)
            {
                Debug.WriteLine("[SearchPartnersPay] PartnersApiService is null");
                return;
            }

            // Выполняем поиск
            var results = await _partnersApiService.SearchAsync(query, ct);

            if (ct.IsCancellationRequested)
                return;

            // Обновляем список
            Partners.Clear();
            foreach (var partner in results)
            {
                Partners.Add(partner);
            }

            // Обновляем HasPartners
            HasPartners = Partners.Count > 0;

            // Показываем сообщение, если результатов нет
            NoResultsLabel.IsVisible = Partners.Count == 0;
        }
        catch (OperationCanceledException)
        {
            // Поиск был отменен, игнорируем
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Search error: {ex.Message}");
            NoResultsLabel.IsVisible = true;
            NoResultsLabel.Text = "Ошибка при поиске партнеров";
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnPartnerSelected(object? sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is PartnerDto partner)
        {
            _selectedPartner = partner;
            ShowPaymentForm();
        }
    }

    private void ShowPaymentForm()
    {
        if (_selectedPartner == null) return;

        // Скрываем состояние поиска
        SearchState.IsVisible = false;

        // Показываем форму оплаты
        PaymentFormState.IsVisible = true;

        // Обновляем информацию о партнере
        SelectedPartnerName.Text = _selectedPartner.Name ?? "Партнер";
        SelectedPartnerCategory.Text = _selectedPartner.Category ?? "";
        
        if (!string.IsNullOrEmpty(_selectedPartner.LogoUrl))
        {
            SelectedPartnerLogo.Source = _selectedPartner.LogoUrl;
        }
        else
        {
            SelectedPartnerLogo.Source = null;
        }

        // Сбрасываем сумму
        AmountEntry.Text = "";
        _amount = 0;
        UpdatePaymentInfo();
    }

    private void ShowSearchState()
    {
        // Скрываем форму оплаты
        PaymentFormState.IsVisible = false;

        // Показываем состояние поиска
        SearchState.IsVisible = true;

        _selectedPartner = null;
    }

    private void OnAmountTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            _amount = 0;
            UpdatePaymentInfo();
            return;
        }

        if (decimal.TryParse(text, out var amount) && amount > 0)
        {
            _amount = amount;
            AmountErrorLabel.IsVisible = false;
        }
        else
        {
            _amount = 0;
            AmountErrorLabel.IsVisible = !string.IsNullOrWhiteSpace(text);
        }

        UpdatePaymentInfo();
    }

    private void UpdatePaymentInfo()
    {
        PaymentAmountLabel.Text = $"{_amount:0.##} Yess!Coin";
        HasValidAmount = _amount > 0;
        PayButton.IsEnabled = _amount > 0 && _selectedPartner != null;
    }

    private async void OnPayButtonClicked(object? sender, EventArgs e)
    {
        if (_amount <= 0 || _selectedPartner == null)
        {
            Debug.WriteLine("[SearchPartnersPay] Invalid payment data");
            return;
        }

        // TODO: Реализовать логику оплаты
        Debug.WriteLine($"[SearchPartnersPay] Paying {_amount} Yess!Coin to partner {_selectedPartner.Name} (ID: {_selectedPartner.Id})");

        // Показываем сообщение об успехе (временное решение)
        await DisplayAlert("Оплата", $"Перевод {_amount} Yess!Coin партнеру {_selectedPartner.Name} будет выполнен", "OK");
    }
}