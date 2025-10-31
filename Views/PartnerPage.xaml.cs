using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace YessGoFront.Views
{
    public partial class PartnerPage : ContentPage
    {
        public ObservableCollection<CategoryItem> Categories { get; set; }

        private async void OnMapButtonClicked(object sender, EventArgs e)
        {
            // Переход на другую страницу
            await Shell.Current.GoToAsync("///MapPage");
        }


        public PartnerPage()
        {
            InitializeComponent();

            Categories = new ObservableCollection<CategoryItem>
            {
                new CategoryItem("Все компании", "cat_all.png"),
                new CategoryItem("Еда и напитки", "cat_food.png"),
                new CategoryItem("Одежда и обувь", "cat_clothes.png"),
                new CategoryItem("Красота", "cat_beauty.png"),
                new CategoryItem("Все для дома", "cat_home.png"),
                new CategoryItem("Продукты", "cat_electronics.png"),
                new CategoryItem("Электроника", "cat_electronics.png"),
                new CategoryItem("Детское", "cat_kids.png"),
                new CategoryItem("Спорт и отдых", "cat_sport.png"),
                new CategoryItem("Кафе и рестораны", "category_cafe.png"),
                new CategoryItem("Транспорт", "category_transport.png"),
                new CategoryItem("Образование", "category_education.png")
            };

            CategoriesCollection.ItemsSource = Categories;
        }

        private async void Category_Tapped(object sender, TappedEventArgs e)
        {
            // Навигация на страницу списка партнёров
            try
            {
                await Shell.Current.GoToAsync("///PartnersListPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось перейти: {ex.Message}", "ОК");
            }

        }
    }

    public class CategoryItem
    {
        public string Name { get; set; }
        public string Image { get; set; }

        public CategoryItem(string name, string image)
        {
            Name = name;
            Image = image;
        }
    }
}
