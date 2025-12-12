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

        // Баланс берём из общего BalanceStore
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
        
        // Кэш для партнёров
        private static IReadOnlyList<PartnerDto>? _cachedPartners;
        private static DateTime _partnersCacheTimestamp = DateTime.MinValue;
        private static readonly TimeSpan PartnersCacheExpiry = TimeSpan.FromMinutes(5);

        // Флаг паузы для удержания пальца
        [ObservableProperty] private bool isStoryPaused;
        
        // Время начала паузы (для корректного возобновления прогресса)
        private DateTime _pauseStartTime;
        private TimeSpan _pausedDuration = TimeSpan.Zero;

        // Оптимизация: защита от повторных вызовов и кэширование
        private readonly SemaphoreSlim _loadUserLock = new(1, 1);
        private readonly SemaphoreSlim _loadPartnersLock = new(1, 1);
        private readonly SemaphoreSlim _loadBalanceLock = new(1, 1);
        private int? _cachedUserId;
        private DateTime _lastBalanceUpdate = DateTime.MinValue;
        private const int BalanceCacheSeconds = 30;

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

        public async Task LoadBalanceAsync(CancellationToken ct = default)
        {
            // Проверяем кэш
            if ((DateTime.Now - _lastBalanceUpdate).TotalSeconds < BalanceCacheSeconds)
                return;

            if (!await _loadBalanceLock.WaitAsync(0))
                return; // Уже выполняется

            try
            {
                if (_walletService == null)
                    return;

                // Используем таймаут для запроса баланса (10 секунд)
                CancellationToken finalCt;
                IDisposable? ctsDisposable = null;
                
                if (ct == default)
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    ctsDisposable = cts;
                    finalCt = cts.Token;
                }
                else
                {
                    // Если передан токен извне, используем его (таймаут уже установлен в вызывающем коде)
                    finalCt = ct;
                }

                try
                {
                    var balance = await _walletService.GetBalanceAsync(finalCt);
                    BalanceStore.Instance.Balance = balance;
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Balance loaded successfully: {balance}");
                }
                finally
                {
                    ctsDisposable?.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] LoadBalanceAsync timed out");
                // Не обновляем баланс при таймауте, оставляем старое значение
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error loading wallet balance: {ex.Message}");
                // Не обновляем баланс при ошибке, оставляем старое значение
            }
            finally
            {
                _loadBalanceLock.Release();
            }
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

                    // DisplayName всегда показывает ФИО из БД
                    var displayName = localUser.Name;
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] LoadUserAsync: Initial displayName from DB = '{displayName}'");
                    
                    // Проверяем, нужно ли загружать профиль из API:
                    // 1. Если Name пустое
                    // 2. Если Name равно "Пользователь" (дефолтное значение)
                    // 3. Если Name не содержит пробел (вероятно, это не полное имя)
                    var shouldLoadFromApi = string.IsNullOrWhiteSpace(displayName) ||
                                           displayName.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase) ||
                                           !displayName.Contains(' ');
                    
                    if (shouldLoadFromApi)
                    {
                        System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Name is empty/invalid in DB, loading profile from API...");
                        try
                        {
                            // Используем таймаут для загрузки профиля (10 секунд)
                            using var profileCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                            var userProfile = await _authService.GetUserProfileAsync(profileCts.Token);
                            
                            if (userProfile != null)
                            {
                                var userProfile = await _authService.GetUserProfileAsync();
                                if (userProfile != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Got profile from API: FirstName='{userProfile.FirstName}', LastName='{userProfile.LastName}'");
                                    
                                    // Небольшая задержка, чтобы дать время SaveOrUpdateUserAsync сохранить данные в БД
                                    await Task.Delay(100);
                                    
                                    // Перезагружаем пользователя из БД, чтобы получить обновленное имя
                                    var updatedUser = await _authService.GetLocalUserAsync();
                                    if (updatedUser != null && !string.IsNullOrWhiteSpace(updatedUser.Name) && 
                                        !updatedUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var updatedDisplayName = fullName;
                                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Loaded Name from API: FirstName={firstName}, LastName={lastName}, FullName={fullName}");
                                        
                                        // Перезагружаем пользователя из БД, чтобы получить обновленное имя
                                        var updatedUser = await _authService.GetLocalUserAsync();
                                        if (updatedUser != null && !string.IsNullOrWhiteSpace(updatedUser.Name))
                                        {
                                            updatedDisplayName = updatedUser.Name;
                                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Updated displayName from DB after profile load: {updatedDisplayName}");
                                        }
                                        
                                        // Обновляем UI на главном потоке
                                        await MainThread.InvokeOnMainThreadAsync(() =>
                                        {
                                            DisplayName = updatedDisplayName;
                                        });
                                    }
                                    else
                                    {
                                        // Если в БД все еще "Пользователь" или пустое, но мы получили имя из API,
                                        // используем имя из API напрямую (GetUserProfileAsync должен был сохранить его в БД)
                                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ⚠️ Name not updated in DB yet, using API value: {displayName}");
                                    }
                                }
                                else
                                {
                                    // Если API не вернул имя, но в БД есть что-то валидное - используем его
                                    if (!string.IsNullOrWhiteSpace(localUser.Name) && 
                                        !localUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                                    {
                                        displayName = localUser.Name;
                                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Using Name from DB as fallback: {displayName}");
                                    }
                                    else
                                    {
                                        displayName = "Пользователь";
                                        System.Diagnostics.Debug.WriteLine("[MainPageViewModel] ❌ FirstName and LastName are empty in API response");
                                    }
                                }
                            }
                            else
                            {
                                // Если API вернул null, но в БД есть валидное имя - используем его
                                if (!string.IsNullOrWhiteSpace(localUser.Name) && 
                                    !localUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                                {
                                    displayName = localUser.Name;
                                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Using Name from DB as fallback (API returned null): {displayName}");
                                }
                                else
                                {
                                    displayName = "Пользователь";
                                    System.Diagnostics.Debug.WriteLine("[MainPageViewModel] ❌ API returned null profile");
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] GetUserProfileAsync timed out");
                            // Если таймаут, но в БД есть валидное имя - используем его
                            if (!string.IsNullOrWhiteSpace(localUser.Name) && 
                                !localUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                            {
                                displayName = localUser.Name;
                                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Using Name from DB as fallback (timeout): {displayName}");
                            }
                            else
                            {
                                displayName = "Пользователь";
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ❌ Failed to load profile from API: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Stack trace: {ex.StackTrace}");
                            
                            // Если API не загрузился, но в БД есть валидное имя - используем его
                            if (!string.IsNullOrWhiteSpace(localUser.Name) && 
                                !localUser.Name.Trim().Equals("Пользователь", StringComparison.OrdinalIgnoreCase))
                            {
                                displayName = localUser.Name;
                                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Using Name from DB as fallback (API error): {displayName}");
                            }
                            else
                            {
                                displayName = "Пользователь";
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Using Name from DB: {displayName}");
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayName = displayName;
                        Phone = phone ?? string.Empty;
                    });
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Loaded user: DisplayName={DisplayName}, Phone={Phone}");
                }
                else
                {
                    // Если нет локального пользователя, пытаемся получить данные из токена и API
                    string? phone = null;
                    string displayName = "Пользователь";
                    
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
                                phone = JwtHelper.GetPhone(accessToken);
                                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Using phone from token (no local user): {phone}");
                                
                                // Пытаемся загрузить профиль из API
                                if (_authService != null)
                                {
                                    try
                                    {
                                        // Используем таймаут для загрузки профиля (10 секунд)
                                        using var profileCts2 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                                        var userProfile = await _authService.GetUserProfileAsync(profileCts2.Token);
                                        
                                        if (userProfile != null)
                                        {
                                            // Формируем ФИО из FirstName и LastName напрямую
                                            var firstName = userProfile.FirstName?.Trim() ?? string.Empty;
                                            var lastName = userProfile.LastName?.Trim() ?? string.Empty;
                                            var fullName = $"{firstName} {lastName}".Trim();
                                            
                                            if (!string.IsNullOrWhiteSpace(fullName))
                                            {
                                                displayName = fullName;
                                                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ✅ Loaded Name from API (no local user): FirstName={firstName}, LastName={lastName}, FullName={fullName}");
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] ❌ FirstName and LastName are empty in API response (no local user)");
                                            }
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] ❌ API returned null profile (no local user)");
                                        }
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        System.Diagnostics.Debug.WriteLine("[MainPageViewModel] GetUserProfileAsync timed out (no local user)");
                                        // Оставляем "Пользователь"
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] ❌ Failed to load profile from API (no local user): {ex.Message}");
                                        // Оставляем "Пользователь"
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Failed to get data from token: {ex.Message}");
                        }
                    }

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayName = displayName;
                        Phone = phone ?? string.Empty;
                    });
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] No local user found: DisplayName={DisplayName}, Phone={Phone}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error loading user: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DisplayName = "Пользователь";
                    Phone = string.Empty;
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
            try
            {
                // Используем таймаут для всех операций (15 секунд)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                
                // Загружаем данные пользователя и баланс параллельно для ускорения
                await Task.WhenAll(
                    LoadUserAsyncWithTimeout(cts.Token),
                    LoadBalanceAsyncWithTimeout(cts.Token)
                );
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] RefreshUserAsync completed: DisplayName={DisplayName}, Phone={Phone}, Balance={Balance}");
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] RefreshUserAsync timed out");
                // Пытаемся загрузить хотя бы пользователя, если операция не завершилась
                try
                {
                    await LoadUserAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error loading user after timeout: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error in RefreshUserAsync: {ex.Message}");
                // Пытаемся загрузить хотя бы пользователя, если баланс не загрузился
                try
                {
                    await LoadUserAsync();
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error loading user after error: {ex2.Message}");
                }
            }
        }

        private async Task LoadUserAsyncWithTimeout(CancellationToken ct)
        {
            try
            {
                await LoadUserAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error in LoadUserAsyncWithTimeout: {ex.Message}");
                throw;
            }
        }

        private async Task LoadBalanceAsyncWithTimeout(CancellationToken ct)
        {
            try
            {
                // Используем таймаут для запроса баланса (10 секунд)
                using var balanceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                balanceCts.CancelAfter(TimeSpan.FromSeconds(10));
                await LoadBalanceAsync(balanceCts.Token);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] LoadBalanceAsync timed out");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Error in LoadBalanceAsyncWithTimeout: {ex.Message}");
                throw;
            }
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
                System.Diagnostics.Debug.WriteLine("[MainPageViewModel] LoadPartnersAsync: начало загрузки партнёров из БД");
#if ANDROID
                Android.Util.Log.Info("MainPageViewModel", "[LoadPartnersAsync] Начало загрузки партнёров");
#endif

                // Очищаем на главном потоке
                await MainThread.InvokeOnMainThreadAsync(() =>
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

                // Проверяем кэш
                IReadOnlyList<PartnerDto>? partners = null;
                if (_cachedPartners != null && DateTime.UtcNow - _partnersCacheTimestamp < PartnersCacheExpiry)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Using cached partners");
                    partners = _cachedPartners;
                }
                else
                {
                    // Загружаем из API с таймаутом
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                        partners = await _partnersApiService.GetAllAsync(cts.Token);
                        
                        // Обновляем кэш
                        if (partners != null && partners.Count > 0)
                        {
                            _cachedPartners = partners;
                            _partnersCacheTimestamp = DateTime.UtcNow;
                            System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] Partners cached: {partners.Count} items");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine("[MainPageViewModel] LoadPartnersAsync: GetAllAsync timed out");
                        // Используем кэш, если он есть, даже если истек
                        if (_cachedPartners != null)
                        {
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Using expired cache due to timeout");
                            partners = _cachedPartners;
                        }
                        else
                        {
                            await MainThread.InvokeOnMainThreadAsync(() => LoadPartnersFallback());
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainPageViewModel] LoadPartnersAsync: Error loading from API: {ex.Message}");
                        // Используем кэш, если он есть
                        if (_cachedPartners != null)
                        {
                            System.Diagnostics.Debug.WriteLine("[MainPageViewModel] Using cache due to API error");
                            partners = _cachedPartners;
                        }
                        else
                        {
                            await MainThread.InvokeOnMainThreadAsync(() => LoadPartnersFallback());
                            return;
                        }
                    }
                }

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
                });

                // === ЗАПОЛНЯЕМ КОЛЛЕКЦИИ ДЛЯ UI НА ГЛАВНОМ ПОТОКЕ ===
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var p in rows.row1) PartnersRow1.Add(p);
                    foreach (var p in rows.row2) PartnersRow2.Add(p);
                    foreach (var p in rows.row3) PartnersRow3.Add(p);
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

        private List<PartnerLogoModel> EnsureEnough(List<PartnerLogoModel> list)
        {
            while (list.Count < 24)
                list = list.Concat(list).ToList();

            if (list.Count > 60)
                list = list.Take(60).ToList();

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

                    await MainThread.InvokeOnMainThreadAsync(() =>
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
