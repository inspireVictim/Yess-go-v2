using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // MainThread
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YessGoFront.Models;
using YessGoFront.Services;

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

        // Баланс берём из общего BalanceStore
        public string Balance => BalanceStore.Instance.Balance.ToString("0.##");

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
            // Подписка на изменение баланса — обновляем метку на главной
            BalanceStore.Instance.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BalanceStore.Balance))
                    OnPropertyChanged(nameof(Balance));
            };

            LoadStories();
            LoadBanners();
            LoadTopCategories();
            LoadPartners();

            OpenStoryAsyncCommand = new AsyncRelayCommand<StoryModel?>(OpenStoryAsync);
            CloseStoryCommand = new RelayCommand(CloseStory);
            NextPageCommand = new RelayCommand(NextPage);
            PrevPageCommand = new RelayCommand(PrevPage);

            OpenBannerAsyncCommand = new AsyncRelayCommand<BannerModel?>(OpenBannerAsync);
            CloseBannerCommand = new RelayCommand(CloseBanner);
        }

        // ====== ДАННЫЕ ======
        private void LoadStories()
        {
            Stories.Clear();

            Stories.Add(new StoryModel
            {
                Title = "Бонусы",
                Icon = "sc_bonus.png",
                Pages = new() {
                    "https://picsum.photos/seed/bonus1/1200/2200",
                    "https://picsum.photos/seed/bonus2/1200/2200",
                    "https://picsum.photos/seed/bonus3/1200/2200",
                }
            });

            Stories.Add(new StoryModel
            {
                Title = "Йесскоины",
                Icon = "sc_coin.png",
                Pages = new() {
                    "https://picsum.photos/seed/coin1/1200/2200",
                    "https://picsum.photos/seed/coin2/1200/2200",
                }
            });

            Stories.Add(new StoryModel
            {
                Title = "Мы",
                Icon = "sc_we.png",
                Pages = new() {
                    "we_stories.png",
                }
            });

            Stories.Add(new StoryModel
            {
                Title = "Акции",
                Icon = "sc_sale.png",
                Pages = new() {
                    "sales_stories1.png",
                    "sales_stories2.png",
                    "sales_stories3.png",
                    "sales_stories4.png",
                }
            });

            Stories.Add(new StoryModel
            {
                Title = "Новости",
                Icon = "sc_news.png",
                Pages = new() {
                    "https://picsum.photos/seed/news1/1200/2200",
                    "https://picsum.photos/seed/news2/1200/2200",
                }
            });
        }

        private void LoadBanners()
        {
            Banners.Clear();
            Banners.Add(new BannerModel { Image = "banner_1.png", PartnerName = "Партнёр A" });
            Banners.Add(new BannerModel { Image = "banner_2.png", PartnerName = "Партнёр B" });
            Banners.Add(new BannerModel { Image = "banner_3.png", PartnerName = "Партнёр C" });
        }

        private void LoadTopCategories()
        {
            TopCategories.Clear();
            TopCategories.Add(new CategoryModel { Title = "Одежда и обувь", Icon = "cat_clothes.png" });
            TopCategories.Add(new CategoryModel { Title = "Для дома", Icon = "cat_home.png" });
            TopCategories.Add(new CategoryModel { Title = "Электроника", Icon = "cat_electronics.png" });
            TopCategories.Add(new CategoryModel { Title = "Здоровье", Icon = "cat_beauty.png" });
            TopCategories.Add(new CategoryModel { Title = "Детям", Icon = "cat_kids.png" });
        }

        private void LoadPartners()
        {
            PartnersRow1.Clear();
            PartnersRow2.Clear();
            PartnersRow3.Clear();

            var logos = new[]
            {
                "promzona.jpg","faiza.png","navat.png","flask.png","chickenstar.jpg",
                "bublik.png","sierra.jpg","ants.jpg","supara.png","teplo.png","savetheales.png"
            };

            foreach (var l in logos) PartnersRow1.Add(new PartnerLogoModel { Logo = l });
            foreach (var l in logos.Reverse()) PartnersRow2.Add(new PartnerLogoModel { Logo = l });
            foreach (var l in logos) PartnersRow3.Add(new PartnerLogoModel { Logo = l });

            // дублирование — для бесшовной ленты
            foreach (var l in logos) PartnersRow1.Add(new PartnerLogoModel { Logo = l });
            foreach (var l in logos.Reverse()) PartnersRow2.Add(new PartnerLogoModel { Logo = l });
            foreach (var l in logos) PartnersRow3.Add(new PartnerLogoModel { Logo = l });
        }

        // ====== СТОРИС: «как в инсте» ======

        public async Task OpenStoryAsync(StoryModel? story)
        {
            if (story == null) return;
            
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

                IsStoryOpen = true;

                for (int p = 0; p < pages.Count; p++)
                {
                    CurrentPageIndex = p;
                    UpdateCurrentPageImage();

                    await RunSmoothProgressAsync(p, ct);
                    if (ct.IsCancellationRequested) return;

                    PageProgressList[p] = 1.0;
                    OnPropertyChanged(nameof(PageProgressList));
                }
            }

            CloseStory();
        }

        private async Task RunSmoothProgressAsync(int segmentIndex, CancellationToken ct)
        {
            const int durationMs = 5500;
            var sw = Stopwatch.StartNew();

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

                await Task.Delay(16, ct); // ~60fps
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

        private Task PrefetchNextImage()
        {
            try
            {
                if (CurrentStory == null || CurrentStory.Pages == null) return Task.CompletedTask;
                var pages = CurrentStory.Pages;
                int next = CurrentPageIndex + 1;
                if (next >= 0 && next < pages.Count)
                {
                    var path = pages[next];

                    if (Uri.TryCreate(path, UriKind.Absolute, out var absUri)
                        && (absUri.Scheme == Uri.UriSchemeHttp || absUri.Scheme == Uri.UriSchemeHttps))
                    {
                        var _ = new UriImageSource
                        {
                            Uri = absUri,
                            CachingEnabled = true,
                            CacheValidity = TimeSpan.FromHours(3)
                        };
                    }
                }
            }
            catch { }
            return Task.CompletedTask;
        }

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

            IsStoryOpen = true;

            CurrentPageIndex = Math.Clamp(pageIndex, 0, pages.Count - 1);
            UpdateCurrentPageImage();

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
        public async Task OpenBannerAsync(BannerModel? banner)
        {
            if (banner == null) return;
            
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
