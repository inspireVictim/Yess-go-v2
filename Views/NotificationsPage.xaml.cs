using YessGoFront.Views.Controls;
using YessGoFront.ViewModels;
using YessGoFront.Services.Domain;
using YessGoFront.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace YessGoFront.Views;

public partial class NotificationsPage : ContentPage
{
    private readonly NotificationsViewModel _viewModel;
    private readonly INotificationService _notificationService;
    private readonly IAuthService _authService;
    private readonly AppDbContext _dbContext;
    private bool _isInitialized = false;

    public NotificationsPage(NotificationsViewModel viewModel, INotificationService notificationService, IAuthService authService, AppDbContext dbContext)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _notificationService = notificationService;
        _authService = authService;
        _dbContext = dbContext;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try 
        {
            // Update bottom navigation bar
            if (this.FindByName<BottomNavBar>("BottomBar") is BottomNavBar bottomBar)
            {
                bottomBar.UpdateSelectedTab("Notifications");
            }
            
            // Ensure ViewModel is initialized and load notifications
            if (!_isInitialized)
            {
                await LoadInitialDataAsync();
                _isInitialized = true;
            }
            else
            {
                // Refresh the notifications
                await _viewModel.LoadInitialAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnAppearing: {ex}");
            // Optionally show an error message to the user
            await DisplayAlert("Ошибка", "Не удалось загрузить уведомления. Пожалуйста, попробуйте позже.", "OK");
        }
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            // Show loading indicator
            _viewModel.IsBusy = true;
            
            // Get current user ID
            var userId = await _authService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                Console.WriteLine("User is not authenticated");
                return;
            }
            
            // Debug: Check notifications in database
            var notificationsCount = await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .CountAsync();
                
            Console.WriteLine($"Found {notificationsCount} notifications in database for user {userId}");
            
            // If no notifications, create sample ones
            if (notificationsCount == 0)
            {
                Console.WriteLine("No notifications found, creating sample notifications...");
                await _notificationService.CreateSampleNotificationsAsync(userId.Value, 5);
            }

            // Load notifications through ViewModel
            await _viewModel.LoadInitialAsync();
            
            // Debug: Log the number of notifications loaded
            Console.WriteLine($"Loaded {_viewModel.Notifications.Count} notifications");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading initial data: {ex}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException}");
            }
        }
    }
}
