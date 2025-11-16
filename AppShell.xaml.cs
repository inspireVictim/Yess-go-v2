using YessGoFront.Views;

namespace YessGoFront
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Регистрация внутренних маршрутов
            Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));
            Routing.RegisterRoute(nameof(PartnersListPage), typeof(PartnersListPage));
            Routing.RegisterRoute(nameof(PartnerPage), typeof(PartnerPage));
            Routing.RegisterRoute(nameof(PartnerDetailPage), typeof(PartnerDetailPage));
        }
    }
}
