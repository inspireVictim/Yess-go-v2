using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using YessGoFront.Data.Entities;
using YessGoFront.Infrastructure.Exceptions;
using YessGoFront.Models;
using YessGoFront.Services;
using YessGoFront.Services.Api;
using YessGoFront.Services.Domain;

namespace YessGoFront.Views;

public partial class Profile : ContentPage
{
    private readonly IAuthService? _authService;
    private readonly IAuthApiService? _authApiService;
    private UserDto? _currentUser;
    private bool _isAppearing = false;
    private readonly SemaphoreSlim _actionLock = new(1, 1);

    public Profile()
    {
        InitializeComponent();
        
        _authService = MauiProgram.Services?.GetService<IAuthService>();
        _authApiService = MauiProgram.Services?.GetService<IAuthApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isAppearing)
            return;

        _isAppearing = true;
        try
        {
            await OnAppearingAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile] Error in OnAppearing: {ex.Message}");
        }
        finally
        {
            _isAppearing = false;
        }
    }

    protected virtual async Task OnAppearingAsync()
    {
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            if (_authService == null || _authApiService == null)
            {
                await DisplayAlert("Ошибка", "Сервисы не инициализированы", "OK");
                return;
            }

            // Показываем индикатор загрузки
            SaveButton.IsEnabled = false;
            SaveButton.Text = "Загрузка...";

            // Сначала пытаемся загрузить профиль из API
            try
            {
                _currentUser = await _authApiService.GetMeAsync();
                
                if (_currentUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Profile] ✅ Profile loaded from API: FirstName={_currentUser.FirstName}, LastName={_currentUser.LastName}");
                    FillProfileFields(_currentUser);
                    return;
                }
            }
            catch (UnauthorizedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profile] ⚠️ API returned 401, loading from local DB: {ex.Message}");
                // Продолжаем загрузку из локальной БД
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profile] ⚠️ API error, trying local DB: {ex.Message}");
                // Продолжаем загрузку из локальной БД
            }

            // Если API не сработал, загружаем из локальной БД
            var localUser = await _authService.GetLocalUserAsync();
            if (localUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"[Profile] ✅ Loading profile from local DB: Name={localUser.Name}, Phone={localUser.Phone}");
                _currentUser = ConvertLocalUserToDto(localUser);
                FillProfileFields(_currentUser);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[Profile] ❌ No local user found");
                await DisplayAlert("Ошибка", "Не удалось загрузить профиль. Пожалуйста, войдите в систему заново.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile] Error loading profile: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Profile] StackTrace: {ex.StackTrace}");
            await DisplayAlert("Ошибка", $"Не удалось загрузить профиль: {ex.Message}", "OK");
        }
        finally
        {
            SaveButton.IsEnabled = true;
            SaveButton.Text = "Сохранить изменения";
        }
    }

    private void FillProfileFields(UserDto user)
    {
        var firstNameEntry = NameScopeExtensions.FindByName<Entry>(this, "FirstNameEntry");
        var lastNameEntry = NameScopeExtensions.FindByName<Entry>(this, "LastNameEntry");
        var phoneEntry = NameScopeExtensions.FindByName<Entry>(this, "PhoneEntry");
        var emailEntry = NameScopeExtensions.FindByName<Entry>(this, "EmailEntry");
        var passwordEntry = NameScopeExtensions.FindByName<Entry>(this, "PasswordEntry");

        if (firstNameEntry != null)
            firstNameEntry.Text = user.FirstName ?? string.Empty;
        
        if (lastNameEntry != null)
            lastNameEntry.Text = user.LastName ?? string.Empty;
        
        if (phoneEntry != null)
            phoneEntry.Text = user.Phone ?? string.Empty;
        
        if (emailEntry != null)
            emailEntry.Text = user.Email ?? string.Empty;
        
        if (passwordEntry != null)
            passwordEntry.Text = string.Empty; // Пароль не показываем
    }

    private UserDto ConvertLocalUserToDto(User localUser)
    {
        // Парсим Name на FirstName и LastName
        var nameParts = localUser.Name?.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        return new UserDto
        {
            Id = localUser.Id,
            FirstName = firstName,
            LastName = lastName,
            Phone = localUser.Phone ?? string.Empty,
            Email = localUser.Email,
            CityId = localUser.CityId,
            ReferralCode = localUser.ReferralCode,
            CreatedAt = localUser.CreatedAt
        };
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!await _actionLock.WaitAsync(0))
            return;

        try
        {
            if (sender is Button btn)
                btn.IsEnabled = false;

            await OnSaveClickedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile] Error in OnSaveClicked: {ex.Message}");
        }
        finally
        {
            if (sender is Button btn)
                btn.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnSaveClickedAsync()
    {
        try
        {
            if (_authApiService == null)
            {
                await DisplayAlert("Ошибка", "Сервис не инициализирован", "OK");
                return;
            }

            var firstNameEntry = NameScopeExtensions.FindByName<Entry>(this, "FirstNameEntry");
            var lastNameEntry = NameScopeExtensions.FindByName<Entry>(this, "LastNameEntry");
            var phoneEntry = NameScopeExtensions.FindByName<Entry>(this, "PhoneEntry");
            var emailEntry = NameScopeExtensions.FindByName<Entry>(this, "EmailEntry");
            var passwordEntry = NameScopeExtensions.FindByName<Entry>(this, "PasswordEntry");

            if (firstNameEntry == null || lastNameEntry == null || phoneEntry == null || 
                emailEntry == null || passwordEntry == null)
            {
                await DisplayAlert("Ошибка", "Не удалось найти поля ввода", "OK");
                return;
            }

            // Валидация
            if (string.IsNullOrWhiteSpace(firstNameEntry.Text))
            {
                await DisplayAlert("Ошибка", "Имя не может быть пустым", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(lastNameEntry.Text))
            {
                await DisplayAlert("Ошибка", "Фамилия не может быть пустой", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(phoneEntry.Text))
            {
                await DisplayAlert("Ошибка", "Номер телефона не может быть пустым", "OK");
                return;
            }

            // Показываем индикатор загрузки
            SaveButton.IsEnabled = false;
            SaveButton.Text = "Сохранение...";

            // Формируем запрос на обновление
            var updateRequest = new UpdateProfileRequest
            {
                FirstName = firstNameEntry.Text.Trim(),
                LastName = lastNameEntry.Text.Trim(),
                Phone = phoneEntry.Text.Trim(),
                Email = string.IsNullOrWhiteSpace(emailEntry.Text) ? null : emailEntry.Text.Trim(),
                Password = string.IsNullOrWhiteSpace(passwordEntry.Text) ? null : passwordEntry.Text
            };

            // Отправляем запрос на сервер
            var updatedUser = await _authApiService.UpdateProfileAsync(updateRequest);

            if (updatedUser != null)
            {
                // Обновляем локальный профиль через AuthService
                if (_authService != null)
                {
                    await _authService.GetUserProfileAsync(); // Это обновит локальную БД
                }

                await DisplayAlert("Успешно", "Профиль успешно обновлен", "OK");
                
                // Очищаем поле пароля
                passwordEntry.Text = string.Empty;
                
                // Обновляем текущего пользователя
                _currentUser = updatedUser;
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось обновить профиль", "OK");
            }
        }
        catch (UnauthorizedException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile] Unauthorized error saving profile: {ex.Message}");
            await DisplayAlert("Ошибка авторизации", "Ваша сессия истекла. Пожалуйста, войдите в систему заново.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile] Error saving profile: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Profile] StackTrace: {ex.StackTrace}");
            await DisplayAlert("Ошибка", $"Не удалось сохранить профиль: {ex.Message}", "OK");
        }
        finally
        {
            SaveButton.IsEnabled = true;
            SaveButton.Text = "Сохранить изменения";
        }
    }

    private async void OnBackButtonClicked(object? sender, EventArgs e)
    {
        if (!await _actionLock.WaitAsync(0))
            return;

        try
        {
            if (sender is Button btn)
                btn.IsEnabled = false;

            await OnBackButtonClickedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile] Error in OnBackButtonClicked: {ex.Message}");
        }
        finally
        {
            if (sender is Button btn)
                btn.IsEnabled = true;
            _actionLock.Release();
        }
    }

    private async Task OnBackButtonClickedAsync()
    {
        try
        {
            if (Shell.Current == null)
            {
                return;
            }

            if (Shell.Current.Navigation != null && Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync(animated: true);
            }
            else
            {
                await Shell.Current.GoToAsync("..", animate: true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile] Navigation error: {ex.Message}");
        }
    }
}
