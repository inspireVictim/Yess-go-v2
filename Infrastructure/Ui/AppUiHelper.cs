using Microsoft.Maui.Controls;
using System;

namespace YessGoFront.Infrastructure.Ui
{
    public static class AppUiHelper
    {
        public static Page GetCurrentPageOrThrow()
        {
            var page = Application.Current?.Windows.Count > 0
                ? Application.Current.Windows[0]?.Page
                : null;

            return page ?? throw new InvalidOperationException("Нет активного окна/страницы приложения.");
        }

        public static Page? TryGetCurrentPage()
        {
            return Application.Current?.Windows.Count > 0
                ? Application.Current.Windows[0]?.Page
                : null;
        }
    }
}
