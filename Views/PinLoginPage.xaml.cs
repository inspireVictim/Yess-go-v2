using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;
using System.ComponentModel;
using YessGoFront.Services.Domain;
using YessGoFront.Services;

namespace YessGoFront.Views
{
    public partial class PinLoginPage : ContentPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private readonly IAuthService? _authService;
        private string _currentPin = string.Empty;
        private string _tokenStatus = "normal"; // normal, fresh, expired
        private string _tokenStatusMessage = string.Empty;
        private Color _tokenStatusColor = Colors.Gray;
        private bool _isCreatingPin = false; // true - создание PIN, false - ввод PIN
        private string? _confirmPin = null; // Для подтверждения при создании
        private string _subtitleText = string.Empty;

        // Bindable свойства
        public string TitleText => _isCreatingPin ? "Создайте PIN-код" : "Введите PIN-код";
        
        public string SubtitleText
        {
            get => _subtitleText;
            set
            {
                _subtitleText = value;
                OnPropertyChanged();
            }
        }

        public string PinCode
        {
            get => _currentPin;
            set
            {
                if (_currentPin != value)
                {
                    _currentPin = value ?? string.Empty;
                    UpdatePinIndicators();
                    OnPropertyChanged(nameof(PinCode));
                    OnPropertyChanged(nameof(CanDelete));
                }
            }
        }

        public bool HasError { get; private set; }

        public string TokenStatusMessage
        {
            get => _tokenStatusMessage;
            set
            {
                _tokenStatusMessage = value;
                OnPropertyChanged();
            }
        }

        public Color TokenStatusColor
        {
            get => _tokenStatusColor;
            set
            {
                _tokenStatusColor = value;
                OnPropertyChanged();
            }
        }

        public bool ShowTokenStatus => !string.IsNullOrEmpty(_tokenStatusMessage);
        public string ErrorMessage { get; private set; } = string.Empty;
        public bool IsBusy { get; private set; }
        public bool CanDelete => _currentPin.Length > 0;
        public bool IsVerificationMode
        {
            get => !_isCreatingPin && _confirmPin == null;
        }

        public PinLoginPage()
        {
            InitializeComponent();
            
            // Получаем сервисы из DI
            _authService = MauiProgram.Services?.GetService<IAuthService>();

            // Устанавливаем BindingContext для команд
            BindingContext = this;
            
            // Пытаемся определить режим работы из текущего состояния Shell
            try
            {
                var shell = Shell.Current;
                if (shell?.CurrentState?.Location != null)
                {
                    var location = shell.CurrentState.Location.ToString();
                    if (location.Contains("isCreatingPin=true"))
                    {
                        _isCreatingPin = true;
                        System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Constructor: Setting isCreatingPin=true from location: {location}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Constructor: Error checking location: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Если уже начали процесс создания (есть _confirmPin), не меняем режим
            if (_confirmPin != null)
            {
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] OnAppearing: Already in creation mode, _confirmPin={_confirmPin}");
                return;
            }
            
            // Обрабатываем параметр из query string
            var shell = Shell.Current;
            bool hasQueryParam = false;
            if (shell?.CurrentState?.Location != null)
            {
                var location = shell.CurrentState.Location.ToString();
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] OnAppearing: Current location: {location}");
                
                // Проверяем наличие параметров в URL
                hasQueryParam = location.Contains("isCreatingPin=true");
                if (hasQueryParam)
                {
                    _isCreatingPin = true;
                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] OnAppearing: Setting isCreatingPin=true from query param");
                }

                // Проверяем параметр tokenStatus
                if (location.Contains("tokenStatus=fresh"))
                {
                    _tokenStatus = "fresh";
                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] OnAppearing: Token status set to fresh");
                }
            }
            
            // Обновляем IsVerificationMode при изменении режима
            OnPropertyChanged(nameof(IsVerificationMode));
            OnPropertyChanged(nameof(TitleText));

            // Проверяем и отображаем статус токенов (будет обновлен после успешной PIN/биометрии)
            // Не показываем предупреждения, если токены будут обновлены автоматически
            await UpdateTokenStatusAsync();

            // Дополнительная проверка: если параметра нет, проверяем наличие PIN-кода
            if (!hasQueryParam && _authService != null)
            {
                try
                {
                    var hasPin = await _authService.HasPinAsync();
                    _isCreatingPin = !hasPin;
                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] OnAppearing: Checking PIN existence. hasPin={hasPin}, setting isCreatingPin={_isCreatingPin}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] OnAppearing: Error checking PIN: {ex.Message}");
                    // По умолчанию считаем, что это режим создания
                    _isCreatingPin = true;
                }
            }
            
            // Обновляем IsVerificationMode при изменении режима
            OnPropertyChanged(nameof(IsVerificationMode));
            OnPropertyChanged(nameof(TitleText));
            
            // Устанавливаем начальный текст подзаголовка только если еще не начали ввод
            if (string.IsNullOrEmpty(_currentPin))
            {
                SubtitleText = _isCreatingPin 
                    ? "Придумайте 4-значный PIN-код для быстрого входа" 
                    : "Введите 4-значный PIN-код для входа";
            }

            // Если создаем PIN и еще не начали ввод, пробуем сначала биометрию
            if (_isCreatingPin && string.IsNullOrEmpty(_currentPin))
            {
                await TryBiometricFirstAsync();
            }
            // Если в режиме верификации (ввод PIN) и еще не начали ввод, также пробуем биометрию
            else if (!_isCreatingPin && string.IsNullOrEmpty(_currentPin))
            {
                await TryBiometricFirstAsync();
            }
        }

        private async Task UpdateTokenStatusAsync()
        {
            try
            {
                var authService = MauiProgram.Services?.GetService<Infrastructure.Auth.IAuthenticationService>();
                if (authService == null) return;

                var accessToken = await authService.GetAccessTokenAsync();
                var refreshToken = await authService.GetRefreshTokenAsync();

                // Сначала проверяем наличие валидного refresh token
                // Если он есть, токены будут автоматически обновлены при PIN/биометрии
                // Поэтому не показываем никаких предупреждений
                bool hasValidRefreshToken = !string.IsNullOrWhiteSpace(refreshToken) && 
                                            Infrastructure.Auth.JwtHelper.IsTokenValid(refreshToken);

                if (hasValidRefreshToken)
                {
                    // Есть валидный refresh token - токены будут обновлены автоматически
                    // Не показываем предупреждения, так как система автоматически обновит токены
                    TokenStatusMessage = string.Empty;
                    OnPropertyChanged(nameof(ShowTokenStatus));
                    System.Diagnostics.Debug.WriteLine("[PinLoginPage] Valid refresh token found, suppressing token expiration warnings");
                    return;
                }

                // Если нет валидного refresh token, проверяем статус access token
                if (_tokenStatus == "fresh")
                {
                    // Токены помечены как свежие AppShell
                    TokenStatusMessage = "Токены свежие. Подтвердите вход PIN";
                    TokenStatusColor = Color.FromArgb("#4CAF50"); // Зеленый
                    OnPropertyChanged(nameof(ShowTokenStatus));
                }
                else if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    var remainingMinutes = Infrastructure.Auth.JwtHelper.GetTokenRemainingMinutes(accessToken);
                    var isValid = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);

                    if (!isValid)
                    {
                        // Access token истек и нет валидного refresh token - нужен повторный вход
                        TokenStatusMessage = "Токены истекли. Требуется повторный вход";
                        TokenStatusColor = Color.FromArgb("#F44336"); // Красный
                    }
                    else if (remainingMinutes < 10)
                    {
                        // Access token скоро истечет и нет валидного refresh token
                        TokenStatusMessage = $"Токены истекут через {remainingMinutes} мин. Рекомендуется обновить";
                        TokenStatusColor = Color.FromArgb("#FF9800"); // Оранжевый
                    }
                    else
                    {
                        // Токены в норме
                        TokenStatusMessage = string.Empty;
                    }
                    OnPropertyChanged(nameof(ShowTokenStatus));
                }
                else if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    // Есть refresh token, но он невалиден, и нет access token
                    TokenStatusMessage = "Токены истекли. Требуется повторный вход";
                    TokenStatusColor = Color.FromArgb("#F44336"); // Красный
                    OnPropertyChanged(nameof(ShowTokenStatus));
                }
                else
                {
                    TokenStatusMessage = "Токены отсутствуют. Требуется авторизация";
                    TokenStatusColor = Color.FromArgb("#F44336"); // Красный
                    OnPropertyChanged(nameof(ShowTokenStatus));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Error updating token status: {ex.Message}");
            }
        }

        private async Task TryBiometricFirstAsync()
        {
            try
            {
                if (_authService != null)
                {
                    // Запрашиваем разрешения на биометрию (если доступно)
                    var biometricSuccess = await _authService.AuthenticateWithBiometricsAsync();
                    if (biometricSuccess)
                    {
                        // Биометрия успешна - проверяем токены и обновляем только если нужно
                        System.Diagnostics.Debug.WriteLine("[PinLoginPage] Biometric authentication successful, checking tokens");
                        var authService = MauiProgram.Services?.GetService<Infrastructure.Auth.IAuthenticationService>();
                        if (authService != null)
                        {
                            var accessToken = await authService.GetAccessTokenAsync();
                            var refreshToken = await authService.GetRefreshTokenAsync();
                            
                            // Если нет токенов - требуем перелогиниться
                            if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
                            {
                                System.Diagnostics.Debug.WriteLine("[PinLoginPage] No tokens found after biometric, redirecting to login");
                                await DisplayAlert(
                                    "Требуется повторный вход",
                                    "Для продолжения работы необходимо войти заново.",
                                    "OK");
                                
                                var pinService = new Services.PinStorageService();
                                await pinService.ClearPinAsync();
                                await authService.ClearTokensAsync();
                                AccountStore.Instance.SignOut();
                                await Shell.Current.GoToAsync("///login", animate: true);
                                return;
                            }
                            
                            // Всегда обновляем токены при успешной авторизации через биометрию
                            // Это продлевает refresh token на неделю каждый раз при открытии приложения
                            if (!string.IsNullOrWhiteSpace(refreshToken))
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine("[PinLoginPage] Ensuring valid tokens after biometric auth");
                                    
                                    // Используем GlobalAuthService для централизованного управления токенами
                                    var globalAuthService = MauiProgram.Services?.GetService<YessGoFront.Services.GlobalAuthService>();
                                    bool tokensValid = false;
                                    
                                    if (globalAuthService != null)
                                    {
                                        // Используем таймаут для обновления токенов (10 секунд)
                                        using var tokenCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                                        try
                                        {
                                            tokensValid = await globalAuthService.EnsureValidTokensAsync(tokenCts.Token);
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            System.Diagnostics.Debug.WriteLine("[PinLoginPage] Token refresh timed out after biometric auth");
                                            tokensValid = false;
                                        }
                                    }
                                    else if (_authService != null)
                                    {
                                        // Fallback на старый метод с таймаутом
                                        using var tokenCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                                        try
                                        {
                                            tokensValid = await _authService.RefreshTokenAsync(tokenCts.Token);
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            System.Diagnostics.Debug.WriteLine("[PinLoginPage] Token refresh timed out (fallback method)");
                                            tokensValid = false;
                                        }
                                    }
                                    
                                    if (tokensValid)
                                    {
                                        System.Diagnostics.Debug.WriteLine("[PinLoginPage] Tokens are valid after biometric auth");
                                        // Обновляем статус токенов после успешного обновления
                                        await UpdateTokenStatusAsync();
                                        await NavigateToMainAsync();
                                        return;
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("[PinLoginPage] Failed to ensure valid tokens after biometric, checking if access token is still valid");
                                        // Проверяем, может access token еще валиден и можно продолжить
                                        bool hasValidAccessToken = false;
                                        if (!string.IsNullOrWhiteSpace(accessToken))
                                        {
                                            hasValidAccessToken = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                                        }
                                        
                                        if (hasValidAccessToken)
                                        {
                                            System.Diagnostics.Debug.WriteLine("[PinLoginPage] Access token still valid, navigating to main despite refresh failure");
                                            // Обновляем статус токенов
                                            await UpdateTokenStatusAsync();
                                            await NavigateToMainAsync();
                                            return;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Error ensuring valid tokens after biometric: {ex.Message}");
                                    // Проверяем, может access token еще валиден и можно продолжить
                                    bool hasValidAccessToken = false;
                                    if (!string.IsNullOrWhiteSpace(accessToken))
                                    {
                                        hasValidAccessToken = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                                    }
                                    
                                    if (hasValidAccessToken)
                                    {
                                        System.Diagnostics.Debug.WriteLine("[PinLoginPage] Access token still valid, navigating to main despite refresh error");
                                        await NavigateToMainAsync();
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                // Нет refresh token - проверяем access token
                                bool hasValidAccessToken = false;
                                if (!string.IsNullOrWhiteSpace(accessToken))
                                {
                                    hasValidAccessToken = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                                }
                                
                                if (hasValidAccessToken)
                                {
                                    System.Diagnostics.Debug.WriteLine("[PinLoginPage] No refresh token but access token valid after biometric, navigating to main");
                                    await NavigateToMainAsync();
                                    return;
                                }
                                
                                // Нет refresh token и access token невалиден - требуем перелогиниться
                                System.Diagnostics.Debug.WriteLine("[PinLoginPage] No refresh token and access token invalid after biometric, redirecting to login");
                                await DisplayAlert(
                                    "Требуется повторный вход",
                                    "Сессия истекла. Пожалуйста, войдите заново.",
                                    "OK");
                                
                                var pinService2 = new Services.PinStorageService();
                                await pinService2.ClearPinAsync();
                                await authService.ClearTokensAsync();
                                AccountStore.Instance.SignOut();
                                await Shell.Current.GoToAsync("///login", animate: true);
                                return;
                            }
                        }
                    }
                }
                // Если биометрия недоступна или пользователь отменил - продолжаем с PIN
            }
            catch
            {
                // Игнорируем ошибки биометрии, продолжаем с PIN
            }
        }

        private void UpdatePinIndicators()
        {
            // Получаем индикаторы по имени из XAML
            var indicator1 = this.FindByName<Frame>("PinIndicator1");
            var indicator2 = this.FindByName<Frame>("PinIndicator2");
            var indicator3 = this.FindByName<Frame>("PinIndicator3");
            var indicator4 = this.FindByName<Frame>("PinIndicator4");

            if (indicator1 == null || indicator2 == null || indicator3 == null || indicator4 == null)
                return;

            var indicators = new[] { indicator1, indicator2, indicator3, indicator4 };
            var filledColor = Color.FromArgb("#0B4A3B");
            var emptyColor = Color.FromArgb("#E0EFE9");

            for (int i = 0; i < indicators.Length; i++)
            {
                if (i < _currentPin.Length)
                {
                    indicators[i].BackgroundColor = filledColor;
                }
                else
                {
                    indicators[i].BackgroundColor = emptyColor;
                }
            }

            OnPropertyChanged(nameof(CanDelete));
        }

        [RelayCommand]
        private void Number(string number)
        {
            // Проверяем длину ДО добавления цифры, чтобы не допустить ввод больше 4 цифр
            if (_currentPin.Length >= 4)
                return;

            // Добавляем цифру
            PinCode = _currentPin + number;
            
            // Автоматическая обработка при вводе 4 цифр (проверяем ПОСЛЕ добавления)
            if (_currentPin.Length == 4)
            {
                _ = ProcessPinAsync();
            }
        }

        [RelayCommand]
        private void Delete()
        {
            if (_currentPin.Length > 0)
            {
                PinCode = _currentPin.Substring(0, _currentPin.Length - 1);
                ClearError();
            }
        }

        private void OnPinCodeChanged(object? sender, TextChangedEventArgs e)
        {
            // Обновляем индикаторы при изменении через скрытое поле
            UpdatePinIndicators();
        }

        private async Task ProcessPinAsync()
        {
            if (_currentPin.Length != 4)
                return;

            System.Diagnostics.Debug.WriteLine($"[PinLoginPage] ProcessPinAsync: START - _isCreatingPin={_isCreatingPin}, _confirmPin={_confirmPin}, _currentPin={_currentPin}");
            
            // ЧЕТКАЯ ЛОГИКА определения режима:
            // 1. Если _confirmPin != null - мы в процессе создания (второй ввод) - ВСЕГДА создание
            // 2. Иначе проверяем наличие PIN в хранилище
            bool isActuallyCreating;
            
            if (_confirmPin != null)
            {
                // Второй ввод при создании - всегда режим создания
                isActuallyCreating = true;
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] ProcessPinAsync: _confirmPin is set → creation mode (second input)");
            }
            else
            {
                // Первый ввод - проверяем наличие PIN в хранилище
                bool hasPinInStorage = false;
                if (_authService != null)
                {
                    try
                    {
                        hasPinInStorage = await _authService.HasPinAsync();
                        System.Diagnostics.Debug.WriteLine($"[PinLoginPage] ProcessPinAsync: hasPinInStorage={hasPinInStorage}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PinLoginPage] ProcessPinAsync: Error checking PIN: {ex.Message}");
                        hasPinInStorage = false; // В случае ошибки считаем, что PIN-кода нет
                    }
                }
                
                // Если PIN нет в хранилище - режим создания, иначе - проверки
                isActuallyCreating = !hasPinInStorage;
                
                // Обновляем _isCreatingPin для будущих вызовов
                if (!hasPinInStorage)
                {
                    _isCreatingPin = true;
                }
                
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] ProcessPinAsync: hasPinInStorage={hasPinInStorage} → isActuallyCreating={isActuallyCreating}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[PinLoginPage] ProcessPinAsync: FINAL - isActuallyCreating={isActuallyCreating}");

            IsBusy = true;
            OnPropertyChanged(nameof(IsBusy));
            ClearError();

            try
            {
                await Task.Delay(300); // Небольшая задержка для UX

                if (isActuallyCreating)
                {
                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Processing PIN creation...");
                    await HandlePinCreationAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Processing PIN verification...");
                    await HandlePinVerificationAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError("Произошла ошибка. Попробуйте снова.");
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private async Task HandlePinCreationAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[PinLoginPage] HandlePinCreationAsync: _confirmPin={_confirmPin}, _currentPin={_currentPin}");
            
            if (_confirmPin == null)
            {
                // Первый ввод - сохраняем для подтверждения
                _confirmPin = _currentPin;
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] First PIN entered: {_confirmPin}");
                
                // Очищаем поле ввода через свойство, чтобы обновить UI
                PinCode = string.Empty;
                
                // Обновляем текст подзаголовка
                SubtitleText = "Подтвердите PIN-код";
                
                // Очищаем ошибки если были
                ClearError();
            }
            else
            {
                // Второй ввод - проверяем совпадение
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Second PIN entered: {_currentPin}, comparing with: {_confirmPin}");
                
                if (_currentPin == _confirmPin)
                {
                    // PIN совпадает - сохраняем
                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] PINs match, saving...");
                    
                    if (_authService != null)
                    {
                        await _authService.SavePinAsync(_currentPin);
                    }
                    else
                    {
                        // Fallback: сохраняем напрямую через SecureStorage
                        await Microsoft.Maui.Storage.SecureStorage.SetAsync("user_pin", _currentPin);
                    }

                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] PIN saved successfully, navigating to main...");
                    
                    // Очищаем состояние
                    _confirmPin = null;
                    PinCode = string.Empty;
                    ClearError();
                    
                    // Сразу переходим в приложение после успешного создания PIN
                    await NavigateToMainAsync();
                }
                else
                {
                    // PIN не совпадает
                    System.Diagnostics.Debug.WriteLine($"[PinLoginPage] PINs don't match! First: {_confirmPin}, Second: {_currentPin}");
                    
                    ShowError("PIN-коды не совпадают. Попробуйте снова.");
                    
                    // Сбрасываем состояние
                    _confirmPin = null;
                    PinCode = string.Empty;
                    
                    // Обновляем режим создания и UI
                    _isCreatingPin = true;
                    OnPropertyChanged(nameof(TitleText));
                    OnPropertyChanged(nameof(IsVerificationMode));
                    
                    // Обновляем текст подзаголовка
                    SubtitleText = "Придумайте 4-значный PIN-код для быстрого входа";
                }
            }
        }

        private async Task HandlePinVerificationAsync()
        {
            bool isValid = false;

            try
            {
                if (_authService != null)
                {
                    isValid = await _authService.ValidatePinAsync(_currentPin);
                }
                else
                {
                    // Fallback: проверяем напрямую через SecureStorage
                    var storedPin = await Microsoft.Maui.Storage.SecureStorage.GetAsync("user_pin");
                    isValid = storedPin == _currentPin;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Error validating PIN: {ex.Message}");
                ShowError("Ошибка при проверке PIN-кода. Попробуйте снова.");
                PinCode = string.Empty;
                return;
            }

            if (isValid)
            {
                // PIN верный - проверяем наличие токенов
                var authService = MauiProgram.Services?.GetService<Infrastructure.Auth.IAuthenticationService>();
                if (authService != null)
                {
                    var accessToken = await authService.GetAccessTokenAsync();
                    var refreshToken = await authService.GetRefreshTokenAsync();
                    
                    // Если нет ни access token, ни refresh token - нужно перелогиниться
                    if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
                    {
                        System.Diagnostics.Debug.WriteLine("[PinLoginPage] No tokens found, redirecting to login");
                        await DisplayAlert(
                            "Требуется повторный вход",
                            "Для продолжения работы необходимо войти заново.",
                            "OK");
                        
                        // Очищаем PIN и токены
                        var pinService = new Services.PinStorageService();
                        await pinService.ClearPinAsync();
                        await authService.ClearTokensAsync();
                        AccountStore.Instance.SignOut();
                        
                        // Переходим на страницу входа
                        await Shell.Current.GoToAsync("///login", animate: true);
                        return;
                    }
                    
                    // Всегда обновляем токены при успешной авторизации через PIN
                    // Это продлевает refresh token на неделю каждый раз при открытии приложения
                    if (!string.IsNullOrWhiteSpace(refreshToken))
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[PinLoginPage] Ensuring valid tokens after PIN verification");
                            
                            // Используем GlobalAuthService для централизованного управления токенами
                            var globalAuthService = MauiProgram.Services?.GetService<YessGoFront.Services.GlobalAuthService>();
                            bool tokensValid = false;
                            
                            if (globalAuthService != null)
                            {
                                // Используем таймаут для обновления токенов (10 секунд)
                                using var tokenCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                                try
                                {
                                    tokensValid = await globalAuthService.EnsureValidTokensAsync(tokenCts.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    System.Diagnostics.Debug.WriteLine("[PinLoginPage] Token refresh timed out after PIN verification");
                                    tokensValid = false;
                                }
                            }
                            else if (_authService != null)
                            {
                                // Fallback на старый метод с таймаутом
                                using var tokenCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                                try
                                {
                                    tokensValid = await _authService.RefreshTokenAsync(tokenCts.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    System.Diagnostics.Debug.WriteLine("[PinLoginPage] Token refresh timed out (fallback method)");
                                    tokensValid = false;
                                }
                            }
                            
                            if (tokensValid)
                            {
                                System.Diagnostics.Debug.WriteLine("[PinLoginPage] Tokens are valid after PIN verification");
                                // Обновляем статус токенов после успешного обновления
                                await UpdateTokenStatusAsync();
                                await NavigateToMainAsync();
                                return;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[PinLoginPage] Failed to ensure valid tokens, checking if access token is still valid");
                                // Проверяем, может access token еще валиден и можно продолжить
                                bool hasValidAccessToken = false;
                                if (!string.IsNullOrWhiteSpace(accessToken))
                                {
                                    hasValidAccessToken = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                                }
                                
                                if (hasValidAccessToken)
                                {
                                    System.Diagnostics.Debug.WriteLine("[PinLoginPage] Access token still valid, navigating to main despite refresh failure");
                                    // Обновляем статус токенов
                                    await UpdateTokenStatusAsync();
                                    await NavigateToMainAsync();
                                    return;
                                }
                                
                                // Если refresh не удался и access token тоже невалиден - требуем перелогиниться
                                System.Diagnostics.Debug.WriteLine("[PinLoginPage] Both refresh and access tokens invalid, redirecting to login");
                                await DisplayAlert(
                                    "Требуется повторный вход",
                                    "Сессия истекла. Пожалуйста, войдите заново.",
                                    "OK");
                                
                                // Очищаем PIN и токены
                                var pinService = new Services.PinStorageService();
                                await pinService.ClearPinAsync();
                                await authService.ClearTokensAsync();
                                AccountStore.Instance.SignOut();
                                
                                // Переходим на страницу входа
                                await Shell.Current.GoToAsync("///login", animate: true);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Error ensuring valid tokens: {ex.Message}");
                            
                            // Проверяем, может access token еще валиден и можно продолжить
                            bool hasValidAccessToken = false;
                            if (!string.IsNullOrWhiteSpace(accessToken))
                            {
                                hasValidAccessToken = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                            }
                            
                            if (hasValidAccessToken)
                            {
                                System.Diagnostics.Debug.WriteLine("[PinLoginPage] Access token still valid, navigating to main despite refresh error");
                                await NavigateToMainAsync();
                                return;
                            }
                            
                            // Если ошибка и access token невалиден - требуем перелогиниться
                            await DisplayAlert(
                                "Требуется повторный вход",
                                "Не удалось обновить сессию. Пожалуйста, войдите заново.",
                                "OK");
                            
                            // Очищаем PIN и токены
                            var pinService = new Services.PinStorageService();
                            await pinService.ClearPinAsync();
                            await authService.ClearTokensAsync();
                            AccountStore.Instance.SignOut();
                            
                            // Переходим на страницу входа
                            await Shell.Current.GoToAsync("///login", animate: true);
                            return;
                        }
                    }
                    else
                    {
                        // Нет refresh token - проверяем access token
                        bool hasValidAccessToken = false;
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            hasValidAccessToken = Infrastructure.Auth.JwtHelper.IsTokenValid(accessToken);
                        }
                        
                        if (hasValidAccessToken)
                        {
                            System.Diagnostics.Debug.WriteLine("[PinLoginPage] No refresh token but access token valid, navigating to main");
                            await NavigateToMainAsync();
                            return;
                        }
                        
                        // Нет refresh token и access token невалиден - требуем перелогиниться
                        System.Diagnostics.Debug.WriteLine("[PinLoginPage] No refresh token and access token invalid, redirecting to login");
                        await DisplayAlert(
                            "Требуется повторный вход",
                            "Сессия истекла. Пожалуйста, войдите заново.",
                            "OK");
                        
                        // Очищаем PIN и токены
                        var pinService = new Services.PinStorageService();
                        await pinService.ClearPinAsync();
                        await authService.ClearTokensAsync();
                        AccountStore.Instance.SignOut();
                        
                        // Переходим на страницу входа
                        await Shell.Current.GoToAsync("///login", animate: true);
                        return;
                    }
                }
                
                // Если authService недоступен, просто переходим (на случай ошибки)
                System.Diagnostics.Debug.WriteLine("[PinLoginPage] AuthService unavailable, navigating to main");
                await NavigateToMainAsync();
            }
            else
            {
                // Неверный PIN
                ShowError("Неверный PIN-код. Попробуйте снова.");
                PinCode = string.Empty;
                
                // Вибрация при ошибке (если доступна)
                try
                {
                    Microsoft.Maui.Devices.HapticFeedback.Default.Perform(Microsoft.Maui.Devices.HapticFeedbackType.Click);
                }
                catch { }
            }
        }

        private async Task NavigateToMainAsync()
        {
            try
            {
                // Переходим на главную страницу
                await Shell.Current.GoToAsync("///main/home", animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Navigation error: {ex.Message}");
                // Fallback навигация
                await Shell.Current.GoToAsync("//main/home", animate: true);
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }

        private void ClearError()
        {
            if (HasError)
            {
                HasError = false;
                ErrorMessage = string.Empty;
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        private async void OnForgotPinClicked(object? sender, EventArgs e)
        {
            try
            {
                // Показываем подтверждение
                var confirmed = await DisplayAlert(
                    "Забыли PIN?",
                    "Вы будете перенаправлены на страницу входа. PIN-код будет удалён.",
                    "Продолжить",
                    "Отмена");

                if (!confirmed)
                    return;

                // Удаляем PIN
                var pinService = new Services.PinStorageService();
                await pinService.ClearPinAsync();
                System.Diagnostics.Debug.WriteLine("[PinLoginPage] PIN cleared after 'Forgot PIN'");

                // Очищаем токен и данные аккаунта
                var authService = MauiProgram.Services?.GetService<Infrastructure.Auth.IAuthenticationService>();
                if (authService != null)
                {
                    await authService.ClearTokensAsync();
                }
                AccountStore.Instance.SignOut();

                // Переходим на страницу входа
                await Shell.Current.GoToAsync("///login", animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PinLoginPage] Error in OnForgotPinClicked: {ex.Message}");
                await DisplayAlert("Ошибка", "Не удалось выполнить операцию", "OK");
            }
        }
    }
}
