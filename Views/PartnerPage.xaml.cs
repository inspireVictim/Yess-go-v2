using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace YessGoFront.Views
{
    public partial class PartnerPage : ContentPage
    {
        public ObservableCollection<CategoryItem> Categories { get; set; }

        // счётчик, чтобы делать небольшую задержку между карточками
        private int _categoryAnimationIndex = 0;

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

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // сбрасываем счётчик, когда возвращаемся на страницу
            _categoryAnimationIndex = 0;

            await AnimatePageAsync();
        }

        private async Task AnimatePageAsync()
        {
            try
            {
                // верх
                await Task.WhenAll(
                    SearchContainer.FadeTo(1, 350, Easing.CubicOut),
                    SearchContainer.TranslateTo(0, 0, 350, Easing.CubicOut)
                );

                // список целиком (без карточек)
                await Task.WhenAll(
                    CategoriesCollection.FadeTo(1, 350, Easing.CubicOut),
                    CategoriesCollection.TranslateTo(0, 0, 350, Easing.CubicOut)
                );

                // низ
                await Task.WhenAll(
                    BottomBar.FadeTo(1, 300, Easing.CubicOut),
                    BottomBar.TranslateTo(0, 0, 300, Easing.CubicOut)
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error: {ex.Message}");
            }
        }

        // 👉 Анимация для КАЖДОЙ карточки — вызывается из XAML (Loaded="CategoryFrame_Loaded")
        private async void CategoryFrame_Loaded(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                try
                {
                    // небольшая ступенчатая задержка, чтобы было по очереди
                    int delay = 60 * _categoryAnimationIndex;
                    _categoryAnimationIndex++;

                    await Task.Delay(delay);

                    view.Opacity = 0;
                    view.TranslationY = 20;

                    await Task.WhenAll(
                        view.FadeTo(1, 280, Easing.CubicOut),
                        view.TranslateTo(0, 0, 280, Easing.CubicOut)
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Item animation error: {ex.Message}");
                }
            }
        }

        private async void OnMapButtonClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("///MapPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось перейти: {ex.Message}", "ОК");
            }
        }

        private async void Category_Tapped(object sender, TappedEventArgs e)
        {
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

        // адаптация под размер
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (CategoriesCollection.ItemsLayout is GridItemsLayout gridLayout)
            {
                if (width < 400)
                    gridLayout.Span = 2;
                else if (width < 700)
                    gridLayout.Span = 3;
                else
                    gridLayout.Span = 4;
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
