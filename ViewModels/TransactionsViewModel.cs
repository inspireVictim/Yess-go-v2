using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YessGoFront.Models;
using YessGoFront.Services.Domain;

namespace YessGoFront.ViewModels;

public enum TransactionsFilterType
{
    All,
    Income,
    Expense
}

public class TransactionGroup
{
    public string DateTitle { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public ObservableCollection<PurchaseDto> Items { get; } = new();
}

public partial class TransactionsViewModel : ObservableObject
{
    private readonly IWalletService _walletService;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private TransactionsFilterType currentFilter = TransactionsFilterType.All;
    [ObservableProperty] private bool hasMoreItems = true;

    public ObservableCollection<TransactionGroup> Groups { get; } = new();

    private int _currentPage = 1;
    private const int PageSize = 20;

    public IAsyncRelayCommand LoadTransactionsCommand { get; }
    public IAsyncRelayCommand LoadMoreCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public TransactionsViewModel(IWalletService walletService)
    {
        _walletService = walletService ?? throw new ArgumentNullException(nameof(walletService));

        LoadTransactionsCommand = new AsyncRelayCommand(LoadInitialAsync);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => HasMoreItems && !IsBusy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    partial void OnCurrentFilterChanged(TransactionsFilterType value)
    {
        _ = RefreshAsync();
    }

    private async Task LoadInitialAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = null;

            _currentPage = 1;
            Groups.Clear();
            HasMoreItems = true;

            await LoadPageAsync(_currentPage, reset: true);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadMoreAsync()
    {
        if (IsBusy || !HasMoreItems)
            return;

        try
        {
            IsBusy = true;
            _currentPage++;
            await LoadPageAsync(_currentPage, reset: false);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            HasMoreItems = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsRefreshing = true;
            _currentPage = 1;
            Groups.Clear();
            HasMoreItems = true;
            await LoadPageAsync(_currentPage, reset: true);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadPageAsync(int page, bool reset)
    {
        var items = await _walletService.GetTransactionHistoryAsync(page, PageSize, CancellationToken.None);

        var filtered = items.Where(FilterByType).OrderByDescending(x => x.CreatedAt);

        if (!filtered.Any())
        {
            if (page == 1)
                HasMoreItems = false;
            else
                HasMoreItems = false;

            return;
        }

        foreach (var item in filtered)
        {
            var date = item.CreatedAt.Date;
            var group = Groups.FirstOrDefault(g => g.Date == date);
            if (group == null)
            {
                group = new TransactionGroup
                {
                    Date = date,
                    DateTitle = date.ToString("dd.MM.yyyy")
                };
                Groups.Add(group);
            }

            group.Items.Add(item);
        }
    }

    private bool FilterByType(PurchaseDto dto)
    {
        return CurrentFilter switch
        {
            TransactionsFilterType.All => true,
            TransactionsFilterType.Income => string.Equals(dto.Type, "topup", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(dto.Type, "bonus", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(dto.Type, "refund", StringComparison.OrdinalIgnoreCase),
            TransactionsFilterType.Expense => string.Equals(dto.Type, "discount", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }
}
