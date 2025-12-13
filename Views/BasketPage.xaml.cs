using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using YessGoFront.Services.Domain;
using YessGoFront.Services.Api;
using YessGoFront.ViewModels;

namespace YessGoFront.Views;

public partial class BasketPage : ContentPage
{
    private readonly BasketViewModel _viewModel;
    private readonly IPaymentApiService? _paymentApiService;
    private bool _isProcessingPayment = false;

    public BasketPage()
    {
        InitializeComponent();

        // Получаем сервисы через DI
        var cartService = MauiProgram.Services.GetRequiredService<ICartService>();
        var partnersService = MauiProgram.Services.GetRequiredService<IPartnersService>();
        var logger = MauiProgram.Services.GetService<ILogger<BasketViewModel>>();

        _viewModel = new BasketViewModel(cartService, partnersService, logger);
        BindingContext = _viewModel;

        // Получаем сервис платежей
        try
        {
            _paymentApiService = MauiProgram.Services?.GetService<IPaymentApiService>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BasketPage] Error getting PaymentApiService: {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Загружаем данные корзины при появлении страницы
        if (_viewModel != null)
        {
            await _viewModel.LoadCartCommand.ExecuteAsync(null);
        }
    }

    public async void OnBackButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (Shell.Current == null)
            {
                return;
            }

            // Сначала пытаемся использовать Navigation.PopAsync (более надежный способ)
            if (Shell.Current.Navigation != null && Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync(animated: true);
                return;
            }

            // Если Navigation.PopAsync не сработал, используем Shell навигацию
            await Shell.Current.GoToAsync("..", animate: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BasketPage] Navigation error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BasketPage] StackTrace: {ex.StackTrace}");
            
            // Fallback: попытка вернуться назад через Shell навигацию
            try
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("..", animate: true);
                }
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"[BasketPage] Fallback navigation error: {fallbackEx.Message}");
            }
        }
    }

    private async void OnTopUpButtonClicked(object sender, EventArgs e)
    {
        if (_isProcessingPayment || _paymentApiService == null)
        {
            return;
        }

        try
        {
            _isProcessingPayment = true;

            // Используем минимальную сумму пополнения (100 сом) или можно сделать выбор суммы
            decimal amount = 100m;

            Debug.WriteLine($"[BasketPage] Starting payment for amount: {amount} KGS");

            // Используем таймаут для создания платежа (15 секунд)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            // Вызываем backend для создания платежа
            var response = await _paymentApiService.CreatePaymentAsync(amount, cts.Token);

            _isProcessingPayment = false;

            // Проверяем, что получили paymentUrl
            if (string.IsNullOrWhiteSpace(response?.PaymentUrl))
            {
                Debug.WriteLine("[BasketPage] PaymentUrl is empty in response");
                await DisplayAlert("Ошибка", "Не удалось получить ссылку на оплату. Попробуйте снова.", "OK");
                return;
            }

            Debug.WriteLine($"[BasketPage] Payment created, opening WebView with URL: {response.PaymentUrl}");

            // Открываем FinikPaymentPage с paymentUrl и redirectUrl
            var paymentUrlEncoded = Uri.EscapeDataString(response.PaymentUrl);
            var redirectUrlEncoded = !string.IsNullOrWhiteSpace(response.RedirectUrl) 
                ? Uri.EscapeDataString(response.RedirectUrl) 
                : string.Empty;
            
            // Используем полное имя маршрута для навигации
            var navigationPath = !string.IsNullOrWhiteSpace(redirectUrlEncoded)
                ? $"FinikPaymentPage?paymentUrl={paymentUrlEncoded}&redirectUrl={redirectUrlEncoded}"
                : $"FinikPaymentPage?paymentUrl={paymentUrlEncoded}";
            
            Debug.WriteLine($"[BasketPage] Navigating to: {navigationPath}");
            await Shell.Current.GoToAsync(navigationPath, animate: true);
        }
        catch (OperationCanceledException)
        {
            _isProcessingPayment = false;
            
            Debug.WriteLine("[BasketPage] Payment creation timed out");
            await DisplayAlert("Ошибка", "Операция создания платежа заняла слишком много времени. Проверьте подключение к интернету и попробуйте снова.", "OK");
        }
        catch (Exception ex)
        {
            _isProcessingPayment = false;
            
            Debug.WriteLine($"[BasketPage] Error processing payment: {ex.Message}");
            Debug.WriteLine($"[BasketPage] Exception type: {ex.GetType().Name}");
            Debug.WriteLine($"[BasketPage] StackTrace: {ex.StackTrace}");
            
            // Показываем общее сообщение об ошибке
            var userMessage = "Произошла ошибка при создании платежа. Пожалуйста, проверьте подключение к интернету и попробуйте снова.";
            await DisplayAlert("Ошибка", userMessage, "OK");
        }
    }
}
