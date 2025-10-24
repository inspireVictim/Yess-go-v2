using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace YessGoFront.Views
{
    public partial class QrPage : ContentPage
    {
        private bool _torchOn = false;

        public QrPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Разрешение на камеру (попросим каждый раз, если не выдано)
            _ = EnsureCameraPermissionAsync();
        }

        private async System.Threading.Tasks.Task EnsureCameraPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            // если Granted — здесь ты можешь запустить реальную CameraView.
            // пока это заглушка.
        }

        // ===== Кнопка "Назад" (в шапке) =====
        private async void OnBackTapped(object? sender, EventArgs e)
        {
            // Возвращаемся назад в Shell.
            // Если QrPage живёт как отдельная вкладка, можно отправить на home таб:
            await Shell.Current.GoToAsync("///main/home");
        }

        // ===== Фонарик =====
        private void OnFlashTapped(object? sender, EventArgs e)
        {
            ToggleTorch();
        }

        private void OnTorchSliderChanged(object? sender, ValueChangedEventArgs e)
        {
            // Если слайдер > 0.5, считаем что фонарик должен быть "вкл"
            bool wantOn = e.NewValue > 0.5;
            if (wantOn != _torchOn)
                ToggleTorch();
        }

        private void ToggleTorch()
        {
            _torchOn = !_torchOn;
            // Здесь должен быть вызов фонарика камеры.
            // Сейчас просто логика-пустышка.
        }

        // ===== Кнопка "Мой QR" =====
        private async void OnMyQrTapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Мой QR", "Здесь покажем персональный QR пользователя", "OK");
        }

        // ===== Кнопка "Из Галереи" =====
        private async void OnGalleryTapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Из галереи", "Откроем галерею и попытаемся распознать QR на изображении", "OK");
        }
    }
}
