using YessGoFront.Services;
using YessGoFront.Views;

namespace YessGoFront
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // регистрация маршрутов внутренних страниц
            Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));
            Routing.RegisterRoute(nameof(PartnersListPage), typeof(PartnersListPage));

            // выбор стартовой страницы
            if (AccountStore.Instance.IsSignedIn)
                _ = GoToAsync("///main");
            else
                _ = GoToAsync("///login");
        }
    }
}
