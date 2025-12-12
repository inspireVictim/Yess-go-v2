using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // MainThread
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using YessGoFront.Models;
using YessGoFront.Services;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;
using System.Collections.Generic;
using YessGoFront.Infrastructure.Auth;
using YessGoFront.Infrastructure;

namespace YessGoFront.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        // ====== Коллекции ======
        public ObservableCollection<StoryModel> Stories { get; } = new();
        public ObservableCollection<InfoButtonModel> InfoButtons { get; } = new();
        public ObservableCollection<CategoryModel> TopCategories { get; } = new();

        public ObservableCollection<PartnerLogoModel> PartnersRow1 { get; } = new();
        public ObservableCollection<PartnerLogoModel> PartnersRow2 { get; } = new();
        public ObservableCollection<PartnerLogoModel> PartnersRow3 { get; } = new();

        // ====== Оверлеи / состояние сторис ======
        [ObservableProperty] private bool isStoryOpen;
        [ObservableProperty] private StoryModel? currentStory;


        // Индексы текущего сторис и страницы
        [ObservableProperty] private int currentStoryIndex = -1;
        [ObservableProperty] private int currentPageIndex = -1;

        // Текущее изображение страницы (безопасно для XAML)
        [ObservableProperty] private string? currentPageImage;

        // Прогресс текущей страницы (0..1) + список прогрессов сегментов
        [ObservableProperty] private double pageProgress; // 0..1
        public ObservableCollection<double> PageProgressList { get; } = new();
        
        // Количество страниц в текущем сторис (для правильного расчета ширины прогресс-баров)
        public int CurrentStoryPageCount => CurrentStory?.Pages?.Count ?? 0;

        // Ширина контейнера прогресс-бара (обновляется через SizeChanged)
        [ObservableProperty] 
        private double progressTimelineContainerWidth = 0;

        // Автоматически обновляем CurrentStoryPageCount при изменении CurrentStory
        partial void OnCurrentStoryChanged(StoryModel? value)
        {
            OnPropertyChanged(nameof(CurrentStoryPageCount));
        }

        /// <summary>
        /// Баланс Yess!Coin пользователя
        /// Берется из Wallet.Balance через API:
        /// MainPageViewModel.Balance → BalanceStore.Instance.Balance → WalletService.GetBalanceAsync() 
        /// → WalletApiService.GetBalanceAsync() → API /api/v1/payments/balance → Wallet.Balance из БД
        /// </summary>
        public string Balance => BalanceStore.Instance.Balance.ToString("0.##");

        // Данные пользователя из локальной БД
        [ObservableProperty] private string displayName = string.Empty;
        [ObservableProperty] private string phone = string.Empty;

        private CancellationTokenSource? _overlayCts;
        private readonly IBannerApiService? _bannerApiService;
        private readonly IPartnersApiService? _partnersApiService;
        private readonly IWalletService? _walletService;
        private readonly IAuthService? _authService;
        private readonly Infrastructure.Auth.IAuthenticationService? _authenticationService;

        // Флаг паузы для удержания пальца
        [ObservableProperty] private bool isStoryPaused;
        
        // Время начала паузы (для корректного возобновления прогресса)
        private DateTime _pauseStartTime;
        private TimeSpan _pausedDuration = TimeSpan.Zero;

        // Оптимизация: защита от повторных вызовов и кэширование
        private readonly SemaphoreSlim _loadUserLock = new(1, 1);
        private readonly SemaphoreSlim _loadPartnersLock = new(1, 1);
        private readonly SemaphoreSlim _loadBalanceLock = new(1, 1);
        private CancellationTokenSource? _loadPartnersCts; // Для отмены предыдущих загрузок
        private int? _cachedUserId;
        private DateTime _lastBalanceUpdate = DateTime.MinValue;
        // Минимальное кэширование (1 секунда) для предотвращения множественных запросов при быстрых обновлениях
        // Баланс всегда загружается напрямую из Wallet.Balance через API
        private const int BalanceCacheSeconds = 1;

        // ====== Команды ======
        public IAsyncRelayCommand<StoryModel> OpenStoryAsyncCommand { get; }
        public IRelayCommand CloseStoryCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PrevPageCommand { get; }
        public IRelayCommand PauseStoryCommand { get; }
        public IRelayCommand ResumeStoryCommand { get; }

        public IAsyncRelayCommand<InfoButtonModel> OpenInfoButtonAsyncCommand { get; }
        
        public IAsyncRelayCommand<PartnerLogoModel> OpenPartnerAsyncCommand { get; }

        public MainPageViewModel(
            IBannerApiService? bannerApiService = null,
            IPartnersApiService? partnersApiService = null,
            IWalletService? walletService = null,
            IAuthService? authService = null,
            Infrastructure.Auth.IAuthenticationService? authenticationService = null)
        {
            _bannerApiService = bannerApiService;
            _partnersApiService = partnersApiService;
            _walletService = walletService;
            _authService = authService;
            _authenticationService = authenticationService;
            
            // Подписка на изменение баланса — обновляем метку на главной
            BalanceStore.Instance.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BalanceStore.Balance))
                    OnPropertyChanged(nameof(Balance));
            };

            LoadStories();
            LoadInfoButtons(); // Загрузка информационных кнопок
            LoadTopCategories();
            _ = LoadPartnersAsync(); // Асинхронная загрузка партнёров с сервера

            // Инициализация команд для сторис
            OpenStoryAsyncCommand = new AsyncRelayCommand<StoryModel?>(OpenStoryAsync);
            CloseStoryCommand = new RelayCommand(CloseStory);
            NextPageCommand = new RelayCommand(() => NextPage());
            PrevPageCommand = new RelayCommand(() => PrevPage());
            PauseStoryCommand = new RelayCommand(PauseStory);
            ResumeStoryCommand = new RelayCommand(ResumeStory);

            // Инициализация команд для информационных кнопок
            OpenInfoButtonAsyncCommand = new AsyncRelayCommand<InfoButtonModel?>(OpenInfoButtonAsync);
            
            // Команда для открытия партнёра создаётся автоматически через [RelayCommand] на методе OpenPartnerAsync
            // Но нужно явно создать её для правильной работы биндинга
            OpenPartnerAsyncCommand = new AsyncRelayCommand<PartnerLogoModel>(OpenPartnerAsync);

            // Загружаем баланс кошелька текущего пользователя (если сервис доступен)
            _ = LoadBalanceAsync();
            
            // Загружаем данные пользователя из локальной БД
            _ = LoadUserAsync();
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
                    "storiespage_bonus.png",
                }
            });

            Stories.Add(new StoryModel
            {
                Title = "Yess!Coin",
                Icon = "stories_yesscoin.png",
                Pages = new() {
                    "storiespage_yesscoin.png",
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
                Icon = "stories_sales.png",
                Pages = new() {
                    "sales_stories1.png",
                    "sales_stories2.png",
                    "sales_stories3.png",
                    "sales_stories4.png",
                }
            });

            Stories.Add(new StoryModel
            {
                Title = "ДР",
                Icon = "stories_bday.png",
                Pages = new() {
                    "storiespage_bday.png",
                }
            });
        }

        /// <summary>
        /// Загружает баланс из Wallet.Balance через API и обновляет BalanceStore
        /// Использует минимальное кэширование (1 секунда) для оптимизации
        /// </summary>
        private async Task LoadBalanceAsync()
        {
            // Проверяем кэш (минимальное кэширование для предотвращения множественных запросов)
            if ((DateTime.Now - _lastBalanceUpdate).TotalSeconds < BalanceCacheSeconds)
                return;

            if (!await _loadBalanceLock.WaitAsync(0))
                return; // Уже выполняется

            try
            {
                if (_walletService == null)
                    return;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                // Получаем баланс напрямую из Wallet.Balance через API (из базы данных)
                var balance = await _walletService.GetBalanceAsync();
                BalanceStore.Instance.Balance = balance;
                _lastBalanceUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error loading wallet balance: {ex.Message}");
            }
            finally
            {
                _loadBalanceLock.Release();
            }
        }

        /// <summary>
        /// Загружает баланс напрямую из Wallet.Balance через API без проверки кэша
        /// Используется для принудительного обновления баланса из базы данных
        /// </summary>
        private async Task LoadBalanceFromDbAsync()
        {
            if (!await _loadBalanceLock.WaitAsync(0))
                return; // Уже выполняется

            try
            {
                if (_walletService == null)
                    return;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                // Получаем баланс напрямую из Wallet.Balance через API (из базы данных)
                // Без проверки кэша - всегда свежие данные из БД
                var balance = await _walletService.GetBalanceAsync();
                BalanceStore.Instance.Balance = balance;
                _lastBalanceUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error loading wallet balance from DB: {ex.Message}");
            }
            finally
            {
                _loadBalanceLock.Release();
            }
        }

        /// <summary>
        /// Публичный метод для обновления баланса из Wallet.Balance
        /// Загружает баланс напрямую из базы данных без кэширования
        /// Можно вызывать извне для принудительного обновления (например, после операций с кошельком)
        /// </summary>
        public async Task RefreshBalanceAsync()
        {
            // Загружаем баланс напрямую из БД без проверки кэша
            await LoadBalanceFromDbAsync();
        }

        /// <summary>
        /// Отменяет загрузку партнеров и освобождает ресурсы
        /// Вызывается при OnDisappearing для оптимизации памяти
        /// </summary>
        public void CancelPartnersLoading()
        {
            _loadPartnersCts?.Cancel();
            _loadPartnersCts?.Dispose();
            _loadPartnersCts = null;
        }

        private async Task LoadUserAsync()
        {
            if (!await _loadUserLock.WaitAsync(0))
                return; // Уже выполняется

            try
            {
                if (_authService == null)
                    return;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                var localUser = await _authService.GetLocalUserAsync();
                if (localUser != null)
                {
                    // Если телефон пустой в локальной БД, пытаемся получить его из токена
                    var phone = localUser.Phone;
                    if (string.IsNullOrWhiteSpace(phone) && _authenticationService != null)
                    {
                        try
                        {
                            // Используем таймаут для получения токена
                            var tokenTask = _authenticationService.GetAccessTokenAsync();
                            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
                            var completedTask = await Task.WhenAny(tokenTask, timeoutTask);
                            
                            if (completedTask == tokenTask)
                            {
                                var accessToken = await tokenTask;
                                if (!string.IsNullOrWhiteSpace(accessToken))
                                {
                                    var phoneFromToken = JwtHelper.GetPhone(accessToken);
                                    if (!string.IsNullOrWhiteSpace(phoneFromToken))
                                    {
                                        phone = phoneFromToken;
                                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Using phone from token: {phone}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Failed to get phone from token: {ex.Message}");
                        }
                    }

                    // DisplayName всегда показывает ФИО из БД (приоритет: БД → API → пустая строка)
                    var displayName = localUser.Name;
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] LoadUserAsync: Initial displayName from DB = '{displayName}'");
                    
                    // Если ФИО пустое в БД, пытаемся загрузить профиль из API и обновить БД
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Name is empty in DB, loading profile from API...");
                        try
                        {
                            // Загружаем профиль из API с таймаутом
                            var profileTask = _authService.GetUserProfileAsync(cts.Token);
                            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
                            var completedTask = await Task.WhenAny(profileTask, timeoutTask);
                            
                            if (completedTask == profileTask)
                            {
                                var userProfile = await profileTask;
                                if (userProfile != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Got profile from API: FirstName='{userProfile.FirstName}', LastName='{userProfile.LastName}'");
                                    
                                    // SaveOrUpdateUserAsync автоматически сохранит Name в БД из FirstName и LastName
                                    // Перезагружаем пользователя из БД, чтобы получить обновленное имя
                                    var updatedUser = await _authService.GetLocalUserAsync(cts.Token);
                                    if (updatedUser != null && !string.IsNullOrWhiteSpace(updatedUser.Name))
                                    {
                                        displayName = updatedUser.Name;
                                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Updated displayName from DB after API load: {displayName}");
                                    }
                                    else
                                    {
                                        // Если в БД все еще пусто, формируем из API напрямую
                                        var firstName = userProfile.FirstName?.Trim() ?? string.Empty;
                                        var lastName = userProfile.LastName?.Trim() ?? string.Empty;
                                        displayName = $"{firstName} {lastName}".Trim();
                                        if (!string.IsNullOrWhiteSpace(displayName))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Using Name from API: {displayName}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] ⚠️ API profile load timed out");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ❌ Failed to load profile from API: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Using Name from DB: {displayName}");
                    }

                    // Используем синхронное обновление для надежной работы в release режиме
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DisplayName = displayName ?? string.Empty;
                        Phone = phone ?? string.Empty;
                        OnPropertyChanged(nameof(DisplayName));
                        OnPropertyChanged(nameof(Phone));
                    });
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Loaded user: DisplayName={displayName ?? string.Empty}, Phone={phone ?? string.Empty}");
                }
                else
                {
                    // Если нет локального пользователя, пытаемся загрузить из API и сохранить в БД
                    string? phone = null;
                    string displayName = string.Empty;
                    
                    if (_authenticationService != null)
                    {
                        try
                        {
                            // Используем таймаут для получения токена
                            var tokenTask = _authenticationService.GetAccessTokenAsync();
                            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
                            var completedTask = await Task.WhenAny(tokenTask, timeoutTask);
                            
                            if (completedTask == tokenTask)
                            {
                                var accessToken = await tokenTask;
                                if (!string.IsNullOrWhiteSpace(accessToken))
                                {
                                    phone = JwtHelper.GetPhone(accessToken);
                                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Using phone from token (no local user): {phone}");
                                    
                                    // Загружаем профиль из API и сохраняем в БД
                                    try
                                    {
                                        var profileTask = _authService.GetUserProfileAsync(cts.Token);
                                        var profileTimeoutTask = Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
                                        var profileCompletedTask = await Task.WhenAny(profileTask, profileTimeoutTask);
                                        
                                        if (profileCompletedTask == profileTask)
                                        {
                                            var userProfile = await profileTask;
                                            if (userProfile != null)
                                            {
                                                // SaveOrUpdateUserAsync автоматически сохранит Name в БД
                                                // Перезагружаем пользователя из БД, чтобы получить Name
                                                var savedUser = await _authService.GetLocalUserAsync(cts.Token);
                                                if (savedUser != null && !string.IsNullOrWhiteSpace(savedUser.Name))
                                                {
                                                    displayName = savedUser.Name;
                                                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Loaded Name from DB after API save: {displayName}");
                                                }
                                                else
                                                {
                                                    // Если в БД все еще пусто, формируем из API напрямую
                                                    var firstName = userProfile.FirstName?.Trim() ?? string.Empty;
                                                    var lastName = userProfile.LastName?.Trim() ?? string.Empty;
                                                    displayName = $"{firstName} {lastName}".Trim();
                                                    if (!string.IsNullOrWhiteSpace(displayName))
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Using Name from API (no local user): {displayName}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Failed to load profile from API: {ex.Message}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Failed to get data from token: {ex.Message}");
                        }
                    }

                    // Используем синхронное обновление для надежной работы в release режиме
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DisplayName = displayName;
                        Phone = phone ?? string.Empty;
                        OnPropertyChanged(nameof(DisplayName));
                        OnPropertyChanged(nameof(Phone));
                    });
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] No local user found, DisplayName={displayName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error loading user: {ex.Message}");
                
                // Попытаемся загрузить из БД еще раз в случае ошибки
                try
                {
                    if (_authService != null)
                    {
                        var fallbackUser = await _authService.GetLocalUserAsync();
                        if (fallbackUser != null && !string.IsNullOrWhiteSpace(fallbackUser.Name))
                        {
                            // Используем синхронное обновление для надежной работы в release режиме
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                DisplayName = fallbackUser.Name;
                                Phone = fallbackUser.Phone ?? string.Empty;
                                OnPropertyChanged(nameof(DisplayName));
                                OnPropertyChanged(nameof(Phone));
                            });
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Recovered from error using DB: DisplayName={fallbackUser.Name}");
                            return;
                        }
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Failed to recover from error: {fallbackEx.Message}");
                }
                
                // Только в крайнем случае устанавливаем пустую строку
                // Используем синхронное обновление для надежной работы в release режиме
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DisplayName = string.Empty;
                    Phone = string.Empty;
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(Phone));
                });
            }
            finally
            {
                _loadUserLock.Release();
            }
        }

        /// <summary>
        /// Обновить данные пользователя (можно вызвать после изменения профиля)
        /// </summary>
        public async Task RefreshUserAsync()
        {
            await LoadUserAsync();
        }

        // ====== ДАННЫЕ Партнёров======
        private void LoadPartnerInfo()
        {
            // 🔹 Тестовый партнёр — пример данных для карточки
            var testPartner = new PartnerDetailDto
            {
                Id = 1,
                Name = "CoffeeTime",
                Description = "CoffeeTime — уютная кофейня с ароматным кофе, десертами и атмосферой уюта. " +
                              "Держателям карты YessGo доступны скидки до 10% и кешбэк 5%.",
                Category = "Кафе и рестораны",
                LogoUrl = "coffeetime_logo.png",     // картинка в Resources/Images/
                CoverImageUrl = "coffee_banner.png",     // опционально
                Address = "г. Бишкек, ул. Ибраимова, 115",
                Latitude = 42.8746,
                Longitude = 74.6122,
                Phone = "+996 555 123 456",
                Website = "https://coffeetime.kg",
                DefaultCashbackRate = 5.0,
                CashbackRate = 5.0,
                MaxDiscountPercent = 10.0
            };

            // 🔹 Лог: выводим информацию в Output (в будущем можно передавать на экран деталей)
            System.Diagnostics.Debug.WriteLine(
                $"[Partner Info]\n" +
                $"Название: {testPartner.Name}\n" +
                $"Категория: {testPartner.Category}\n" +
                $"Описание: {testPartner.Description}\n" +
                $"Телефон: {testPartner.Phone}\n" +
                $"Адрес: {testPartner.Address}\n" +
                $"Кешбэк: {testPartner.DefaultCashbackRate}%\n" +
                $"Макс. скидка: {testPartner.MaxDiscountPercent}%");

            // 🔹 Пример, как можно позже использовать:
            // await Shell.Current.GoToAsync($"partnerdetails?partnerId={testPartner.Id}");
        }


        private void LoadInfoButtons()
        {
            try
            {
                InfoButtons.Clear();
                
                // Информационные кнопки
                InfoButtons.Add(new InfoButtonModel
                {
                    Title = "Как пользоваться",
                    Icon = "📱",
                    ActionType = "help",
                    Route = "///MorePage"
                });
                
                InfoButtons.Add(new InfoButtonModel
                {
                    Title = "Пополнить баланс",
                    Icon = "💳",
                    ActionType = "topup",
                    Route = "///wallet"
                });
                
                InfoButtons.Add(new InfoButtonModel
                {
                    Title = "Перевести средства",
                    Icon = "💸",
                    ActionType = "transfer",
                    Route = "///wallet"
                });
                
                InfoButtons.Add(new InfoButtonModel
                {
                    Title = "О нас",
                    Icon = "ℹ️",
                    ActionType = "about",
                    Route = "///MorePage"
                });
                
                InfoButtons.Add(new InfoButtonModel
                {
                    Title = "Помощь",
                    Icon = "❓",
                    ActionType = "support",
                    Route = "///FeedbackPage"
                });
                
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Loaded {InfoButtons.Count} info buttons");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error loading info buttons: {ex.Message}");
            }
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

        private async Task LoadPartnersAsync()
        {
            if (!await _loadPartnersLock.WaitAsync(0))
                return; // Уже выполняется

            try
            {
                // Отменяем предыдущую загрузку, если она еще выполняется
                _loadPartnersCts?.Cancel();
                _loadPartnersCts?.Dispose();
                _loadPartnersCts = new CancellationTokenSource();

                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] LoadPartnersAsync: начало загрузки партнёров из БД");
#if ANDROID
                Android.Util.Log.Info("MainPageViewModel", "[LoadPartnersAsync] Начало загрузки партнёров");
#endif

                // Проверяем отмену перед обновлением UI
                _loadPartnersCts.Token.ThrowIfCancellationRequested();
                
                // Очищаем на главном потоке
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    PartnersRow1.Clear();
                    PartnersRow2.Clear();
                    PartnersRow3.Clear();
                });

                if (_partnersApiService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPageViewModel] PartnersApiService недоступен, не можем загрузить партнёров");
#if ANDROID
                    Android.Util.Log.Warn("MainPageViewModel", "[LoadPartnersAsync] PartnersApiService is null");
#endif
                    return; // Не используем fallback, просто оставляем пустым
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    _loadPartnersCts.Token, 
                    new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                var partners = await _partnersApiService.GetAllAsync(cts.Token);

                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Получено партнёров из API: {partners?.Count ?? 0}");
#if ANDROID
                Android.Util.Log.Info("MainPageViewModel", $"[LoadPartnersAsync] Получено партнёров: {partners?.Count ?? 0}");
#endif

                if (partners == null || partners.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Партнёры не получены из API или список пуст, используем fallback");
#if ANDROID
                    Android.Util.Log.Warn("MainPageViewModel", "[LoadPartnersAsync] Партнёры не получены, используем fallback");
#endif
                    await MainThread.InvokeOnMainThreadAsync(() => LoadPartnersFallback());
                    return;
                }

                // Обработку данных выполняем в фоне для оптимизации
                var list = await Task.Run(() =>
                {
                    return partners
                        .OfType<PartnerDto>()
                        .Select(p => 
                        {
                            var logoUrl = p.LogoUrl?.Trim() ?? "";
                            
                            // Нормализуем URL: если это относительный путь, он будет обработан конвертером
                            // Но если URL пустой или null, оставляем пустым - будет показан текст
                            if (!string.IsNullOrWhiteSpace(logoUrl))
                            {
                                // Если URL не начинается с http/https, но начинается с /, это относительный путь
                                // Конвертер сам добавит базовый URL
                                if (!logoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                    !logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                                    !logoUrl.StartsWith("/"))
                                {
                                    // Если URL не начинается с /, добавляем его
                                    logoUrl = "/" + logoUrl.TrimStart('/');
                                }
                            }
                            
                            return new PartnerLogoModel
                            {
                                Id = p.Id.ToString(),
                                Name = p.Name ?? "Партнёр",
                                Logo = logoUrl // Сохраняем даже пустой LogoUrl - конвертер обработает
                            };
                        })
                        .ToList(); // НЕ фильтруем - показываем ВСЕ партнёры из БД
                });

                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Всего партнёров из БД: {list.Count}");
#if ANDROID
                Android.Util.Log.Info("MainPageViewModel", $"[LoadPartnersAsync] Всего партнёров после обработки: {list.Count}");
#endif
                
                if (list.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Список партнёров пуст после обработки, используем fallback");
#if ANDROID
                    Android.Util.Log.Warn("MainPageViewModel", "[LoadPartnersAsync] Список пуст, используем fallback");
#endif
                    await MainThread.InvokeOnMainThreadAsync(() => LoadPartnersFallback());
                    return;
                }

                // Разделение на ряды также выполняем в фоне
                var rows = await Task.Run(() =>
                {
                    // Проверяем отмену
                    cts.Token.ThrowIfCancellationRequested();

                    // === ДЕЛИМ НА 3 РЯДА ===
                    int count = list.Count;
                    int perRow = Math.Max(1, count / 3);
                    
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Разделение на ряды: всего={count}, на ряд={perRow}");

                    var row1 = list.Take(perRow).ToList();
                    var row2 = list.Skip(perRow).Take(perRow).ToList();
                    var row3 = list.Skip(perRow * 2).ToList();

                    if (row2.Count == 0) row2 = row1.ToList();
                    if (row3.Count == 0) row3 = row2.ToList();

                    // === ДУБЛИРУЕМ КАЖДЫЙ РЯД (обязательно для бесшовного скролла) ===
                    row1 = row1.Concat(row1).ToList();
                    row2 = row2.Concat(row2).ToList();
                    row3 = row3.Concat(row3).ToList();

                    // === ДЕЛАЕМ ВСЕ РЯДЫ ОДИНАКОВЫМИ ПО ДЛИНЕ ===
                    row1 = EnsureEnough(row1);
                    row2 = EnsureEnough(row2);
                    row3 = EnsureEnough(row3);

                    return (row1, row2, row3);
                }, cts.Token);

                // Проверяем отмену перед обновлением UI
                cts.Token.ThrowIfCancellationRequested();
                
                // === ЗАПОЛНЯЕМ КОЛЛЕКЦИИ ДЛЯ UI НА ГЛАВНОМ ПОТОКЕ (оптимизировано) ===
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Добавляем элементы батчами для лучшей производительности
                    // (ObservableCollection не имеет AddRange, но батчинг уменьшает количество обновлений UI)
                    const int batchSize = 10;
                    for (int i = 0; i < rows.row1.Count; i += batchSize)
                    {
                        var batch = rows.row1.Skip(i).Take(batchSize);
                        foreach (var p in batch) PartnersRow1.Add(p);
                    }
                    for (int i = 0; i < rows.row2.Count; i += batchSize)
                    {
                        var batch = rows.row2.Skip(i).Take(batchSize);
                        foreach (var p in batch) PartnersRow2.Add(p);
                    }
                    for (int i = 0; i < rows.row3.Count; i += batchSize)
                    {
                        var batch = rows.row3.Skip(i).Take(batchSize);
                        foreach (var p in batch) PartnersRow3.Add(p);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] PARTNERS READY: row1={PartnersRow1.Count}, row2={PartnersRow2.Count}, row3={PartnersRow3.Count}");
#if ANDROID
                Android.Util.Log.Info("MainPageViewModel", $"[LoadPartnersAsync] PARTNERS READY: row1={PartnersRow1.Count}, row2={PartnersRow2.Count}, row3={PartnersRow3.Count}");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] LoadPartnersAsync ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] StackTrace: {ex.StackTrace}");
#if ANDROID
                Android.Util.Log.Error("MainPageViewModel", $"[LoadPartnersAsync] ERROR: {ex.Message}");
                Android.Util.Log.Error("MainPageViewModel", $"[LoadPartnersAsync] StackTrace: {ex.StackTrace}");
#endif
                await MainThread.InvokeOnMainThreadAsync(() => LoadPartnersFallback());
            }
            finally
            {
                _loadPartnersLock.Release();
            }
        }

        /// <summary>
        /// Оптимизированная функция для обеспечения достаточного количества элементов для бесшовного скролла
        /// Уменьшено количество элементов для улучшения производительности
        /// </summary>
        private List<PartnerLogoModel> EnsureEnough(List<PartnerLogoModel> list)
        {
            const int MinItems = 12; // Уменьшено с 24 для оптимизации
            const int MaxItems = 30; // Уменьшено с 60 для оптимизации
            
            while (list.Count < MinItems)
                list = list.Concat(list).ToList();

            if (list.Count > MaxItems)
                list = list.Take(MaxItems).ToList();

            return list;
        }



        private void LoadPartnersFallback()
        {
            PartnersRow1.Clear();
            PartnersRow2.Clear();
            PartnersRow3.Clear();

            var logos = new[]
            {
                "promzona.jpg","faiza.png","navat.png","flask.png","chikenstar.jpg",
                "bublik.png","sierra.jpg","ants.jpg","supara.png","teplo.png","savetheales.png"
            };

            // Маппинг логотипов к ID партнёров (для навигации)
            var logoToIdMap = new Dictionary<string, string>
            {
                { "navat.png", "12" },  // ID "Нават" из базы данных
                { "sierra.jpg", "1" },
                { "ants.jpg", "2" },
                { "bublik.png", "3" },
                { "flask.png", "4" },
                { "supara.png", "5" },
                { "faiza.png", "6" },
                { "chikenstar.jpg", "7" },
                { "savetheales.png", "8" },
                { "promzona.jpg", "9" },
                { "teplo.png", "10" }
            };

            foreach (var l in logos) 
            {
                var id = logoToIdMap.GetValueOrDefault(l, string.Empty);
                PartnersRow1.Add(new PartnerLogoModel { Logo = l, Id = id, Name = l.Replace(".png", "").Replace(".jpg", "") });
            }
            foreach (var l in logos.Reverse()) 
            {
                var id = logoToIdMap.GetValueOrDefault(l, string.Empty);
                PartnersRow2.Add(new PartnerLogoModel { Logo = l, Id = id, Name = l.Replace(".png", "").Replace(".jpg", "") });
            }
            foreach (var l in logos) 
            {
                var id = logoToIdMap.GetValueOrDefault(l, string.Empty);
                PartnersRow3.Add(new PartnerLogoModel { Logo = l, Id = id, Name = l.Replace(".png", "").Replace(".jpg", "") });
            }

            // дублирование — для бесшовной ленты
            foreach (var l in logos) 
            {
                var id = logoToIdMap.GetValueOrDefault(l, string.Empty);
                PartnersRow1.Add(new PartnerLogoModel { Logo = l, Id = id, Name = l.Replace(".png", "").Replace(".jpg", "") });
            }
            foreach (var l in logos.Reverse()) 
            {
                var id = logoToIdMap.GetValueOrDefault(l, string.Empty);
                PartnersRow2.Add(new PartnerLogoModel { Logo = l, Id = id, Name = l.Replace(".png", "").Replace(".jpg", "") });
            }
            foreach (var l in logos) 
            {
                var id = logoToIdMap.GetValueOrDefault(l, string.Empty);
                PartnersRow3.Add(new PartnerLogoModel { Logo = l, Id = id, Name = l.Replace(".png", "").Replace(".jpg", "") });
            }
        }

        // ====== СТОРИС: «как в инсте» ======

        public async Task OpenStoryAsync(StoryModel? story)
        {
            // Валидация: если сторис пустой или нет страниц - не открываем
            if (story == null || story.Pages == null || story.Pages.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] OpenStoryAsync: Story is null or has no pages");
                return;
            }

            // Валидация: если массив сторисов пуст - не открываем
            if (Stories == null || Stories.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] OpenStoryAsync: Stories collection is empty");
                return;
            }
            
            _overlayCts?.Cancel();
            _overlayCts = new CancellationTokenSource();
            
            // Сбрасываем состояние паузы
            IsStoryPaused = false;
            _pausedDuration = TimeSpan.Zero;

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
                OnPropertyChanged(nameof(CurrentStoryPageCount)); // Обновляем количество страниц для биндинга

                var pages = CurrentStory.Pages ?? new();
                if (pages.Count == 0) continue;

                PrepareSegments(pages.Count);
                
                // Сбрасываем паузу для нового сториса
                IsStoryPaused = false;
                _pausedDuration = TimeSpan.Zero;

                IsStoryOpen = true;

                for (int p = 0; p < pages.Count; p++)
                {
                    CurrentPageIndex = p;
                    UpdateCurrentPageImage();
                    
                    // Сбрасываем паузу для новой страницы
                    IsStoryPaused = false;
                    _pausedDuration = TimeSpan.Zero;

                    await RunSmoothProgressAsync(p, ct);
                    if (ct.IsCancellationRequested) return;

                    // Проверяем границы перед установкой значения
                    if (p >= 0 && p < PageProgressList.Count)
                    {
                        PageProgressList[p] = 1.0;
                        OnPropertyChanged(nameof(PageProgressList));
                    }
                }
                
                // Помечаем Story как просмотренный после завершения просмотра
                if (CurrentStory != null && pages.Count > 0)
                {
                    // Помечаем как просмотренный, только если просмотрели все страницы
                    bool allPagesViewed = PageProgressList.All(prog => prog >= 1.0);
                    if (allPagesViewed)
                    {
                        CurrentStory.IsViewed = true;
                    }
                }
            }

            CloseStory();
        }

        private async Task RunSmoothProgressAsync(int segmentIndex, CancellationToken ct)
        {
            const int durationMs = 5000; // 5 секунд как в Instagram
            var sw = Stopwatch.StartNew();
            var startTime = sw.ElapsedMilliseconds;

            try
            {
                _ = PrefetchNextImage();

                while (sw.ElapsedMilliseconds - startTime - _pausedDuration.TotalMilliseconds < durationMs && !ct.IsCancellationRequested)
                {
                    // Если сторис на паузе - ждем и не обновляем прогресс
                    if (IsStoryPaused)
                    {
                        await Task.Delay(16, ct);
                        continue;
                    }

                    // Вычисляем прогресс с учетом времени паузы
                    double elapsed = sw.ElapsedMilliseconds - startTime - _pausedDuration.TotalMilliseconds;
                    double prog = Math.Clamp(elapsed / durationMs, 0, 1);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        PageProgress = prog;
                        // Проверяем границы перед установкой значения
                        if (segmentIndex >= 0 && segmentIndex < PageProgressList.Count)
                        {
                            PageProgressList[segmentIndex] = prog;
                            OnPropertyChanged(nameof(PageProgressList));
                        }
                    });

                    await Task.Delay(16, ct); // ~60fps
                }
            }
            catch (TaskCanceledException)
            {
                // 🔹 Это штатная ситуация — пользователь пролистал или закрыл сторис.
                // Игнорируем отмену, чтобы не падало приложение.
            }
            catch (Exception ex)
            {
                // 🔹 Любые другие ошибки логируем, чтобы не крашилось приложение.
                System.Diagnostics.Debug.WriteLine($"[StoryProgress] Unexpected error: {ex}");
            }
            finally
            {
                sw.Stop();
            }
        }


        private void PrepareSegments(int pagesCount)
        {
            PageProgressList.Clear();
            for (int i = 0; i < pagesCount; i++) PageProgressList.Add(0.0);
            PageProgress = 0;
            OnPropertyChanged(nameof(PageProgressList));
            // Убеждаемся, что CurrentStoryPageCount обновлен для правильного расчета ширины
            OnPropertyChanged(nameof(CurrentStoryPageCount));
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

        private Task PrefetchBannerImage(string imageUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                    return Task.CompletedTask;

                if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absUri)
                    && (absUri.Scheme == Uri.UriSchemeHttp || absUri.Scheme == Uri.UriSchemeHttps))
                {
                    // Предзагрузка изображения в кэш для более быстрого отображения
                    var imageSource = new UriImageSource
                    {
                        Uri = absUri,
                        CachingEnabled = true,
                        CacheValidity = TimeSpan.FromDays(7)
                    };
                    // MAUI автоматически кэширует изображение при создании UriImageSource
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Prefetching banner image: {imageUrl}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error prefetching banner image: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private void NextPage()
        {
            if (!IsStoryOpen || CurrentStory == null) return;

            _overlayCts?.Cancel();
            
            // Сбрасываем паузу при переходе
            IsStoryPaused = false;
            _pausedDuration = TimeSpan.Zero;

            var pages = CurrentStory.Pages ?? new();
            if (CurrentPageIndex + 1 < pages.Count)
            {
                // Переход к следующей странице в текущем сторисе
                _ = ResumeFrom(CurrentStoryIndex, CurrentPageIndex + 1);
            }
            else
            {
                // Переход к следующему сторису
                int nextStoryIndex = CurrentStoryIndex + 1;
                if (nextStoryIndex < Stories.Count)
                {
                    _ = ResumeFrom(nextStoryIndex, 0);
                }
                else
                {
                    // Это был последний сторис - закрываем
                    CloseStory();
                }
            }
        }

        private void PrevPage()
        {
            if (!IsStoryOpen) return;

            _overlayCts?.Cancel();
            
            // Сбрасываем паузу при переходе
            IsStoryPaused = false;
            _pausedDuration = TimeSpan.Zero;

            if (CurrentStory != null && CurrentPageIndex - 1 >= 0)
            {
                // Переход к предыдущей странице в текущем сторисе
                _ = ResumeFrom(CurrentStoryIndex, CurrentPageIndex - 1);
            }
            else
            {
                // Переход к предыдущему сторису
                int prevStory = CurrentStoryIndex - 1;
                if (prevStory >= 0)
                {
                    var prevPages = Stories[prevStory].Pages ?? new();
                    int lastPage = Math.Max(0, prevPages.Count - 1);
                    _ = ResumeFrom(prevStory, lastPage);
                }
                // Если это первый сторис - ничего не делаем (как в Instagram)
            }
        }

        private async Task ResumeFrom(int storyIndex, int pageIndex)
        {
            _overlayCts = new CancellationTokenSource();
            
            // Сбрасываем паузу при переходе
            IsStoryPaused = false;
            _pausedDuration = TimeSpan.Zero;

            CurrentStoryIndex = Math.Clamp(storyIndex, 0, Stories.Count - 1);
            CurrentStory = Stories[CurrentStoryIndex];
            OnPropertyChanged(nameof(CurrentStoryPageCount)); // Обновляем количество страниц для биндинга

            var pages = CurrentStory.Pages ?? new();
            if (pages.Count == 0) { CloseStory(); return; }

            PrepareSegments(pages.Count);
            for (int i = 0; i < pages.Count && i < PageProgressList.Count; i++)
            {
                PageProgressList[i] = i < pageIndex ? 1.0 : 0.0;
            }
            OnPropertyChanged(nameof(PageProgressList));

            IsStoryOpen = true;

            CurrentPageIndex = Math.Clamp(pageIndex, 0, pages.Count - 1);
            UpdateCurrentPageImage();

            for (int p = CurrentPageIndex; p < pages.Count; p++)
            {
                CurrentPageIndex = p;
                UpdateCurrentPageImage();
                
                // Сбрасываем паузу для новой страницы
                IsStoryPaused = false;
                _pausedDuration = TimeSpan.Zero;

                await RunSmoothProgressAsync(p, _overlayCts.Token);
                if (_overlayCts.IsCancellationRequested) return;

                // Проверяем границы перед установкой значения
                if (p >= 0 && p < PageProgressList.Count)
                {
                    PageProgressList[p] = 1.0;
                    OnPropertyChanged(nameof(PageProgressList));
                }
            }
            
            // Помечаем Story как просмотренный после завершения просмотра всех страниц
            if (CurrentStory != null && pages.Count > 0)
            {
                bool allPagesViewed = PageProgressList.All(prog => prog >= 1.0);
                if (allPagesViewed)
                {
                    CurrentStory.IsViewed = true;
                }
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
            IsStoryPaused = false;
            CurrentStory = null;
            CurrentStoryIndex = -1;
            CurrentPageIndex = -1;
            CurrentPageImage = null;
            PageProgress = 0;
            PageProgressList.Clear();
            _pausedDuration = TimeSpan.Zero;
            OnPropertyChanged(nameof(PageProgressList));
        }

        // Методы для управления паузой
        public void PauseStory()
        {
            if (!IsStoryOpen || IsStoryPaused) return;
            
            IsStoryPaused = true;
            _pauseStartTime = DateTime.Now;
            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Story paused");
        }

        public void ResumeStory()
        {
            if (!IsStoryOpen || !IsStoryPaused) return;
            
            // Вычисляем время паузы
            _pausedDuration += DateTime.Now - _pauseStartTime;
            IsStoryPaused = false;
            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Story resumed. Total paused: {_pausedDuration.TotalMilliseconds}ms");
        }

        // ====== Информационные кнопки ======
        public async Task OpenInfoButtonAsync(InfoButtonModel? infoButton)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ===== OpenInfoButtonAsync CALLED =====");
            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] InfoButton: {(infoButton == null ? "NULL" : $"Title='{infoButton.Title}', ActionType='{infoButton.ActionType}'")}");
            
            if (infoButton == null)
            {
                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] InfoButton is null, returning");
                return;
            }

            try
            {
                string title = infoButton.Title;
                string message = GetInfoMessage(infoButton.ActionType);
                
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Title: {title}");
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Message length: {message.Length}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Пробуем несколько способов получить текущую страницу
                        Page? currentPage = null;
                        
                        // Способ 1: через Application.Current?.MainPage
                        if (Application.Current?.MainPage != null)
                        {
                            currentPage = Application.Current.MainPage;
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Got page from Application.Current.MainPage");
                        }
                        
                        // Способ 2: через Shell.Current
                        if (currentPage == null && Shell.Current != null)
                        {
                            currentPage = Shell.Current.CurrentPage;
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Got page from Shell.Current.CurrentPage");
                        }
                        
                        // Способ 3: через Navigation
                        if (currentPage == null && Application.Current?.MainPage is Shell shell)
                        {
                            currentPage = shell.CurrentPage;
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Got page from Shell.CurrentPage");
                        }

                        if (currentPage != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Showing alert: Title='{title}'");
                            await currentPage.DisplayAlert(title, message, "OK");
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Alert shown successfully");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] ERROR: Could not get current page");
                            // Fallback: пробуем через Shell напрямую
                            if (Shell.Current != null)
                            {
                                await Shell.Current.DisplayAlert(title, message, "OK");
                                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Alert shown via Shell.Current");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error in MainThread.InvokeOnMainThreadAsync: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Stack trace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error opening info button: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Stack trace: {ex.StackTrace}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ===== OpenInfoButtonAsync COMPLETED =====");
        }

        private string GetInfoMessage(string actionType)
        {
            return actionType.ToLower() switch
            {
                "help" => "Как пользоваться приложением:\n\n" +
                         "1. Зарегистрируйтесь или войдите в систему\n" +
                         "2. Просматривайте партнёров и их предложения\n" +
                         "3. Используйте карту для поиска партнёров рядом\n" +
                         "4. Получайте кешбэк за покупки\n" +
                         "5. Пополняйте баланс и используйте Yess!Coin\n\n" +
                         "Подробная информация доступна в разделе 'Ещё'.",
                
                "topup" => "Как пополнить баланс:\n\n" +
                          "1. Перейдите в раздел 'Кошелёк'\n" +
                          "2. Нажмите кнопку 'Пополнить'\n" +
                          "3. Выберите способ пополнения\n" +
                          "4. Введите сумму пополнения\n" +
                          "5. Подтвердите операцию\n\n" +
                          "Средства поступят на ваш баланс в течение нескольких минут.",
                
                "transfer" => "Как перевести средства:\n\n" +
                             "1. Перейдите в раздел 'Кошелёк'\n" +
                             "2. Нажмите кнопку 'Перевести'\n" +
                             "3. Введите номер телефона получателя\n" +
                             "4. Укажите сумму перевода\n" +
                             "5. Подтвердите операцию\n\n" +
                             "Перевод выполняется мгновенно при наличии достаточного баланса.",
                
                "about" => "О нас:\n\n" +
                           "YessGo - это приложение для получения кешбэка и скидок от партнёров.\n\n" +
                           "Мы помогаем вам:\n" +
                           "• Экономить на покупках\n" +
                           "• Получать кешбэк за каждую покупку\n" +
                           "• Находить лучшие предложения рядом\n" +
                           "• Управлять своими финансами\n\n" +
                           "Присоединяйтесь к нашей программе лояльности!",
                
                "support" => "Помощь и поддержка:\n\n" +
                            "Если у вас возникли вопросы или проблемы:\n\n" +
                            "• Обратитесь в раздел 'Обратная связь'\n" +
                            "• Свяжитесь с нашей службой поддержки\n" +
                            "• Посетите раздел 'Ещё' для дополнительной информации\n\n" +
                            "Мы всегда готовы помочь!",
                
                _ => "Информация о данной функции будет добавлена позже."
            };
        }


        public async Task OpenPartnerAsync(PartnerLogoModel partner)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ===== OpenPartnerAsync CALLED =====");
            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Partner parameter: {(partner == null ? "NULL" : $"Name='{partner.Name}', ID='{partner?.Id}'")}");
            
            if (partner == null)
            {
                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] OpenPartnerAsync: partner is null, returning");
                return;
            }

            // 🔹 Для проверки — выведем лог
            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Нажали на партнёра: Name='{partner.Name}', ID='{partner.Id}'");

            try
            {
                // Используем ID партнёра для навигации
                if (!string.IsNullOrWhiteSpace(partner.Id))
                {
                    // Используем абсолютный путь с тремя слешами для навигации к зарегистрированному маршруту
                    var route = $"///partnerdetails?partnerId={Uri.EscapeDataString(partner.Id)}";
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Navigating to: {route}");
                    
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            await Shell.Current.GoToAsync(route);
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Navigation completed successfully");
                        }
                        catch (Exception navEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Navigation exception: {navEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Navigation stack trace: {navEx.StackTrace}");
                            throw;
                        }
                    });
                }
                else if (!string.IsNullOrWhiteSpace(partner.Name))
                {
                    // Fallback: используем имя, если ID не указан
                    var route = $"///partnerdetails?partnerName={Uri.EscapeDataString(partner.Name)}";
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Navigating by name to: {route}");
                    
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            await Shell.Current.GoToAsync(route);
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Navigation completed successfully");
                        }
                        catch (Exception navEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Navigation exception: {navEx.Message}");
                            throw;
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Не удалось открыть партнёра: нет ID и имени");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current?.MainPage?.DisplayAlert("Ошибка", "Не удалось открыть информацию о партнёре", "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Ошибка навигации к партнёру: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Stack trace: {ex.StackTrace}");
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current?.MainPage?.DisplayAlert("Ошибка", $"Не удалось открыть партнёра: {ex.Message}", "OK");
                });
            }
            
            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ===== OpenPartnerAsync COMPLETED =====");
        }
    }
}
