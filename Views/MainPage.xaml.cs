using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YessGoFront.ViewModels;
using YessGoFront.Views.Controls;

namespace YessGoFront.Views
{
    public partial class MainPage : ContentPage
    {
        private bool _isNavigating;
        private const string WalletRoute = "WalletPage";

        private bool _topIsA = true;                      // кто сверху: A или B
        private CancellationTokenSource? _swapCts;        // отмена прошлой анимации

        // Кэшируем найденные по имени элементы, чтобы не искать каждый раз
        private Image? _imgA;
        private Image? _imgB;

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

            // Подхватим ссылки на картинки для кросс-фейда (если ещё не кэшировали)
            _imgA ??= this.FindByName<Image>("StoryImageA");
            _imgB ??= this.FindByName<Image>("StoryImageB");

            if (BottomBar != null)
                BottomBar.SelectedTab = BottomTab.Home;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is MainPageViewModel vm)
                vm.PropertyChanged -= OnVmPropertyChanged;
        }

        private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainPageViewModel.CurrentPageImage))
                return;

            if (BindingContext is not MainPageViewModel vm)
                return;

            var nextSrc = vm.CurrentPageImage;
            if (string.IsNullOrWhiteSpace(nextSrc))
                return;

            // Перестрахуемся: вдруг XAML ещё не прогрузился
            _imgA ??= this.FindByName<Image>("StoryImageA");
            _imgB ??= this.FindByName<Image>("StoryImageB");
            if (_imgA is null || _imgB is null)
                return;

            // Отменяем прошлую анимацию, если была
            _swapCts?.Cancel();
            _swapCts = new CancellationTokenSource();
            var ct = _swapCts.Token;

            try
            {
                var top = _topIsA ? _imgA : _imgB;
                var bottom = _topIsA ? _imgB : _imgA;

                // Готовим нижнюю (будет всплывать)
                bottom.Opacity = 0;
                bottom.Source = null;
                bottom.Source = nextSrc;

                await Task.Delay(10, ct);              // даём кадр на раскладку/подкачку

                await bottom.FadeTo(1, 180, Easing.Linear);  // кросс-фейд

                _topIsA = !_topIsA;                   // теперь эта стала верхней

                // Старая "верхняя" уходит вниз и очищается
                top.Source = null;
                top.Opacity = 0;
            }
            catch (TaskCanceledException)
            {
                // пришло новое изображение быстрее — нормально
            }
            catch
            {
                // если элемент уже выгружен/пересоздан — просто игнорируем
            }
        }

        private async void OnWalletTapped(object? sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Shell.Current.GoToAsync(WalletRoute);
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}
