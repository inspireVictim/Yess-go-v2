using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YessGoFront.Data;
using YessGoFront.Data.Entities;
using YessGoFront.Services.Domain;
using Microsoft.EntityFrameworkCore;

namespace YessGoFront.ViewModels;

public partial class NotificationsViewModel : BaseViewModel
{
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _dbContext;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool hasMoreItems = true;

    [ObservableProperty]
    private int unreadCount;

    public ObservableCollection<Notification> Notifications { get; } = new();

    private int _currentPage = 1;
    private const int PageSize = 20;

    public IAsyncRelayCommand LoadNotificationsCommand { get; }
    public IAsyncRelayCommand LoadMoreCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<Notification> MarkAsReadCommand { get; }
    public IAsyncRelayCommand MarkAllAsReadCommand { get; }

    public NotificationsViewModel(INotificationService notificationService, AppDbContext dbContext)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        LoadNotificationsCommand = new AsyncRelayCommand(LoadInitialAsync);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => hasMoreItems && !isBusy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        MarkAsReadCommand = new AsyncRelayCommand<Notification?>(MarkAsReadAsync, notification => notification != null && notification.ReadAt == null);
        MarkAllAsReadCommand = new AsyncRelayCommand(MarkAllAsReadAsync, () => Notifications.Any(n => n.ReadAt == null));
    }

    private async Task LoadInitialAsync()
    {
        if (isBusy)
            return;

        try
        {
            isBusy = true;
            hasError = false;
            errorMessage = null;

            _currentPage = 1;
            Notifications.Clear();
            hasMoreItems = true;

            await LoadPageAsync(_currentPage);
            await LoadUnreadCountAsync();
        }
        catch (Exception ex)
        {
            hasError = true;
            errorMessage = ex.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task LoadMoreAsync()
    {
        if (isBusy || !hasMoreItems)
            return;

        try
        {
            isBusy = true;
            _currentPage++;
            await LoadPageAsync(_currentPage);
        }
        catch (Exception ex)
        {
            hasError = true;
            errorMessage = ex.Message;
            hasMoreItems = false;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        if (isBusy)
            return;

        try
        {
            isRefreshing = true;
            _currentPage = 1;
            Notifications.Clear();
            hasMoreItems = true;
            await LoadPageAsync(_currentPage);
            await LoadUnreadCountAsync();
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private async Task LoadPageAsync(int page)
    {
        var userId = _dbContext.Users.FirstOrDefault()?.Id ?? 1; // TODO: Get from authenticated user

        var notifications = await _notificationService.GetNotificationsAsync(userId, page, PageSize);
        
        if (!notifications.Any())
        {
            hasMoreItems = false;
            return;
        }

        foreach (var notification in notifications.OrderByDescending(n => n.CreatedAt))
        {
            Notifications.Add(notification);
        }
    }

    private async Task LoadUnreadCountAsync()
    {
        var userId = _dbContext.Users.FirstOrDefault()?.Id ?? 1; // TODO: Get from authenticated user

        unreadCount = await _notificationService.GetUnreadCountAsync(userId);
    }

    private async Task MarkAsReadAsync(Notification? notification)
    {
        if (notification?.ReadAt != null)
            return;

        try
        {
            if (notification == null) return;
            await _notificationService.MarkAsReadAsync(notification.Id);
            notification.ReadAt = DateTime.UtcNow;
            
            // Update the notification in the collection
            var index = Notifications.IndexOf(notification);
            if (index >= 0)
            {
                Notifications[index] = notification;
            }

            unreadCount = Math.Max(0, unreadCount - 1);
        }
        catch (Exception ex)
        {
            hasError = true;
            errorMessage = ex.Message;
        }
    }

    private async Task MarkAllAsReadAsync()
    {
        var unreadNotifications = Notifications.Where(n => n.ReadAt == null).ToList();
        if (!unreadNotifications.Any())
            return;

        try
        {
            var userId = _dbContext.Users.FirstOrDefault()?.Id ?? 1; // TODO: Get from authenticated user

            await _notificationService.MarkAllAsReadAsync(userId);

            foreach (var notification in unreadNotifications)
            {
                notification.ReadAt = DateTime.UtcNow;
                var index = Notifications.IndexOf(notification);
                if (index >= 0)
                {
                    Notifications[index] = notification;
                }
            }

            unreadCount = 0;
        }
        catch (Exception ex)
        {
            hasError = true;
            errorMessage = ex.Message;
        }
    }

    public string GetFormattedTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1)
            return "Только что";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} мин назад";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} ч назад";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays} д назад";
        
        return dateTime.ToString("dd.MM.yyyy");
    }

    public string GetPriorityColor(NotificationPriority priority)
    {
        return priority switch
        {
            NotificationPriority.Urgent => "#DC2626",
            NotificationPriority.High => "#F59E0B",
            NotificationPriority.Normal => "#6B7280",
            NotificationPriority.Low => "#9CA3AF",
            _ => "#6B7280"
        };
    }
}