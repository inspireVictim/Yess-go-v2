using System.Linq;
using Microsoft.Maui.Controls;
using YessGoFront.Views;

namespace YessGoFront
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Регистрируем маршрут страницы кошелька
            Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));

            // Стартовая вкладка через коллекции, без Shell.Current
            var tabBar = this.Items.OfType<TabBar>().FirstOrDefault();
            if (tabBar != null)
            {
                var homeTab = tabBar.Items.OfType<Tab>().FirstOrDefault(t => t.Route == "home");
                if (homeTab != null)
                    this.CurrentItem = homeTab;
            }

            // Если очень хочется GoToAsync — делаем после загрузки:
            this.Loaded += async (_, __) =>
            {
                // здесь Shell.Current уже не null
                // навигация на корень home (не обязательно, но допустимо)
                // await this.GoToAsync("//home");
            };
        }
    }
}
