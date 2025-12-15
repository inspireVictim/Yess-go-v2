using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using YessGoFront.Models;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;
using YessGoFront.Services;
using YessGoFront.Data;
using YessGoFront.Data.Entities;

namespace YessGoFront.Views;

public partial class SearchPartnersPay : ContentPage, INotifyPropertyChanged
{
    private readonly IPartnersApiService? _partnersApiService;
    private readonly IWalletService? _walletService;
    private readonly IWalletApiService? _walletApiService;
    private readonly IAuthService? _authService;
    private CancellationTokenSource? _searchCts;
    private PartnerDto? _selectedPartner;
    private decimal _amount = 0;
    private decimal _userBalance = 0;
    private bool _isNavigating = false;
    private bool _hasPartners = false;
    private bool _hasValidAmount = false;
    private List<PartnerDto> _allPartners = new(); // Кэш всех партнеров
    private bool _isAppearing = false; // Защита от повторных вызовов OnAppearing
    private readonly SemaphoreSlim _actionLock = new(1, 1); // Защита от повторных нажатий

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
        
        // Получаем сервисы из DI
        _partnersApiService = MauiProgram.Services?.GetService<IPartnersApiService>();
        _walletService = MauiProgram.Services?.GetService<IWalletService>();
        _walletApiService = MauiProgram.Services?.GetService<IWalletApiService>();
        _authService = MauiProgram.Services?.GetService<IAuthService>();
        
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (_isAppearing)
            return; // Уже выполняется

        _isAppearing = true;
        try
        {
            await OnAppearingAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error in OnAppearing: {ex.Message}");
        }
        finally
        {
            _isAppearing = false;
        }
    }

    protected virtual async Task OnAppearingAsync()
    {
        // Загружаем баланс пользователя
        await LoadUserBalanceAsync();
        
        // Загружаем всех партнеров при открытии страницы, если еще не загружены
        if (_allPartners.Count == 0 && _partnersApiService != null)
        {
            await LoadAllPartnersAsync();
        }
    }

    private async Task LoadUserBalanceAsync()
    {
        try
        {
            // Используем BalanceStore для быстрого доступа
            _userBalance = BalanceStore.Instance.Balance;
            
            // Обновляем баланс из API, если сервис доступен
            if (_walletService != null)
            {
                _userBalance = await _walletService.GetBalanceAsync();
                BalanceStore.Instance.Balance = _userBalance;
            }
            
            // Обновляем отображение баланса
            UpdateBalanceDisplay();
            
            Debug.WriteLine($"[SearchPartnersPay] User balance loaded: {_userBalance}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error loading balance: {ex.Message}");
            // Используем баланс из BalanceStore, если загрузка не удалась
            _userBalance = BalanceStore.Instance.Balance;
            UpdateBalanceDisplay();
        }
    }

    private void UpdateBalanceDisplay()
    {
        if (UserBalanceLabel != null)
        {
            UserBalanceLabel.Text = $"У вас в наличии: {_userBalance:0.##} Yess!Coin";
        }
    }

    private async Task LoadAllPartnersAsync()
    {
        if (_partnersApiService == null)
        {
            Debug.WriteLine("[SearchPartnersPay] PartnersApiService is null");
            return;
        }

        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            NoResultsLabel.IsVisible = false;

            // Загружаем всех партнеров
            var allPartners = await _partnersApiService.GetAllAsync();
            
            _allPartners = allPartners.ToList();
            
            // Показываем всех партнеров
            Partners.Clear();
            foreach (var partner in _allPartners)
            {
                Partners.Add(partner);
            }

            HasPartners = Partners.Count > 0;
            NoResultsLabel.IsVisible = Partners.Count == 0;
            
            Debug.WriteLine($"[SearchPartnersPay] Loaded {_allPartners.Count} partners");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error loading partners: {ex.Message}");
            NoResultsLabel.IsVisible = true;
            NoResultsLabel.Text = "Ошибка при загрузке партнеров";
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        // Защита от повторных нажатий
        if (!await _actionLock.WaitAsync(0))
            return; // Уже обрабатывается

        try
        {
            // Отключаем кнопку визуально
            if (sender is VisualElement element)
                element.IsEnabled = false;

            await OnBackButtonClickedAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error in OnBackButtonClicked: {ex.Message}");
        }
        finally
        {
            if (sender is VisualElement element)
                element.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnBackButtonClickedAsync()
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
        try
        {
            await OnSearchTextChangedAsync(e);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error in OnSearchTextChanged: {ex.Message}");
        }
    }

    private async Task OnSearchTextChangedAsync(TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim() ?? string.Empty;

        // Отменяем предыдущий поиск
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        // Если поле поиска пустое, показываем всех партнеров
        if (string.IsNullOrWhiteSpace(query))
        {
            Partners.Clear();
            foreach (var partner in _allPartners)
            {
                Partners.Add(partner);
            }
            HasPartners = Partners.Count > 0;
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
            // Debounce: ждем 500мс после последнего ввода (увеличено с 400мс для лучшей оптимизации)
            await Task.Delay(500, ct);

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

    private async void ShowPaymentForm()
    {
        try
        {
            await ShowPaymentFormAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error in ShowPaymentForm: {ex.Message}");
        }
    }

    private async Task ShowPaymentFormAsync()
    {
        if (_selectedPartner == null) return;

        // Обновляем баланс перед показом формы
        await LoadUserBalanceAsync();

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
            if (AmountErrorLabel != null)
            {
                AmountErrorLabel.IsVisible = false;
            }
        }
        else
        {
            _amount = 0;
            if (AmountErrorLabel != null)
            {
                AmountErrorLabel.IsVisible = !string.IsNullOrWhiteSpace(text);
            }
        }

        UpdatePaymentInfo();
    }

    private void UpdatePaymentInfo()
    {
        PaymentAmountLabel.Text = $"{_amount:0.##} Yess!Coin";
        HasValidAmount = _amount > 0;
        
        // Проверяем, достаточно ли средств
        var hasEnoughBalance = _amount > 0 && _userBalance >= _amount;
        PayButton.IsEnabled = hasEnoughBalance && _selectedPartner != null;
        
        // Показываем/скрываем сообщение о недостатке средств
        if (InsufficientFundsLabel != null)
        {
            InsufficientFundsLabel.IsVisible = _amount > 0 && _userBalance < _amount;
        }
    }

    private async void OnPayButtonClicked(object? sender, EventArgs e)
    {
        // Защита от повторных нажатий
        if (!await _actionLock.WaitAsync(0))
            return; // Уже обрабатывается

        try
        {
            // Отключаем кнопку визуально
            if (sender is VisualElement element)
                element.IsEnabled = false;

            await OnPayButtonClickedAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error in OnPayButtonClicked: {ex.Message}");
        }
        finally
        {
            if (sender is VisualElement element)
                element.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnPayButtonClickedAsync()
    {
        if (_amount <= 0 || _selectedPartner == null)
        {
            Debug.WriteLine("[SearchPartnersPay] Invalid payment data");
            return;
        }

        // Проверяем баланс перед оплатой
        if (_userBalance < _amount)
        {
            await DisplayAlert("Недостаточно средств", 
                $"Не хватает средств для перевода.\nУ вас: {_userBalance:0.##} Yess!Coin\nТребуется: {_amount:0.##} Yess!Coin", 
                "OK");
            return;
        }

        if (_walletApiService == null)
        {
            await DisplayAlert("Ошибка", "Сервис оплаты недоступен", "OK");
            return;
        }

        try
        {
            // Блокируем кнопку
            PayButton.IsEnabled = false;
            PayButton.Text = "Обработка...";

            Debug.WriteLine($"[SearchPartnersPay] Starting transfer: {_amount} Yess!Coin to partner {_selectedPartner.Name} (ID: {_selectedPartner.Id})");

            // Выполняем перевод через API
            var transaction = await _walletApiService.TransferToPartnerAsync(
                _selectedPartner.Id,
                _amount,
                $"Перевод партнеру {_selectedPartner.Name}",
                CancellationToken.None);

            Debug.WriteLine($"[SearchPartnersPay] Transfer completed: TransactionId={transaction.Id}");

            // Обновляем баланс
            await LoadUserBalanceAsync();
            
            // Обновляем баланс в BalanceStore для синхронизации с другими страницами
            BalanceStore.Instance.Balance = _userBalance;
            
            // Обновляем отображение баланса
            UpdateBalanceDisplay();

            // Создаем уведомление
            await CreatePaymentNotificationAsync(transaction);

            // Получаем данные пользователя для чека
            var userProfile = await GetUserProfileAsync();

            // Переходим на страницу чека с параметрами
            var partnerNameParam = Uri.EscapeDataString(_selectedPartner.Name ?? "Партнер");
            var firstNameParam = Uri.EscapeDataString(userProfile?.FirstName ?? "");
            var lastNameParam = Uri.EscapeDataString(userProfile?.LastName ?? "");
            var transactionIdParam = Uri.EscapeDataString(transaction.Id);
            var amountParam = Uri.EscapeDataString(_amount.ToString());
            var dateParam = Uri.EscapeDataString(transaction.CreatedAt.ToString("O"));

            var receiptRoute = $"receipt?transactionId={transactionIdParam}&partnerName={partnerNameParam}&amount={amountParam}&userFirstName={firstNameParam}&userLastName={lastNameParam}&date={dateParam}";
            await Shell.Current.GoToAsync(receiptRoute);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Payment error: {ex.Message}");
            await DisplayAlert("Ошибка", $"Не удалось выполнить перевод: {ex.Message}", "OK");
        }
        finally
        {
            // Разблокируем кнопку
            PayButton.IsEnabled = true;
            PayButton.Text = "Оплатить";
        }
    }

    private async Task CreatePaymentNotificationAsync(PurchaseDto transaction)
    {
        try
        {
            var localUser = await _authService?.GetLocalUserAsync();
            if (localUser == null)
            {
                Debug.WriteLine("[SearchPartnersPay] Cannot create notification: user not found");
                return;
            }

            using var scope = MauiProgram.Services?.CreateScope();
            if (scope == null)
            {
                Debug.WriteLine("[SearchPartnersPay] Cannot create notification: service scope is null");
                return;
            }

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var notification = new Notification
            {
                UserId = localUser.Id,
                Title = "Перевод выполнен",
                Message = $"Вы перевели {transaction.Amount:0.##} Yess!Coin партнеру {transaction.PartnerName ?? "партнеру"}. Дата: {transaction.CreatedAt:dd.MM.yyyy HH:mm}",
                NotificationType = NotificationType.InApp,
                Priority = NotificationPriority.Normal,
                Status = NotificationStatus.Delivered,
                CreatedAt = DateTime.UtcNow,
                DeliveredAt = DateTime.UtcNow,
                Data = new Dictionary<string, object>
                {
                    ["category"] = "finance",
                    ["transactionId"] = transaction.Id,
                    ["amount"] = transaction.Amount.ToString(),
                    ["partnerName"] = transaction.PartnerName ?? ""
                }
            };

            await dbContext.Notifications.AddAsync(notification);
            await dbContext.SaveChangesAsync();

            Debug.WriteLine($"[SearchPartnersPay] Notification created: Id={notification.Id}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error creating notification: {ex.Message}");
            // Не блокируем процесс, если уведомление не создалось
        }
    }

    private async Task<UserDto?> GetUserProfileAsync()
    {
        try
        {
            if (_authService == null)
                return null;

            return await _authService.GetUserProfileAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SearchPartnersPay] Error getting user profile: {ex.Message}");
            return null;
        }
    }
}