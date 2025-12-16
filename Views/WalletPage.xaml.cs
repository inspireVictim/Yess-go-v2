using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YessGoFront.Services;
using YessGoFront.Services.Domain;
using YessGoFront.Services.Api;

namespace YessGoFront.Views
{
    public partial class WalletPage : ContentPage
    {
        private readonly SemaphoreSlim _loadBalanceLock = new(1, 1);
        private readonly SemaphoreSlim _actionLock = new(1, 1); // Защита от повторных нажатий
        private DateTime _lastBalanceLoad = DateTime.MinValue;
        private const int BalanceCacheSeconds = 30;
        private bool _isAppearing = false; // Защита от повторных вызовов OnAppearing

        public WalletPage()
        {
            InitializeComponent();
            // Привязываем страницу к общему хранилищу баланса
            BindingContext = BalanceStore.Instance;
            
            // Загружаем баланс из БД при инициализации страницы
            _ = LoadBalanceAsync();
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
                System.Diagnostics.Debug.WriteLine($"[WalletPage] Error in OnAppearing: {ex.Message}");
            }
            finally
            {
                _isAppearing = false;
            }
        }

        protected virtual async Task OnAppearingAsync()
        {
            // Обновляем баланс при каждом появлении страницы
            _ = LoadBalanceAsync();
        }

        private async Task LoadBalanceAsync()
        {
            // Проверяем кэш
            if ((DateTime.Now - _lastBalanceLoad).TotalSeconds < BalanceCacheSeconds)
                return;

            if (!await _loadBalanceLock.WaitAsync(0))
                return; // Уже выполняется

            try
            {
                var walletService = MauiProgram.Services.GetService<IWalletService>();
                if (walletService != null)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    var balance = await walletService.GetBalanceAsync();
                    BalanceStore.Instance.Balance = balance;
                    _lastBalanceLoad = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"[WalletPage] Баланс загружен из БД: {balance}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WalletPage] Ошибка загрузки баланса: {ex.Message}");
            }
            finally
            {
                _loadBalanceLock.Release();
            }
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnBackClickedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WalletPage] Error in OnBackClicked: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnBackClickedAsync()
        {
            System.Diagnostics.Debug.WriteLine("[WalletPage] Кнопка 'Назад' нажата");
            
            try
            {
                // Переходим на главную страницу (home tab)
                System.Diagnostics.Debug.WriteLine("[WalletPage] Переходим на '//main/home'");
                await Shell.Current.GoToAsync("//main/home", animate: true);
                System.Diagnostics.Debug.WriteLine("[WalletPage] Успешно перешли на main/home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WalletPage] Ошибка при переходе на main/home: {ex.Message}");
                
                // Если не получилось, пробуем альтернативный маршрут
                try
                {
                    System.Diagnostics.Debug.WriteLine("[WalletPage] Пытаемся перейти на '//main'");
                    await Shell.Current.GoToAsync("//main", animate: true);
                    System.Diagnostics.Debug.WriteLine("[WalletPage] Успешно перешли на main");
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[WalletPage] Ошибка при переходе на main: {ex2.Message}");
                    
                    // Последняя попытка - вернуться назад
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("[WalletPage] Пытаемся вернуться назад через '..'");
                        await Shell.Current.GoToAsync("..", animate: true);
                        System.Diagnostics.Debug.WriteLine("[WalletPage] Успешно вернулись назад");
                    }
                    catch (Exception ex3)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WalletPage] Все маршруты не сработали: {ex3.Message}");
                    }
                }
            }
        }

        private void OnOtherCheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (rbOther == null) return;

            if (rbOther.IsChecked)
            {
                // Скрываем RadioButton "Другая сумма" и показываем Entry для ввода
                if (OtherAmountGrid != null)
                    OtherAmountGrid.IsVisible = false;
                
                if (entryOtherFull != null)
                {
                    entryOtherFull.IsVisible = true;
                    entryOtherFull.Focus();
                }
            }
            else
            {
                // Показываем RadioButton обратно и скрываем Entry
                if (OtherAmountGrid != null)
                    OtherAmountGrid.IsVisible = true;
                
                if (entryOtherFull != null)
                {
                    entryOtherFull.IsVisible = false;
                    entryOtherFull.Text = string.Empty;
                }
            }
        }

        private async void OnAboutCoinsClicked(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                await OnAboutCoinsClickedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WalletPage] Error in OnAboutCoinsClicked: {ex.Message}");
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private async Task OnAboutCoinsClickedAsync()
        {
            await DisplayAlert("Yess!Coin", "Йесскоины — внутренняя валюта, накапливается за покупки у партнёров.", "OK");
        }

        private async void OnTopUpClicked(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnTopUpClickedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WalletPage] Error in OnTopUpClicked: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnTopUpClickedAsync()
        {
            try
            {
                decimal amount = 0;

                if (rbOther?.IsChecked == true)
                {
                    // Используем entryOtherFull для ввода суммы
                    var amountText = entryOtherFull?.Text ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(amountText) || !decimal.TryParse(amountText, out amount) || amount <= 0)
                    {
                        await DisplayAlert("Ошибка", "Введите корректную сумму.", "OK");
                        return;
                    }
                }
                else
                {
                    // Считываем Value из отмеченной радиокнопки
                    amount = GetCheckedPresetAmount();
                    if (amount <= 0)
                    {
                        await DisplayAlert("Ошибка", "Выберите сумму пополнения.", "OK");
                        return;
                    }
                }

                // Получаем сервис платежей
                var paymentService = MauiProgram.Services?.GetService<IPaymentApiService>();
                if (paymentService == null)
                {
                    await DisplayAlert("Ошибка", "Сервис оплаты недоступен.", "OK");
                    return;
                }

                // --- ДОБАВЛЕНИЕ ЛОГИРОВАНИЯ ---
                System.Diagnostics.Debug.WriteLine($"DEBUG: Начинаем вызов PaymentAPI. Сумма: {amount}");

                // Создаем платеж через бэкенд Django
                // Индикатор загрузки будет показан на странице FinikQrPage
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var paymentResponse = await paymentService.CreatePaymentAsync(
                    amount,
                    cts.Token);

                // --- ДОБАВЛЕНИЕ ЛОГИРОВАНИЯ УСПЕХА ---
                System.Diagnostics.Debug.WriteLine($"DEBUG: Payment API УСПЕШНО вернул ответ. URL: {paymentResponse?.PaymentUrl ?? "null"}");

                // Проверяем, что получили paymentUrl
                if (string.IsNullOrWhiteSpace(paymentResponse?.PaymentUrl))
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG: PaymentUrl пустой в ответе от API");
                    await DisplayAlert("Ошибка", "Не удалось получить ссылку на оплату. Попробуйте снова.", "OK");
                    return;
                }

                var paymentUrl = paymentResponse.PaymentUrl;

                // Открываем URL напрямую в WebView (как на сайте)
                var paymentUrlEncoded = Uri.EscapeDataString(paymentUrl);
                var redirectUrlEncoded = !string.IsNullOrWhiteSpace(paymentResponse.RedirectUrl) 
                    ? Uri.EscapeDataString(paymentResponse.RedirectUrl) 
                    : string.Empty;
                
                System.Diagnostics.Debug.WriteLine($"DEBUG: Открываем URL в WebView: {paymentUrl}");
                
                // Используем FinikPaymentPage для отображения страницы оплаты в WebView
                var navigationPath = !string.IsNullOrWhiteSpace(redirectUrlEncoded)
                    ? $"FinikPaymentPage?paymentUrl={paymentUrlEncoded}&redirectUrl={redirectUrlEncoded}"
                    : $"FinikPaymentPage?paymentUrl={paymentUrlEncoded}";
                
                await Shell.Current.GoToAsync(navigationPath, animate: true);
            }
            catch (Exception ex)
            {
                // --- ДОБАВЛЕНИЕ ОБРАБОТКИ ОШИБОК ---
                System.Diagnostics.Debug.WriteLine($"!!! КРИТИЧЕСКАЯ ОШИБКА: {ex.Message} \nStack: {ex.StackTrace}");
                await DisplayAlert("Ошибка пополнения", $"Не удалось создать платёж. Детали: {ex.Message}", "OK");
            }
        }

        private decimal GetCheckedPresetAmount()
        {
            if (rb1000?.IsChecked == true) return 1000m;
            if (rb800?.IsChecked == true) return 800m;
            if (rb600?.IsChecked == true) return 600m;
            if (rb500?.IsChecked == true) return 500m;
            if (rb300?.IsChecked == true) return 300m;
            if (rb100?.IsChecked == true) return 100m;
            return 0m;
        }



    }
}
