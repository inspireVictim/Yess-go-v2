using Microsoft.Maui.Controls;

namespace YessGoFront.Views;

public partial class ContactsPage : ContentPage
{
    public ContactsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Обновляем нижний навбар
        if (this.FindByName<Controls.BottomNavBar>("BottomBar") is { } bottom)
            bottom.UpdateSelectedTab("More");
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        try
        {
            // Отключаем кнопку на время навигации, чтобы избежать двойных нажатий
            if (BackButton != null)
            {
                BackButton.IsEnabled = false;
            }
            
            // Возвращаемся на страницу More
            await Shell.Current.GoToAsync("//main/more", animate: true);
            System.Diagnostics.Debug.WriteLine("[ContactsPage] Успешно вернулись на страницу More");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContactsPage] Ошибка навигации: {ex.Message}");
            // Попытка использовать альтернативный маршрут
            try
            {
                await Shell.Current.GoToAsync("..", animate: true);
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.WriteLine($"[ContactsPage] Альтернативный маршрут тоже не сработал: {ex2.Message}");
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
}