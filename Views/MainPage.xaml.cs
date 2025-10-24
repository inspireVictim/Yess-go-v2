using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using YessGoFront.ViewModels;
using YessGoFront.Views.Controls;

namespace YessGoFront.Views
{
    public partial class MainPage : ContentPage
    {
        // ===== Навигация в кошелёк =====
        private bool _isNavigating;
        private const string WalletRoute = "///wallet";

        // ===== Кросс-фейд сторис =====
        private bool _topIsA = true;
        private CancellationTokenSource? _swapCts;
        private Image? _imgA;
        private Image? _imgB;

        // ===== Автопрокрутка партнёров =====
        private CancellationTokenSource? _autoScrollCts;
        private DateTime _lastTouch = DateTime.Now;
        private const int IdleSeconds = 5;

        // скорость пикселей в секунду
        private const double SpeedRow1 = 20; // вправо
        private const double SpeedRow2 = -15; // влево
        private const double SpeedRow3 = 18; // вправо

        private ScrollView? _row1;
        private ScrollView? _row2;
        private ScrollView? _row3;

        private bool _row1Ready, _row2Ready, _row3Ready;

        public MainPage()
        {
            InitializeComponent();

            BindingContextChanged += (_, __) =>
            {
                if (BindingContext is MainPageViewModel vm)
                {
                    vm.PropertyChanged -= OnVmPropertyChanged;
                    vm.PropertyChanged += OnVmPropertyChanged;
                }
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _imgA ??= this.FindByName<Image>("StoryImageA");
            _imgB ??= this.FindByName<Image>("StoryImageB");

            _row1 ??= this.FindByName<ScrollView>("Row1");
            _row2 ??= this.FindByName<ScrollView>("Row2");
            _row3 ??= this.FindByName<ScrollView>("Row3");

            HookSizeReady(_row1, r => _row1Ready = r);
            HookSizeReady(_row2, r => _row2Ready = r);
            HookSizeReady(_row3, r => _row3Ready = r);

            HookPartnerRows();
            StartSmoothAutoScroll();

            if (BottomBar != null)
                BottomBar.SelectedTab = BottomTab.Home;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            UnhookPartnerRows();
            StopSmoothAutoScroll();

            if (BindingContext is MainPageViewModel vm)
                vm.PropertyChanged -= OnVmPropertyChanged;
        }

        // ======== Кросс-фейд сторис ========
        private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainPageViewModel.CurrentPageImage))
                return;

            if (BindingContext is not MainPageViewModel vm)
                return;

            var nextSrc = vm.CurrentPageImage;
            if (string.IsNullOrWhiteSpace(nextSrc))
                return;

            _imgA ??= this.FindByName<Image>("StoryImageA");
            _imgB ??= this.FindByName<Image>("StoryImageB");
            if (_imgA is null || _imgB is null)
                return;

            _swapCts?.Cancel();
            _swapCts = new CancellationTokenSource();
            var ct = _swapCts.Token;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var top = _topIsA ? _imgA : _imgB;
                    var bottom = _topIsA ? _imgB : _imgA;

                    bottom.Opacity = 0;
                    bottom.Source = null;
                    bottom.Source = nextSrc;

                    await Task.Delay(50, ct);
                    await bottom.FadeTo(1, 250, Easing.Linear);

                    _topIsA = !_topIsA;

                    top.Source = null;
                    top.Opacity = 0;
                });
            }
            catch (TaskCanceledException)
            {
                // новый кадр — игнорируем
            }
            catch
            {
                // безопасно игнорировать
            }
        }

        // ======== Размер рядов ========
        private void HookSizeReady(ScrollView? row, Action<bool> setReady)
        {
            if (row == null) { setReady(false); return; }

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
            => row.ContentSize.Width > Math.Max(0, row.Width) + 1;

        // ======== Пользователь взаимодействует ========
        private void HookPartnerRows()
        {
            Attach(_row1);
            Attach(_row2);
            Attach(_row3);

            void Attach(ScrollView? sv)
            {
                if (sv == null) return;
                sv.Scrolled += OnAnyRowScrolled;

                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, __) => _lastTouch = DateTime.Now;
                sv.GestureRecognizers.Add(tap);
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

        // ======== Плавная авто-анимация ========
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
                // ждём 5 секунд простоя
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

                await Task.Delay(16, token); // ~60fps
            }
        }

        private async Task StepSmoothScroll(ScrollView? sv, double delta)
        {
            if (sv == null) return;

            double contentWidth = sv.ContentSize.Width;
            double viewport = sv.Width;
            if (contentWidth <= viewport || contentWidth <= 0)
                return;

            double newX = sv.ScrollX + delta;

            if (newX > contentWidth / 2)
                newX -= contentWidth / 2;
            else if (newX < 0)
                newX += contentWidth / 2;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    sv.ScrollToAsync(newX, 0, false));
            }
            catch { /* безопасно игнорируем */ }
        }

        private void SeamlessWrap(ScrollView sv)
        {
            double contentWidth = sv.ContentSize.Width;
            double viewport = sv.Width;
            if (contentWidth <= viewport || contentWidth <= 0)
                return;

            double half = contentWidth / 2.0;
            double x = sv.ScrollX;

            if (x > half + 2)
                _ = sv.ScrollToAsync(x - half, 0, false);
            else if (x < -2)
                _ = sv.ScrollToAsync(x + half, 0, false);
        }

        // ======== Навигация в кошелёк ========
        private async void OnWalletTapped(object? sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Shell.Current.GoToAsync(WalletRoute);
            }
            catch
            {
                try { await Shell.Current.GoToAsync("//wallet"); } catch { }
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}
