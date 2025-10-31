using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps; // 👈 обязательно для Location, MapSpan, Distance

namespace YessGoFront.Views
{
    public partial class MapPage : ContentPage
    {
        public MapPage()
        {
            InitializeComponent();
            //InitializeMap();
        }


        private async void OnExitClicked(object sender, EventArgs e)
        {
            // Возвращаемся на предыдущую страницу в стеке навигации
            await Shell.Current.GoToAsync("///main/partner");
        }

        //private void InitializeMap()
        //{
        //    var bishkek = new Location(42.8746, 74.5698);
        //    MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(bishkek, Distance.FromKilometers(3)));
        //}

        //private async void OnFilterButtonClicked(object sender, EventArgs e)
        //{
        //    await Navigation.PushAsync(new FilterPage());
        //}
    }
}