using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace YessGoFront.Views
{
    public partial class PartnersListPage : ContentPage
    {
        public ObservableCollection<PartnerListItem> Partners { get; } = new();

        public PartnersListPage()
        {
            InitializeComponent();

            // наполняем тестовыми данными (как на макете)
            Partners.Add(new PartnerListItem
            {
                Logo = "farmamir_logo.png",
                Name = "ФармаМир",
                Category = "Здоровье и красота",
                CashbackText = "до 1%"
            });

            Partners.Add(new PartnerListItem
            {
                Logo = "adriano_logo.png",
                Name = "Адриан Кофе",
                Category = "Кафе и рестораны",
                CashbackText = "до 8%"
            });

            Partners.Add(new PartnerListItem
            {
                Logo = "salon_logo.jpg",
                Name = "Салон красоты",
                Category = "Здоровье и красота",
                CashbackText = "до 10%"
            });

            Partners.Add(new PartnerListItem
            {
                Logo = "eldik_logo.jpg",
                Name = "Элдик",
                Category = "Супермаркет",
                CashbackText = "до 1%"
            });

            // можно повторить, чтобы показать длинный список:
            Partners.Add(new PartnerListItem
            {
                Logo = "farmamir_logo.png",
                Name = "ФармаМир",
                Category = "Здоровье и красота",
                CashbackText = "до 1%"
            });

            Partners.Add(new PartnerListItem
            {
                Logo = "adriano_logo.png",
                Name = "Адриан Кофе",
                Category = "Кафе и рестораны",
                CashbackText = "до 8%"
            });

            Partners.Add(new PartnerListItem
            {
                Logo = "salon_logo.jpg",
                Name = "Салон красоты",
                Category = "Здоровье и красота",
                CashbackText = "до 10%"
            });

            Partners.Add(new PartnerListItem
            {
                Logo = "eldik_logo.jpg",
                Name = "Элдик",
                Category = "Супермаркет",
                CashbackText = "до 1%"
            });

            BindingContext = this;
        }

        private async void OnBackTapped(object? sender, TappedEventArgs e)
        {
            // вернуться назад
            await Shell.Current.GoToAsync("///main/partner");
        }
    }

    // модель для строки списка
    public class PartnerListItem
    {
        public string Logo { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string CashbackText { get; set; } = "";
    }
}
