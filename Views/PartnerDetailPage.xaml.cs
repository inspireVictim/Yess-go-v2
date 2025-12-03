using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using YessGoFront.Models;
using YessGoFront.Services.Domain;
using Microsoft.Extensions.DependencyInjection;
using YessGoFront.Converters;
#if ANDROID
using Android.Util;
#endif

namespace YessGoFront.Views
{
    [QueryProperty(nameof(PartnerId), "partnerId")]
    public partial class PartnerDetailPage : ContentPage
    {
        private string? partnerId;
        private IPartnersService? _partnersService;
        private PartnerDetailDto? _currentPartner;

        public string? PartnerId
        {
            get => partnerId;
            set
            {
                partnerId = value;
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] PartnerId set to: '{partnerId}'");
                if (!string.IsNullOrWhiteSpace(partnerId))
                {
                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Loading partner with ID: '{partnerId}'");
                    LoadPartner(partnerId);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] PartnerId is null or empty, not loading partner");
                }
            }
        }

        public PartnerDetailPage()
        {
            InitializeComponent();
            // Получаем сервис из DI
            _partnersService = MauiProgram.Services.GetService<IPartnersService>();
        }

        private async void LoadPartner(string id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] LoadPartner called with id: '{id}'");
#if ANDROID
                Android.Util.Log.Info("PartnerDetailPage", $"[LoadPartner] Загрузка партнёра с ID: {id}");
#endif
                
                if (_partnersService != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Calling GetPartnerByIdAsync with id: '{id}'");
#if ANDROID
                    Android.Util.Log.Info("PartnerDetailPage", $"[LoadPartner] Вызов GetPartnerByIdAsync для ID: {id}");
#endif
                    
                    var partner = await _partnersService.GetPartnerByIdAsync(id);
                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] GetPartnerByIdAsync returned: {(partner != null ? $"Partner '{partner.Name}' (Id: {partner.Id})" : "null")}");
#if ANDROID
                    Android.Util.Log.Info("PartnerDetailPage", $"[LoadPartner] Партнёр получен: {(partner != null ? partner.Name : "null")}");
#endif
                    
                    if (partner != null)
                    {
                        // Обновляем UI на главном потоке
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            PartnerName.Text = partner.Name ?? "Партнёр";
                            
                            // Отображаем категории
                            if (partner.Categories != null && partner.Categories.Count > 0)
                            {
                                var categoryNames = partner.Categories.Select(c => c.Name).ToList();
                                PartnerCategory.Text = $"Категории: {string.Join(", ", categoryNames)}";
                            }
                            else if (!string.IsNullOrWhiteSpace(partner.Category))
                            {
                                PartnerCategory.Text = $"Категория: {partner.Category}";
                            }
                            else
                            {
                                PartnerCategory.Text = "Категория: Не указана";
                            }
                            
                            // Описание - показываем только если оно есть
                            System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Description: '{partner.Description}'");
                            if (!string.IsNullOrWhiteSpace(partner.Description))
                            {
                                PartnerDescription.Text = partner.Description;
                                PartnerDescription.IsVisible = true;
                                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Описание отображается: {partner.Description.Substring(0, Math.Min(50, partner.Description.Length))}...");
                            }
                            else
                            {
                                PartnerDescription.IsVisible = false;
                                System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] Описание пустое или null, скрываем поле");
                            }
                            
                            // Кэшбэк и скидки
                            CashbackLabel.Text = $"{partner.CashbackRate ?? partner.DefaultCashbackRate:F0}%";
                            DiscountLabel.Text = $"{partner.MaxDiscountPercent ?? 10:F0}%";
                            
                            // Контакты
                            PartnerPhone.Text = partner.Phone ?? "Не указан";
                            PartnerWebsite.Text = partner.Website ?? "Не указан";
                            PartnerAddress.Text = partner.Address ?? "Не указан";
                            
                            // Скрываем адрес, если он не указан
                            bool hasAddress = !string.IsNullOrWhiteSpace(partner.Address);
                            AddressContainer.IsVisible = hasAddress;
                            AddressSeparator.IsVisible = hasAddress;
                        });
                        
                        // Загружаем логотип или показываем текст
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] ===== ЗАГРУЗКА ЛОГОТИПА =====");
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] LogoUrl: '{partner.LogoUrl}'");
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] LogoUrl is null: {partner.LogoUrl == null}");
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] LogoUrl is empty: {string.IsNullOrEmpty(partner.LogoUrl)}");
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] LogoUrl is whitespace: {string.IsNullOrWhiteSpace(partner.LogoUrl)}");
                        
                        // Получаем ссылки на элементы
                        var logoText = this.FindByName("LogoText") as Label;
                        var logoFrame = this.FindByName("LogoFrame") as Frame;
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] logoText found: {logoText != null}");
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] logoFrame found: {logoFrame != null}");
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] PartnerLogo found: {PartnerLogo != null}");
                        
                        // Нормализуем URL логотипа перед загрузкой
                        string? logoUrl = partner.LogoUrl?.Trim();
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
                                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Нормализован URL логотипа: '{partner.LogoUrl}' -> '{logoUrl}'");
                            }
                        }
                        
                        if (!string.IsNullOrWhiteSpace(logoUrl))
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Вызываем конвертер для: '{logoUrl}'");
                                var converter = new StringToImageSourceConverter();
                                var imageSource = converter.Convert(logoUrl, typeof(ImageSource), null, System.Globalization.CultureInfo.CurrentCulture) as ImageSource;
                                
                                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Конвертер вернул: {(imageSource != null ? "ImageSource" : "null")}");
                                
                                if (imageSource != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] ✅ Устанавливаем логотип: {logoUrl}");
                                    
                                    // Устанавливаем на главном потоке
                                    await MainThread.InvokeOnMainThreadAsync(() =>
                                    {
                                        PartnerLogo.Source = imageSource;
                                        PartnerLogo.IsVisible = true;
                                        
                                        // Скрываем текст
                                        if (logoText != null)
                                        {
                                            logoText.IsVisible = false;
                                        }
                                        
                                        // Убеждаемся, что Frame видим
                                        if (logoFrame != null)
                                        {
                                            logoFrame.IsVisible = true;
                                        }
                                    });
                                    
                                    System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] ✅ Логотип установлен и видим");
                                }
                                else
                                {
                                    // Если конвертер вернул null, показываем текст
                                    System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] ❌ Конвертер вернул null для логотипа, показываем текст");
                                    await MainThread.InvokeOnMainThreadAsync(() =>
                                    {
                                        ShowLogoText(partner.Name, logoText, logoFrame);
                                        PartnerLogo.IsVisible = false;
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] ❌ Ошибка загрузки логотипа: {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Stack trace: {ex.StackTrace}");
                                // При ошибке показываем текст
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    ShowLogoText(partner.Name, logoText, logoFrame);
                                    PartnerLogo.IsVisible = false;
                                });
                            }
                        }
                        else
                        {
                            // Если URL логотипа пустой, показываем текст
                            System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] LogoUrl пустой, показываем текст");
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                ShowLogoText(partner.Name, logoText, logoFrame);
                                PartnerLogo.IsVisible = false;
                            });
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] ===== ЗАГРУЗКА ЛОГОТИПА ЗАВЕРШЕНА =====");
                        
                        // Загружаем обложку
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] ===== ЗАГРУЗКА ОБЛОЖКИ =====");
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] CoverImageUrl: '{partner.CoverImageUrl}'");
                        
                        // Нормализуем URL обложки перед загрузкой
                        string? coverImageUrl = partner.CoverImageUrl?.Trim();
                        if (!string.IsNullOrWhiteSpace(coverImageUrl))
                        {
                            // Если URL не начинается с http/https, но начинается с /, это относительный путь
                            if (!coverImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                !coverImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                                !coverImageUrl.StartsWith("/"))
                            {
                                // Если URL не начинается с /, добавляем его
                                coverImageUrl = "/" + coverImageUrl.TrimStart('/');
                                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Нормализован URL обложки: '{partner.CoverImageUrl}' -> '{coverImageUrl}'");
                            }
                            
                            try
                            {
                                var converter = new StringToImageSourceConverter();
                                var coverImageSource = converter.Convert(coverImageUrl, typeof(ImageSource), null, System.Globalization.CultureInfo.CurrentCulture) as ImageSource;
                                
                                if (coverImageSource != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] ✅ Обложка установлена: {coverImageUrl}");
                                    
                                    // Устанавливаем на главном потоке
                                    await MainThread.InvokeOnMainThreadAsync(() =>
                                    {
                                        PartnerCoverImage.Source = coverImageSource;
                                        PartnerCoverImage.IsVisible = true;
                                    });
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] ❌ Конвертер вернул null для обложки");
                                    await MainThread.InvokeOnMainThreadAsync(() =>
                                    {
                                        PartnerCoverImage.IsVisible = false;
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] ❌ Ошибка загрузки обложки: {ex.Message}");
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    PartnerCoverImage.IsVisible = false;
                                });
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] CoverImageUrl пустой");
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                PartnerCoverImage.IsVisible = false;
                            });
                        }
                        
                        // Сохраняем данные партнёра для кнопок
                        _currentPartner = partner;
                        
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Загружен партнёр: {partner.Name}");
                    }
                    else
                    {
                        PartnerName.Text = $"Партнёр №{id}";
                        PartnerCategory.Text = "Категория: Не указана";
                        PartnerDescription.Text = "Информация о партнёре не найдена.";
                        var logoText = this.FindByName("LogoText") as Label;
                        var logoFrame = this.FindByName("LogoFrame") as Frame;
                        ShowLogoText($"Партнёр №{id}", logoText, logoFrame);
                        PartnerLogo.IsVisible = false;
                    }
                }
                else
                {
                    // Fallback, если сервис недоступен
                    PartnerName.Text = $"Партнёр №{id}";
                    PartnerCategory.Text = "Категория: Еда и напитки";
                    PartnerDescription.Text = "Описание партнёра, информация о скидках, адрес и контакты.";
                    var logoText = this.FindByName("LogoText") as Label;
                    var logoFrame = this.FindByName("LogoFrame") as Frame;
                    ShowLogoText($"Партнёр №{id}", logoText, logoFrame);
                    PartnerLogo.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Ошибка загрузки партнёра: {ex.Message}");
                PartnerName.Text = $"Партнёр №{id}";
                PartnerCategory.Text = "Ошибка загрузки";
                PartnerDescription.Text = "Не удалось загрузить информацию о партнёре.";
                var logoText = this.FindByName("LogoText") as Label;
                var logoFrame = this.FindByName("LogoFrame") as Frame;
                ShowLogoText($"Партнёр №{id}", logoText, logoFrame);
                PartnerLogo.IsVisible = false;
            }
        }

        private void ShowLogoText(string partnerName, Label? logoText, Frame? logoFrame)
        {
            if (logoText != null)
            {
                // Берем первые буквы имени партнёра (максимум 10 символов) и делаем заглавными
                var text = string.IsNullOrWhiteSpace(partnerName) 
                    ? "?" 
                    : (partnerName.Length > 10 
                        ? partnerName.Substring(0, 10).ToUpper() 
                        : partnerName.ToUpper());
                
                logoText.Text = text;
                logoText.IsVisible = true;
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Показываем текст логотипа: '{text}'");
            }
            
            // Убеждаемся, что Frame видим
            if (logoFrame != null)
            {
                logoFrame.IsVisible = true;
                System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] LogoFrame установлен как видимый для текста");
            }
        }

        private async void OnMapClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] === OnMapClicked НАЧАЛО ===");
            
            try
            {
                // Отключаем кнопку на время навигации
                if (sender is Button button)
                {
                    button.IsEnabled = false;
                }

                System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] Начало навигации к MapPage...");
                
                // Навигация к нашей внутренней MapPage
                await Shell.Current.GoToAsync("//MapPage", animate: true);
                
                System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] Навигация завершена успешно");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] КРИТИЧЕСКАЯ ОШИБКА навигации: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Inner exception: {ex.InnerException?.Message}");
                
                // Показываем детальную информацию об ошибке
                var errorMessage = $"Не удалось открыть карту.\n\nОшибка: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nДетали: {ex.InnerException.Message}";
                }
                
                await DisplayAlert("Ошибка", errorMessage, "OK");
            }
            finally
            {
                // Включаем кнопку обратно
                if (sender is Button button)
                {
                    button.IsEnabled = true;
                }
                System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] === OnMapClicked ЗАВЕРШЕНО ===");
            }
        }

        private async void OnCallClicked(object sender, EventArgs e)
        {
            if (_currentPartner != null && !string.IsNullOrWhiteSpace(_currentPartner.Phone))
            {
                try
                {
                    PhoneDialer.Open(_currentPartner.Phone);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось совершить звонок: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Информация", "Телефон партнёра не указан.", "OK");
            }
        }

        private async void OnWebsiteTapped(object sender, EventArgs e)
        {
            if (_currentPartner != null && !string.IsNullOrWhiteSpace(_currentPartner.Website))
            {
                try
                {
                    var uri = _currentPartner.Website;
                    if (!uri.StartsWith("http://") && !uri.StartsWith("https://"))
                    {
                        uri = "https://" + uri;
                    }
                    await Launcher.OpenAsync(new Uri(uri));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть сайт: {ex.Message}", "OK");
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] Кнопка 'Назад' нажата");
            
            // Отключаем кнопку на время навигации, чтобы избежать двойных нажатий
            if (BackButton != null)
            {
                BackButton.IsEnabled = false;
            }
            
            try
            {
                // Возвращаемся на главную страницу (откуда обычно открывается страница партнёра)
                await Shell.Current.GoToAsync("///main/home", animate: true);
                System.Diagnostics.Debug.WriteLine("[PartnerDetailPage] Успешно вернулись на главную страницу");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Ошибка навигации: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Stack trace: {ex.StackTrace}");
                
                // Попытка использовать альтернативный маршрут
                try
                {
                    await Shell.Current.GoToAsync("//main/home", animate: true);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
                }
            }
            finally
            {
                // Включаем кнопку обратно
                if (BackButton != null)
                {
                    BackButton.IsEnabled = true;
                }
            }
        }

        private async void OnViewProductsClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(partnerId))
            {
                await DisplayAlert("Ошибка", "Не удалось определить партнёра", "OK");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Переход к продуктам партнёра: {partnerId}");
                var route = $"PartnerDetailViewPage?partnerId={Uri.EscapeDataString(partnerId)}";
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Навигация к: {route}");
                await Shell.Current.GoToAsync(route, animate: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Ошибка навигации к продуктам: {ex.Message}");
                await DisplayAlert("Ошибка", "Не удалось открыть каталог продуктов", "OK");
            }
        }
    }
}
