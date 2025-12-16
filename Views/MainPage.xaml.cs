 using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.ApplicationModel;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;
using YessGoFront.Views.Controls;
using YessGoFront.Models;

namespace YessGoFront.Views
{
    public partial class MainPage : ContentPage
    {
        // ============================
        // Навигация
        // ============================
        private bool _isNavigating;
        private bool _isRefreshingUser;
        private bool _isAppearing = false; // Защита от повторных вызовов OnAppearing
        private readonly SemaphoreSlim _actionLock = new(1, 1); // Защита от повторных нажатий
        private const string WalletRoute = "///wallet";
        private const string TransactionsRoute = "///TransactionsPage";

        // ============================
        // Story Crossfade
        // ============================
        private bool _topIsA = true;
        private CancellationTokenSource? _swapCts;
        private Image? _imgA;
        private Image? _imgB;

        // ============================
        // Автоскролл партнёров
        // ============================
        private CancellationTokenSource? _autoScrollCts;
        private DateTime _lastTouch = DateTime.Now;
        private const int IdleSeconds = 5;

        // Скорости рядов
        private const double SpeedRow1 = 20;   // вправо
        private const double SpeedRow2 = -20;  // влево
        private const double SpeedRow3 = 20;   // вправо

        private ScrollView? _row1;
        private ScrollView? _row2;
        private ScrollView? _row3;

        private bool _row1Ready;
        private bool _row2Ready;
        private bool _row3Ready;


        // ============================
        // Конструктор
        // ============================
        public MainPage()
        {
            InitializeComponent();

            // DI
            var bannerApiService = MauiProgram.Services.GetService<IBannerApiService>();
            var partnersApiService = MauiProgram.Services.GetService<IPartnersApiService>();
            var walletService = MauiProgram.Services.GetService<IWalletService>();
            var authService = MauiProgram.Services.GetService<IAuthService>();
            var authenticationService = MauiProgram.Services.GetService<Infrastructure.Auth.IAuthenticationService>();

            BindingContext = new MainPageViewModel(bannerApiService, partnersApiService, walletService, authService, authenticationService);

            BindingContextChanged += (_, __) =>
            {
                if (BindingContext is MainPageViewModel vm)
                {
                    vm.PropertyChanged -= OnVmPropertyChanged;
                    vm.PropertyChanged += OnVmPropertyChanged;
                }
            };
        }


        // ============================
        // OnAppearing
        // ============================
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
                Debug.WriteLine($"[MainPage] Error in OnAppearing: {ex.Message}");
            }
            finally
            {
                _isAppearing = false;
            }
        }

        protected virtual async Task OnAppearingAsync()
        {
            _imgA ??= FindByName("StoryImageA") as Image;
            _imgB ??= FindByName("StoryImageB") as Image;

            _row1 ??= FindByName("Row1") as ScrollView;
            _row2 ??= FindByName("Row2") as ScrollView;
            _row3 ??= FindByName("Row3") as ScrollView;

            // Фикс готовности рядов
            HookSizeReady(_row1, r => _row1Ready = r);
            HookSizeReady(_row2, r => _row2Ready = r);
            HookSizeReady(_row3, r => _row3Ready = r);

            HookPartnerRows();

            // ДОП. ФИКС — ждём загрузки контента (BindLayout) в фоне (контролируемая задача)
            // Отменяем предыдущую задачу
            _autoScrollCts?.Cancel();
            _autoScrollCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    // Убрана задержка Task.Delay(300) для улучшения UX
                    await MainThread.InvokeOnMainThreadAsync(() => StartSmoothAutoScroll());
                }
                catch (OperationCanceledException) { /* Игнорируем отмену */ }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MainPage] Error in auto scroll task: {ex.Message}");
                }
            }, _autoScrollCts.Token);

            // Navbar
            if (BottomBar != null)
                BottomBar.UpdateSelectedTab("Home");

            // Story timeline grid width
            if (BindingContext is MainPageViewModel viewModel)
            {
                var progressContainer = FindByName("ProgressTimelineContainer") as Grid;
                if (progressContainer != null)
                {
                    progressContainer.SizeChanged += OnProgressTimelineContainerSizeChanged;

                    if (progressContainer.Width > 0)
                        viewModel.ProgressTimelineContainerWidth = progressContainer.Width;
                }

                // Оптимизация: загружаем данные пользователя и баланс параллельно в фоне
                // Ограничиваем частоту вызовов, чтобы не перегружать сеть
                if (!_isRefreshingUser)
                {
                    _isRefreshingUser = true;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            Debug.WriteLine("[MainPage] OnAppearing: Refreshing user data and balance in parallel...");
                            
                            // Используем таймаут для обновления (15 секунд)
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                            
                            // Загружаем баланс и пользователя параллельно (упрощено: убраны вложенные Task.Run)
                            await Task.WhenAll(
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await viewModel.RefreshBalanceAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[MainPage] OnAppearing: Error refreshing balance: {ex.Message}");
                                    }
                                }, cts.Token),
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        await viewModel.RefreshUserAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[MainPage] OnAppearing: Error refreshing user data: {ex.Message}");
                                    }
                                }, cts.Token)
                            );

                            Debug.WriteLine($"[MainPage] OnAppearing: User data and balance refreshed - DisplayName={viewModel.DisplayName}, Phone={viewModel.Phone}, Balance={viewModel.Balance}");
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("[MainPage] OnAppearing: Refresh operations timed out");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MainPage] OnAppearing: Unexpected error in background refresh: {ex.Message}");
                        }
                        finally
                        {
                            _isRefreshingUser = false;
                        }
                    });
                }
            }
        }


        // ============================
        // OnDisappearing
        // ============================
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Отменяем незавершенные задачи при навигации для оптимизации памяти
            if (BindingContext is MainPageViewModel viewModel)
            {
                // Отменяем загрузку партнеров, если она выполняется
                viewModel.CancelPartnersLoading();
            }

            UnhookPartnerRows();
            StopSmoothAutoScroll();

            var vm = BindingContext as MainPageViewModel;
            if (vm != null)
                vm.PropertyChanged -= OnVmPropertyChanged;

            var progressContainer = FindByName("ProgressTimelineContainer") as Grid;
            if (progressContainer != null)
                progressContainer.SizeChanged -= OnProgressTimelineContainerSizeChanged;
        }


        // ============================
        // Info buttons size fix
        // ============================
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            var infoButtons = FindByName("InfoButtonsCollection") as CollectionView;
            if (infoButtons != null)
                infoButtons.HeightRequest = 90;
        }


        private void OnProgressTimelineContainerSizeChanged(object? sender, EventArgs e)
        {
            if (sender is Grid container && BindingContext is MainPageViewModel vm)
            {
                if (container.Width > 0)
                    vm.ProgressTimelineContainerWidth = container.Width;
            }
        }


        // ============================
        // Story crossfade
        // ============================
        private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                await OnVmPropertyChangedAsync(sender, e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error in OnVmPropertyChanged: {ex.Message}");
            }
        }

        private async Task OnVmPropertyChangedAsync(object? sender, PropertyChangedEventArgs e)
        {
            if (BindingContext is not MainPageViewModel vm)
                return;

            if (e.PropertyName == nameof(MainPageViewModel.IsStoryOpen))
            {
                if (vm.IsStoryOpen)
                {
                    await Task.Delay(50);
                    var progressContainer = FindByName("ProgressTimelineContainer") as Grid;
                    if (progressContainer != null && progressContainer.Width > 0)
                        vm.ProgressTimelineContainerWidth = progressContainer.Width;
                }
                return;
            }

            if (e.PropertyName != nameof(MainPageViewModel.CurrentPageImage))
                return;

            var nextSrc = vm.CurrentPageImage;
            if (string.IsNullOrWhiteSpace(nextSrc))
                return;

            _imgA ??= FindByName("StoryImageA") as Image;
            _imgB ??= FindByName("StoryImageB") as Image;

            if (_imgA == null || _imgB == null)
                return;

            _swapCts?.Cancel();
            _swapCts = new CancellationTokenSource();
            var ct = _swapCts.Token;

            try
            {
                // ВАЖНО: Убрали вложенный async для оптимизации
                // Используем BeginInvokeOnMainThread и выполняем анимации напрямую
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var top = _topIsA ? _imgA : _imgB;
                    var bottom = _topIsA ? _imgB : _imgA;

                    bottom.Opacity = 0;
                    bottom.Source = nextSrc;

                    await Task.Delay(50, ct);
                    await bottom.FadeTo(1, 250, Easing.Linear);

                    _topIsA = !_topIsA;

                    top.Source = null;
                    top.Opacity = 0;
                });
            }
            catch (OperationCanceledException)
            {
                // Игнорируем отмену операции
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] OnVmPropertyChanged: Error updating story image: {ex.Message}");
            }
        }


        // ============================
        // Готовность рядов
        // ============================
        private void HookSizeReady(ScrollView? row, Action<bool> setReady)
        {
            if (row == null)
            {
                setReady(false);
                return;
            }

            setReady(IsRowReady(row));
            row.SizeChanged += (_, __) => setReady(IsRowReady(row));

            void Once(object? s, ScrolledEventArgs e)
            {
                setReady(IsRowReady(row));
                row.Scrolled -= Once;
            }

            row.Scrolled += Once;
        }

        private static bool IsRowReady(ScrollView row)
        {
            return row.ContentSize.Width > row.Width + 20;
        }


        // ============================
        // Обработчики скрола
        // ============================
        private void HookPartnerRows()
        {
            Attach(_row1);
            Attach(_row2);
            Attach(_row3);

            void Attach(ScrollView? sv)
            {
                if (sv == null) return;

                sv.Scrolled += OnAnyRowScrolled;
            }
        }

        private void UnhookPartnerRows()
        {
            Detach(_row1);
            Detach(_row2);
            Detach(_row3);

            void Detach(ScrollView? sv)
            {
                if (sv == null) return;
                sv.Scrolled -= OnAnyRowScrolled;
            }
        }

        private void OnAnyRowScrolled(object? sender, ScrolledEventArgs e)
        {
            _lastTouch = DateTime.Now;

            if (sender is ScrollView sv)
                SeamlessWrap(sv);
        }


        // ============================
        // Автоскролл
        // ============================
        private void StartSmoothAutoScroll()
        {
            StopSmoothAutoScroll();
            _autoScrollCts = new CancellationTokenSource();
            _ = RunAutoScrollAsync(_autoScrollCts.Token);
        }

        private void StopSmoothAutoScroll()
        {
            _autoScrollCts?.Cancel();
            _autoScrollCts = null;
        }

        private async Task RunAutoScrollAsync(CancellationToken token)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            while (!token.IsCancellationRequested)
            {
                if ((DateTime.Now - _lastTouch).TotalSeconds >= IdleSeconds)
                {
                    double dt = sw.Elapsed.TotalSeconds;
                    sw.Restart();

                    if (_row1Ready) await StepSmoothScroll(_row1, SpeedRow1 * dt);
                    if (_row2Ready) await StepSmoothScroll(_row2, SpeedRow2 * dt);
                    if (_row3Ready) await StepSmoothScroll(_row3, SpeedRow3 * dt);
                }
                else
                {
                    sw.Restart();
                }

                await Task.Delay(16, token);
            }
        }

        private async Task StepSmoothScroll(ScrollView? sv, double delta)
        {
            if (sv == null) return;

            double contentWidth = sv.ContentSize.Width;
            double viewportWidth = sv.Width;

            if (contentWidth <= viewportWidth)
                return;

            double newX = sv.ScrollX + delta;

            double half = contentWidth / 2;

            if (newX > half)
                newX -= half;
            else if (newX < 0)
                newX += half;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    sv.ScrollToAsync(newX, 0, false));
            }
            catch { }
        }

        private void SeamlessWrap(ScrollView sv)
        {
            double contentWidth = sv.ContentSize.Width;
            double viewportWidth = sv.Width;

            if (contentWidth <= viewportWidth)
                return;

            double half = contentWidth / 2;
            double x = sv.ScrollX;

            if (x > half + 2)
                _ = sv.ScrollToAsync(x - half, 0, false);
            else if (x < -2)
                _ = sv.ScrollToAsync(x + half, 0, false);
        }


        // ============================
        // Навигация
        // ============================
        private async void OnWalletTapped(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnWalletTappedAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error in OnWalletTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnWalletTappedAsync()
        {
            if (_isNavigating) return;

            _isNavigating = true;
            try
            {
                await Shell.Current.GoToAsync(WalletRoute);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] OnWalletTapped: Error navigating: {ex.Message}");
                try 
                { 
                    await Shell.Current.GoToAsync("//wallet"); 
                } 
                catch (Exception ex2)
                {
                    Debug.WriteLine($"[MainPage] OnWalletTapped: Fallback navigation failed: {ex2.Message}");
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnHistoryClicked(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnHistoryClickedAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error in OnHistoryClicked: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnHistoryClickedAsync()
        {
            if (_isNavigating) return;
            _isNavigating = true;

            try
            {
                await Shell.Current.GoToAsync(TransactionsRoute);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] OnHistoryClicked: Error navigating: {ex.Message}");
                try 
                { 
                    await Shell.Current.GoToAsync("//TransactionsPage"); 
                } 
                catch (Exception ex2)
                {
                    Debug.WriteLine($"[MainPage] OnHistoryClicked: Fallback navigation failed: {ex2.Message}");
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnPayYessCoinClicked(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnPayYessCoinClickedAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error in OnPayYessCoinClicked: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnPayYessCoinClickedAsync()
        {
            if (_isNavigating) return;
            _isNavigating = true;

            try
            {
                Debug.WriteLine("[MainPage] Navigating to PayPage");
                await Shell.Current.GoToAsync("PayPage", animate: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error navigating to PayPage: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnProfileTapped(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnProfileTappedAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error in OnProfileTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnProfileTappedAsync()
        {
            if (_isNavigating) return;
            _isNavigating = true;

            try
            {
                await Shell.Current.GoToAsync(nameof(Profile), animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Ошибка навигации к Profile: {ex.Message}");
                try
                {
                    await Shell.Current.GoToAsync("Profile", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }


        private async void OnMoreTapped(object sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnMoreTappedAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error in OnMoreTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnMoreTappedAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//main/partner");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] OnMoreTapped: Error navigating: {ex.Message}");
            }
        }


        private void OnPartnerTapped(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainPage] ===== OnPartnerTapped EVENT FIRED =====");
            
            if (sender is Grid grid && grid.BindingContext is PartnerLogoModel partner)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] OnPartnerTapped: Partner Name='{partner.Name}', ID='{partner.Id}'");
                
                // Вызываем команду из ViewModel
                if (BindingContext is MainPageViewModel vm)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPage] Calling OpenPartnerAsyncCommand from ViewModel");
                    vm.OpenPartnerAsyncCommand.Execute(partner);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainPage] ERROR: BindingContext is not MainPageViewModel");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] OnPartnerTapped: sender type={sender?.GetType()?.Name}, BindingContext type={((sender as Grid)?.BindingContext)?.GetType()?.Name}");
            }
        }

        private async void OnCategoryTapped(object? sender, EventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnCategoryTappedAsync(sender);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error in OnCategoryTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnCategoryTappedAsync(object? sender)
        {
            if (_isNavigating) return;

            _isNavigating = true;

            try
            {
                string? slug = null;
                string? name = null;

                if (sender is Frame frame)
                {
                    var tap = frame.GestureRecognizers
                        .OfType<TapGestureRecognizer>()
                        .FirstOrDefault();

                    if (tap?.CommandParameter is string param)
                    {
                        var map = new Dictionary<string, (string slug, string name)>
                        {
                            { "beauty", ("beauty", "Салоны красоты") },
                            { "pharmacy", ("pharmacy", "Аптеки") },
                            { "groceries", ("groceries", "Магазины") }
                        };

                        if (map.TryGetValue(param, out var data))
                        {
                            slug = data.slug;
                            name = data.name;
                        }
                    }
                }

                if (slug == null)
                {
                    await Shell.Current.GoToAsync("///PartnersListPage");
                }
                else
                {
                    var route =
                        $"///PartnersListPage?categorySlug={Uri.EscapeDataString(slug)}&categoryName={Uri.EscapeDataString(name!)}";

                    await Shell.Current.GoToAsync(route);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] OnCategoryTapped: Error navigating: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        // ============================
        // Story жесты
        // ============================
        private DateTime _storyPanStartTime;
        private bool _storyIsHolding;
        private Point _storyPanStartPoint;
        private const double HoldThresholdMs = 150; // Порог для определения удержания (150мс)
        private const double HoldMaxDistance = 15; // Максимальное расстояние для удержания (пиксели)

        private void OnStoryPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            if (BindingContext is not MainPageViewModel vm || !vm.IsStoryOpen)
                return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _storyPanStartTime = DateTime.Now;
                    _storyPanStartPoint = new Point(e.TotalX, e.TotalY);
                    _storyIsHolding = false;
                    // НЕ паузим сразу - ждем, чтобы отличить тап от удержания
                    break;

                case GestureStatus.Running:
                    // Вычисляем расстояние от начальной точки
                    var distance = Math.Sqrt(Math.Pow(e.TotalX - _storyPanStartPoint.X, 2) + 
                                            Math.Pow(e.TotalY - _storyPanStartPoint.Y, 2));
                    var elapsed = (DateTime.Now - _storyPanStartTime).TotalMilliseconds;
                    
                    // Если прошло достаточно времени и движение минимальное - это удержание
                    if (elapsed > HoldThresholdMs && distance < HoldMaxDistance && !_storyIsHolding)
                    {
                        _storyIsHolding = true;
                        vm.PauseStory();
                        System.Diagnostics.Debug.WriteLine("[MainPage] Story holding detected - paused");
                    }
                    // Если движение значительное - возобновляем (если была пауза)
                    else if (distance > HoldMaxDistance * 2 && _storyIsHolding)
                    {
                        _storyIsHolding = false;
                        vm.ResumeStory();
                        System.Diagnostics.Debug.WriteLine("[MainPage] Story movement detected - resumed");
                    }
                    break;

                case GestureStatus.Canceled:
                case GestureStatus.Completed:
                    // Возобновляем при отпускании, только если была пауза
                    if (_storyIsHolding && vm.IsStoryPaused)
                    {
                        vm.ResumeStory();
                        System.Diagnostics.Debug.WriteLine("[MainPage] Story released - resumed");
                    }
                    _storyIsHolding = false;
                    break;
            }
        }

        private void OnStoryLeftTapped(object? sender, EventArgs e)
        {
            if (BindingContext is MainPageViewModel vm)
            {
                vm.PrevPageCommand.Execute(null);
            }
        }

        private void OnStoryRightTapped(object? sender, EventArgs e)
        {
            if (BindingContext is MainPageViewModel vm)
            {
                vm.NextPageCommand.Execute(null);
            }
        }

        // ============================
        // Info Button обработчик
        // ============================
        private async void OnInfoButtonTapped(object? sender, TappedEventArgs e)
        {
            // Защита от повторных нажатий
            if (!await _actionLock.WaitAsync(0))
                return; // Уже обрабатывается

            try
            {
                // Отключаем кнопку визуально
                if (sender is VisualElement element)
                    element.IsEnabled = false;

                await OnInfoButtonTappedAsync(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] Error in OnInfoButtonTapped: {ex.Message}");
            }
            finally
            {
                if (sender is VisualElement element)
                    element.IsEnabled = true;
                _actionLock.Release();
            }
        }

        private async Task OnInfoButtonTappedAsync(TappedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] ===== OnInfoButtonTapped EVENT FIRED =====");
                
                if (e.Parameter is InfoButtonModel infoButton)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] InfoButton tapped: Title='{infoButton.Title}', ActionType='{infoButton.ActionType}'");
                    
                    if (BindingContext is MainPageViewModel vm)
                    {
                        System.Diagnostics.Debug.WriteLine("[MainPage] Calling OpenInfoButtonAsyncCommand from ViewModel");
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        await vm.OpenInfoButtonAsyncCommand.ExecuteAsync(infoButton);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[MainPage] ERROR: BindingContext is not MainPageViewModel");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] OnInfoButtonTapped: Parameter type={e.Parameter?.GetType()?.Name ?? "NULL"}");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[MainPage] OnInfoButtonTapped: Operation timed out");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] OnInfoButtonTapped: Error: {ex.Message}");
            }
        }
    }
}
