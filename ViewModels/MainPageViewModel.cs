using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui; // для MainThread
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YessGoFront.Models;

namespace YessGoFront.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        // ====== Коллекции ======
        public ObservableCollection<StoryModel> Stories { get; } = new();
        public ObservableCollection<BannerModel> Banners { get; } = new();
        public ObservableCollection<CategoryModel> TopCategories { get; } = new();

        public ObservableCollection<PartnerLogoModel> PartnersRow1 { get; } = new();
        public ObservableCollection<PartnerLogoModel> PartnersRow2 { get; } = new();
        public ObservableCollection<PartnerLogoModel> PartnersRow3 { get; } = new();

        // ====== Оверлеи / состояние сторис ======
        [ObservableProperty] private bool isStoryOpen;
        [ObservableProperty] private StoryModel? currentStory;

        [ObservableProperty] private bool isBannerOpen;
        [ObservableProperty] private BannerModel? currentBanner;

        // Индексы текущего сторис и страницы
        [ObservableProperty] private int currentStoryIndex = -1;
        [ObservableProperty] private int currentPageIndex = -1;

        // Текущее изображение страницы (безопасно для XAML)
        [ObservableProperty] private string? currentPageImage;

        // Прогресс текущей страницы (0..1) + список прогрессов сегментов
        [ObservableProperty] private double pageProgress; // 0..1
        public ObservableCollection<double> PageProgressList { get; } = new();

        public string Balance => "55.7";

        private CancellationTokenSource? _overlayCts;

        // ====== Команды ======
        public IAsyncRelayCommand<StoryModel> OpenStoryAsyncCommand { get; }
        public IRelayCommand CloseStoryCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PrevPageCommand { get; }

        public IAsyncRelayCommand<BannerModel> OpenBannerAsyncCommand { get; }
        public IRelayCommand CloseBannerCommand { get; }

        public MainPageViewModel()
        {
            LoadStories();
            LoadBanners();
            LoadTopCategories();
            LoadPartners();

            OpenStoryAsyncCommand = new AsyncRelayCommand<StoryModel>(OpenStoryAsync);
            CloseStoryCommand = new RelayCommand(CloseStory);
            NextPageCommand = new RelayCommand(NextPage);
            PrevPageCommand = new RelayCommand(PrevPage);

            OpenBannerAsyncCommand = new AsyncRelayCommand<BannerModel>(OpenBannerAsync);
            CloseBannerCommand = new RelayCommand(CloseBanner);
        }

        // ====== ДАННЫЕ ======
        private void LoadStories()
        {
            Stories.Clear();

            // Несколько страниц в каждом сторис (для теста — URL, потом заменишь на ассеты)
            Stories.Add(new StoryModel
            {
                Title = "Бонусы",
                Icon = "https://picsum.photos/seed/bonus/200/200",
                Pages = new() {
                    "https://picsum.photos/seed/bonus1/1200/2200",
                    "https://picsum.photos/seed/bonus2/1200/2200",
                    "https://picsum.photos/seed/bonus3/1200/2200",
                }
            });
            Stories.Add(new StoryModel
            {
                Title = "Йесскоины",
                Icon = "https://picsum.photos/seed/coin/200/200",
                Pages = new() {
                    "https://picsum.photos/seed/coin1/1200/2200",
                    "https://picsum.photos/seed/coin2/1200/2200",
                }
            });
            Stories.Add(new StoryModel
            {
                Title = "Мы",
                Icon = "https://picsum.photos/seed/we/200/200",
                Pages = new() {
                    "https://picsum.photos/seed/we1/1200/2200",
                }
            });
            Stories.Add(new StoryModel
            {
                Title = "Акции",
                Icon = "https://picsum.photos/seed/sale/200/200",
                Pages = new() {
                    "https://picsum.photos/seed/sale1/1200/2200",
                    "https://picsum.photos/seed/sale2/1200/2200",
                    "https://picsum.photos/seed/sale3/1200/2200",
                    "https://picsum.photos/seed/sale4/1200/2200",
                }
            });
            Stories.Add(new StoryModel
            {
                Title = "Новости",
                Icon = "https://picsum.photos/seed/news/200/200",
                Pages = new() {
                    "https://picsum.photos/seed/news1/1200/2200",
                    "https://picsum.photos/seed/news2/1200/2200",
                }
            });
        }

        private void LoadBanners()
        {
            Banners.Clear();
            Banners.Add(new BannerModel { Image = "https://picsum.photos/seed/banner1/1200/600", PartnerName = "Партнёр A" });
            Banners.Add(new BannerModel { Image = "https://picsum.photos/seed/banner2/1200/600", PartnerName = "Партнёр B" });
        }

        private void LoadTopCategories()
        {
            TopCategories.Clear();
            TopCategories.Add(new CategoryModel { Title = "Одежда и обувь", Icon = "https://picsum.photos/seed/cat1/200/200" });
            TopCategories.Add(new CategoryModel { Title = "Для дома", Icon = "https://picsum.photos/seed/cat2/200/200" });
            TopCategories.Add(new CategoryModel { Title = "Электроника", Icon = "https://picsum.photos/seed/cat3/200/200" });
            TopCategories.Add(new CategoryModel { Title = "Здоровье", Icon = "https://picsum.photos/seed/cat4/200/200" });
            TopCategories.Add(new CategoryModel { Title = "Детям", Icon = "https://picsum.photos/seed/cat5/200/200" });
        }

        private void LoadPartners()
        {
            PartnersRow1.Clear();
            PartnersRow2.Clear();
            PartnersRow3.Clear();

            var logos = new[]
            {
                "https://picsum.photos/seed/p1/200/200",
                "https://picsum.photos/seed/p2/200/200",
                "https://picsum.photos/seed/p3/200/200",
                "https://picsum.photos/seed/p4/200/200",
                "https://picsum.photos/seed/p5/200/200",
                "https://picsum.photos/seed/p6/200/200"
            };

            foreach (var l in logos) PartnersRow1.Add(new PartnerLogoModel { Logo = l });
            foreach (var l in logos.Reverse()) PartnersRow2.Add(new PartnerLogoModel { Logo = l });
            foreach (var l in logos) PartnersRow3.Add(new PartnerLogoModel { Logo = l });

            foreach (var l in logos) PartnersRow1.Add(new PartnerLogoModel { Logo = l });
            foreach (var l in logos.Reverse()) PartnersRow2.Add(new PartnerLogoModel { Logo = l });
            foreach (var l in logos) PartnersRow3.Add(new PartnerLogoModel { Logo = l });
        }

        // ====== СТОРИС: «как в инсте» ======

        public async Task OpenStoryAsync(StoryModel story)
        {
            _overlayCts?.Cancel();
            _overlayCts = new CancellationTokenSource();

            CurrentStoryIndex = Math.Max(0, Stories.IndexOf(story));
            await PlayFromStoryIndexAsync(CurrentStoryIndex, _overlayCts.Token);
        }

        private async Task PlayFromStoryIndexAsync(int storyIndex, CancellationToken ct)
        {
            if (storyIndex < 0 || storyIndex >= Stories.Count) return;

            for (int s = storyIndex; s < Stories.Count; s++)
            {
                CurrentStoryIndex = s;
                CurrentStory = Stories[s];

                var pages = CurrentStory.Pages ?? new();
                if (pages.Count == 0) continue;

                PrepareSegments(pages.Count);

                for (int p = 0; p < pages.Count; p++)
                {
                    // Инициализация до показа
                    CurrentPageIndex = p;
                    UpdateCurrentPageImage(); // выставить картинку заранее

                    IsStoryOpen = true;
                    await RunSmoothProgressAsync(p, ct); // плавная анимация прогресса

                    if (ct.IsCancellationRequested) return;

                    // зафиксировать сегмент = 100%
                    PageProgressList[p] = 1.0;
                    OnPropertyChanged(nameof(PageProgressList));
                }
            }

            CloseStory();
        }

        // Плавная анимация прогресса, как в инсте (линейно ~5.5 сек)
        private async Task RunSmoothProgressAsync(int segmentIndex, CancellationToken ct)
        {
            const int durationMs = 5500; // около 5.5с
            var sw = Stopwatch.StartNew();

            // Предзагрузка следующей страницы в фоне (если есть)
            _ = PrefetchNextImage();

            while (sw.ElapsedMilliseconds < durationMs && !ct.IsCancellationRequested)
            {
                double prog = Math.Clamp(sw.Elapsed.TotalMilliseconds / durationMs, 0, 1);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    PageProgress = prog;
                    PageProgressList[segmentIndex] = prog;
                    OnPropertyChanged(nameof(PageProgressList));
                });

                // 60fps ~ 16мс
                await Task.Delay(16, ct);
            }

            sw.Stop();
            if (!ct.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    PageProgress = 1.0;
                    PageProgressList[segmentIndex] = 1.0;
                    OnPropertyChanged(nameof(PageProgressList));
                });
            }
        }

        private void PrepareSegments(int pagesCount)
        {
            PageProgressList.Clear();
            for (int i = 0; i < pagesCount; i++) PageProgressList.Add(0.0);
            PageProgress = 0;
            OnPropertyChanged(nameof(PageProgressList));
        }

        private void UpdateCurrentPageImage()
        {
            string? img = null;
            if (CurrentStory != null &&
                CurrentStory.Pages != null &&
                CurrentPageIndex >= 0 &&
                CurrentPageIndex < CurrentStory.Pages.Count)
            {
                img = CurrentStory.Pages[CurrentPageIndex];
            }
            CurrentPageImage = img;
        }

        // Предзагрузка следующего изображения (простая warmed-cache логика для UriImageSource)
        private Task PrefetchNextImage()
        {
            try
            {
                if (CurrentStory == null || CurrentStory.Pages == null) return Task.CompletedTask;
                var pages = CurrentStory.Pages;
                int next = CurrentPageIndex + 1;
                if (next >= 0 && next < pages.Count)
                {
                    var uri = pages[next];
                    // «Коснуться» UriImageSource, чтобы MAUI прогрел кэш
                    var src = new UriImageSource { Uri = new Uri(uri), CachingEnabled = true, CacheValidity = TimeSpan.FromHours(3) };
                    // Ничего не делаем с результатом — MAUI сам прогреет картинку
                }
            }
            catch { }
            return Task.CompletedTask;
        }

        // вправо (следующая страница / следующий сторис)
        private void NextPage()
        {
            if (!IsStoryOpen || CurrentStory == null) return;

            _overlayCts?.Cancel();

            var pages = CurrentStory.Pages ?? new();
            if (CurrentPageIndex + 1 < pages.Count)
            {
                _ = ResumeFrom(CurrentStoryIndex, CurrentPageIndex + 1);
            }
            else
            {
                _ = ResumeFrom(CurrentStoryIndex + 1, 0);
            }
        }

        // влево (предыдущая страница / предыдущий сторис)
        private void PrevPage()
        {
            if (!IsStoryOpen) return;

            _overlayCts?.Cancel();

            if (CurrentStory != null && CurrentPageIndex - 1 >= 0)
            {
                _ = ResumeFrom(CurrentStoryIndex, CurrentPageIndex - 1);
            }
            else
            {
                int prevStory = CurrentStoryIndex - 1;
                if (prevStory >= 0)
                {
                    var prevPages = Stories[prevStory].Pages ?? new();
                    int lastPage = Math.Max(0, prevPages.Count - 1);
                    _ = ResumeFrom(prevStory, lastPage);
                }
                else
                {
                    _ = ResumeFrom(0, 0);
                }
            }
        }

        private async Task ResumeFrom(int storyIndex, int pageIndex)
        {
            _overlayCts = new CancellationTokenSource();

            CurrentStoryIndex = Math.Clamp(storyIndex, 0, Stories.Count - 1);
            CurrentStory = Stories[CurrentStoryIndex];

            var pages = CurrentStory.Pages ?? new();
            if (pages.Count == 0) { CloseStory(); return; }

            PrepareSegments(pages.Count);
            for (int i = 0; i < pages.Count; i++)
                PageProgressList[i] = i < pageIndex ? 1.0 : 0.0;
            OnPropertyChanged(nameof(PageProgressList));

            CurrentPageIndex = Math.Clamp(pageIndex, 0, pages.Count - 1);
            UpdateCurrentPageImage();
            IsStoryOpen = true;

            for (int p = CurrentPageIndex; p < pages.Count; p++)
            {
                CurrentPageIndex = p;
                UpdateCurrentPageImage();

                await RunSmoothProgressAsync(p, _overlayCts.Token);
                if (_overlayCts.IsCancellationRequested) return;

                PageProgressList[p] = 1.0;
                OnPropertyChanged(nameof(PageProgressList));
            }

            int nextStory = CurrentStoryIndex + 1;
            if (nextStory < Stories.Count)
            {
                await PlayFromStoryIndexAsync(nextStory, _overlayCts.Token);
            }
            else
            {
                CloseStory();
            }
        }

        public void CloseStory()
        {
            _overlayCts?.Cancel();
            IsStoryOpen = false;
            CurrentStory = null;
            CurrentStoryIndex = -1;
            CurrentPageIndex = -1;
            CurrentPageImage = null;
            PageProgress = 0;
            PageProgressList.Clear();
            OnPropertyChanged(nameof(PageProgressList));
        }

        // ====== Баннеры ======
        public async Task OpenBannerAsync(BannerModel banner)
        {
            _overlayCts?.Cancel();
            _overlayCts = new CancellationTokenSource();

            CurrentBanner = banner;
            IsBannerOpen = true;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(25), _overlayCts.Token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                if (!_overlayCts.IsCancellationRequested)
                    IsBannerOpen = false;
            }
        }

        public void CloseBanner()
        {
            _overlayCts?.Cancel();
            IsBannerOpen = false;
            CurrentBanner = null;
        }
    }
}
