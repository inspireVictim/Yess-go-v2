using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace YessGoFront.Converters
{
    public class ProgressToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is double progress)
                {
                    // Берём ширину экрана (не MainPage.Width, а самого визуального слоя)
                    var page = Application.Current?.MainPage;
                    double totalWidth = 0;

                    if (page is ContentPage cp && cp.Content is VisualElement ve)
                        totalWidth = ve.Width;
                    else
                        totalWidth = page?.Width ?? 0;

                    if (totalWidth <= 0)
                        totalWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

                    // Получаем количество сегментов
                    var vm = page?.BindingContext as ViewModels.MainPageViewModel;
                    int count = Math.Max(1, vm?.PageProgressList?.Count ?? 1);

                    // Ширина текущего сегмента
                    double segmentWidth = totalWidth / count;

                    // Возвращаем ширину для текущего прогресса
                    return segmentWidth * Math.Clamp(progress, 0, 1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Converter Error] {ex}");
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ProgressToColorConverter : IValueConverter
    {
        public Color ActiveColor { get; set; } = Colors.White;
        public Color DoneColor { get; set; } = Color.FromArgb("#BBBBBB");
        public Color FutureColor { get; set; } = Color.FromArgb("#333333");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double p)
            {
                if (p >= 1.0) return DoneColor;   // завершённый
                if (p > 0.0) return ActiveColor;  // текущий
                return FutureColor;               // будущий
            }

            return FutureColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
