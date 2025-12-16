using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YessGoFront.Services.Domain;
using YessGoFront.ViewModels;
using YessGoFront.Services;

namespace YessGoFront.Views
{
    [QueryProperty(nameof(ReferralCode), "ref")]
    public partial class RegisterPage : ContentPage
    {
        private readonly RegisterViewModel _viewModel;
        private readonly AuthNavigationHandler _authNavigationHandler;
        private bool _acknowledged;
        private string? _pendingReferralCode; // Временное хранение реферального кода до инициализации ViewModel
        private bool _isNavigating = false; // Защита от повторных вызовов навигации
        private readonly SemaphoreSlim _actionLock = new(1, 1);
        
        // Реферальный код из URL параметра
        public string? ReferralCode
        {
            get => _viewModel?.ReferralCode;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _pendingReferralCode = value;
                    System.Diagnostics.Debug.WriteLine($"[RegisterPage] Referral code from URL: {value}");
                    
                    // Если ViewModel уже инициализирован, устанавливаем значение сразу
                    if (_viewModel != null)
                    {
                        _viewModel.ReferralCode = value;
                        _pendingReferralCode = null;
                    }
                }
            }
        }


        //Проверка, что пользователь согласен с условиями
        public bool Acknowledged
        {
            get => _acknowledged;
            set
            {
                _acknowledged = value;
                OnPropertyChanged();
            }
        }

        public RegisterPage()
        {
            InitializeComponent();

            // Получаем сервисы через DI
            var authService = MauiProgram.Services.GetRequiredService<IAuthService>();
            var logger = MauiProgram.Services.GetService<Microsoft.Extensions.Logging.ILogger<RegisterViewModel>>();
            _authNavigationHandler = MauiProgram.Services.GetRequiredService<AuthNavigationHandler>();

            _viewModel = new RegisterViewModel(authService, logger);
            BindingContext = _viewModel;

            // Подписываемся на событие успешной регистрации
            _viewModel.OnRegisterSuccess += OnRegisterSuccess;
            
            // Если реферальный код был установлен до инициализации ViewModel, устанавливаем его сейчас
            if (!string.IsNullOrWhiteSpace(_pendingReferralCode))
            {
                _viewModel.ReferralCode = _pendingReferralCode;
                _pendingReferralCode = null;
                System.Diagnostics.Debug.WriteLine($"[RegisterPage] Referral code applied to ViewModel: {_viewModel.ReferralCode}");
            }
        }

        private async Task OnRegisterSuccess(Services.Api.AuthResponse response)
        {
            // Защита от повторных вызовов навигации
            if (_isNavigating)
            {
                System.Diagnostics.Debug.WriteLine("[RegisterPage] Navigation already in progress, ignoring duplicate call");
                return;
            }

            try
            {
                // Проверка на null response
                if (response == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RegisterPage] ERROR: Registration response is null");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("Ошибка", "Получен пустой ответ от сервера. Попробуйте зарегистрироваться снова.", "OK");
                    });
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[RegisterPage] Registration success! UserId: {response.UserId}");

                // Сохраняем данные пользователя в AccountStore
                if (response.User != null)
                {
                    var accountStore = AccountStore.Instance;
                    accountStore.SignIn(
                        email: response.User.Email ?? string.Empty,
                        firstName: response.User.FirstName ?? string.Empty,
                        lastName: response.User.LastName ?? string.Empty,
                        remember: true,
                        phone: response.User.Phone ?? string.Empty
                    );
                    System.Diagnostics.Debug.WriteLine($"[RegisterPage] AccountStore updated. UserId: {response.UserId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[RegisterPage] WARNING: response.User is null");
                }

                // Проверяем, есть ли PIN-код с таймаутом
                var domainAuthService = MauiProgram.Services.GetRequiredService<Services.Domain.IAuthService>();
                if (domainAuthService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RegisterPage] ERROR: IAuthService is null");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("Ошибка", "Сервис авторизации недоступен. Перезапустите приложение.", "OK");
                    });
                    return;
                }

                bool hasPin = false;
                try
                {
                    // Используем таймаут для проверки PIN (10 секунд)
                    // HasPinAsync не принимает CancellationToken, поэтому используем Task.WhenAny с таймаутом
                    var pinTask = domainAuthService.HasPinAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    var completedTask = await Task.WhenAny(pinTask, timeoutTask);
                    
                    if (completedTask == pinTask)
                    {
                        hasPin = await pinTask;
                        System.Diagnostics.Debug.WriteLine($"[RegisterPage] HasPin: {hasPin}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[RegisterPage] HasPinAsync timed out, assuming no PIN");
                        hasPin = false; // При таймауте предполагаем, что PIN нет
                    }
                }
                catch (Exception pinEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[RegisterPage] Error checking PIN: {pinEx.Message}");
                    hasPin = false; // При ошибке предполагаем, что PIN нет
                }

                // Навигацию делаем на главном потоке
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (_isNavigating)
                    {
                        System.Diagnostics.Debug.WriteLine("[RegisterPage] Navigation already in progress, skipping");
                        return;
                    }

                    _isNavigating = true;
                    try
                    {
                        var shell = Shell.Current;
                        if (shell == null)
                        {
                            System.Diagnostics.Debug.WriteLine("[RegisterPage] ERROR: Shell.Current is null");
                            await DisplayAlert("Ошибка", "Не удалось выполнить навигацию. Перезапустите приложение.", "OK");
                            return;
                        }

                        // Если PIN-кода нет - переходим на страницу создания PIN
                        if (!hasPin)
                        {
                            System.Diagnostics.Debug.WriteLine("[RegisterPage] No PIN found, navigating to PIN creation page");
                            await shell.GoToAsync("///pinlogin?isCreatingPin=true", animate: true);
                        }
                        else
                        {
                            // Если PIN-код есть - переходим на главную страницу
                            System.Diagnostics.Debug.WriteLine("[RegisterPage] PIN exists, navigating to main/home");
                            await shell.GoToAsync("///main/home", animate: true);
                        }
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RegisterPage] Navigation error: {navEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[RegisterPage] Stack trace: {navEx.StackTrace}");
                        
                        // Показываем ошибку пользователю
                        try
                        {
                            await DisplayAlert("Ошибка навигации", 
                                $"Не удалось перейти на следующий экран: {navEx.Message}", "OK");
                        }
                        catch
                        {
                            // Игнорируем ошибки при показе алерта
                        }
                    }
                    finally
                    {
                        _isNavigating = false;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegisterPage] Error in OnRegisterSuccess: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RegisterPage] Stack trace: {ex.StackTrace}");
            }
        }

        private async void OpenLogin_Tapped(object? sender, EventArgs e)
        {
            if (!await _actionLock.WaitAsync(0))
                return;

            try
            {
                await OpenLogin_TappedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegisterPage] Error in OpenLogin_Tapped: {ex.Message}");
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private async Task OpenLogin_TappedAsync()
        {
            await Shell.Current.GoToAsync("///login");
        }

        private void TogglePassword_Tapped(object? sender, EventArgs e)
        {
            // Оптимизировано: используем прямое обращение к элементу через x:Name
            if (PasswordEntry != null)
            {
                PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            }
        }

        private void ToggleConfirmPassword_Tapped(object? sender, EventArgs e)
        {
            // Оптимизировано: используем прямое обращение к элементу через x:Name
            if (ConfirmPasswordEntry != null)
            {
                ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
            }
        }

        private async void OnPolicyLinkTapped(object? sender, EventArgs e)
        {
            if (!await _actionLock.WaitAsync(0))
                return;

            try
            {
                await OnPolicyLinkTappedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegisterPage] Error in OnPolicyLinkTapped: {ex.Message}");
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private async Task OnPolicyLinkTappedAsync()
        {
            const string policyText = @"ПОЛИТИКА КОНФИДЕНЦИАЛЬНОСТИ

Приложения YESS!GO

Последнее обновление: [укажи дату]

Настоящая Политика конфиденциальности определяет порядок, в котором приложение YESS!GO (далее — «Мы», «Нам», «Приложение», «Сервис») собирает, использует, хранит и защищает информацию пользователей.

Используя приложение YESS!GO, вы подтверждаете своё согласие с условиями данной Политики.

1. Какие данные мы собираем

1.1. Персональные данные

Мы можем собирать информацию, которую пользователь предоставляет самостоятельно:
• Имя и фамилия
• Номер телефона
• Адрес электронной почты
• Данные профиля
• Данные, указанные при регистрации и в процессе использования приложения

1.2. Технические данные

Мы автоматически получаем следующие данные:
• IP-адрес
• Тип устройства и операционной системы
• Идентификаторы устройства
• Логи работы приложения (ошибки, время входа, действия)
• Файлы cookie и аналогичные технологии

1.3. Данные об использовании Сервиса

• Информация о заказах, действиях в приложении
• История транзакций (если применимо)
• Данные о местоположении (если пользователь дал разрешение)

2. Для чего мы используем данные

Мы можем использовать собранные данные для:
• Регистрации и авторизации пользователя
• Предоставления основного функционала приложения
• Обработки запросов и улучшения сервиса
• Поддержки и техподдержки пользователей
• Отправки уведомлений
• Аналитики (анонимизированные данные)
• Соблюдения требований законодательства
• Защиты от мошенничества и обеспечения безопасности

3. Передача данных третьим лицам

Мы не продаём ваши персональные данные.

Данные могут передаваться:
• Платёжным и сервисным провайдерам
• Хостинг-платформам и службам хранения данных
• Аналитическим сервисам
• Партнёрским сервисам, участвующим в работе приложения
• Государственным органам — только по законному запросу

Все такие сервисы обязуются соблюдать конфиденциальность и защищать данные пользователей.

4. Хранение и защита данных

Мы применяем современные механизмы защиты:
• Шифрование
• Контроль доступа
• Защищённые серверы
• Регулярные проверки безопасности

Мы стремимся максимально защищать ваши данные, однако ни один метод передачи данных в интернете не может гарантировать абсолютную безопасность.

5. Срок хранения данных

Мы храним данные:
• Пока действует учетная запись пользователя
• Пока это необходимо для работы сервиса
• Пока это требуется законодательством

Пользователь имеет право запросить удаление данных.

6. Права пользователей

Вы имеете право:
• Получить доступ к своим данным
• Исправить неточные данные
• Запросить удаление данных
• Ограничить обработку
• Отозвать согласие на обработку данных
• Отключить уведомления и cookie

Связаться с нами можно по адресу:
📧 [укажите вашу почту поддержки]

7. Cookies и аналогичные технологии

Мы используем cookie для:
• Авторизации
• Улучшения работы приложения
• Аналитики
• Запоминания пользовательских настроек

Вы можете отключить cookie в настройках устройства или браузера.

8. Политика в отношении детей

Приложение YESS!GO не предназначено для использования лицами младше 13/16/18 лет (выберите подходящий возраст в зависимости от законодательства вашей страны).

Мы не собираем данные детей намеренно. Если вы считаете, что ребёнок предоставил нам данные — свяжитесь с нами для удаления.

9. Обновление Политики

Мы можем периодически обновлять данную Политику. Дата последнего обновления указана в начале документа.

О значимых изменениях мы уведомим через:
• уведомление в приложении,
• сайт,
• либо по электронной почте.

Продолжая использовать приложение после изменений, вы соглашаетесь с обновлённой Политикой.";

            // Создаем модальную страницу с текстом политики
            var policyPage = new ContentPage
            {
                Title = "Политика конфиденциальности",
                BackgroundColor = Colors.White,
                Padding = new Thickness(0)
            };

            var scrollView = new ScrollView();
            var stackLayout = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 12
            };

            // Кнопка закрытия
            var closeButton = new Button
            {
                Text = "Закрыть",
                BackgroundColor = Color.FromArgb("#007A51"),
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 44,
                Margin = new Thickness(0, 0, 0, 16)
            };
            closeButton.Clicked += async (s, args) => await Navigation.PopModalAsync();
            stackLayout.Children.Add(closeButton);

            // Текст политики
            var policyLabel = new Label
            {
                Text = policyText,
                FontSize = 14,
                TextColor = Colors.Black,
                LineBreakMode = LineBreakMode.WordWrap
            };
            stackLayout.Children.Add(policyLabel);

            scrollView.Content = stackLayout;
            policyPage.Content = scrollView;

            // Открываем модальную страницу
            await Navigation.PushModalAsync(policyPage);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.OnRegisterSuccess -= OnRegisterSuccess;
        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {

        }

        public async void OnLoginsPage(object sender, EventArgs e)
        {
            if (!await _actionLock.WaitAsync(0))
                return;

            try
            {
                await OnLoginsPageAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegisterPage] Error in OnLoginsPage: {ex.Message}");
            }
            finally
            {
                _actionLock.Release();
            }
        }

        private async Task OnLoginsPageAsync()
        {
            await Shell.Current.GoToAsync("///login");
        }
    }
}
