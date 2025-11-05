using Microsoft.Maui.Controls;
using YessGoFront.Models;

namespace YessGoFront.Views
{
    [QueryProperty(nameof(PartnerId), "partnerId")]
    public partial class PartnerDetailPage : ContentPage
    {
        private string? partnerId;

        public string? PartnerId
        {
            get => partnerId;
            set
            {
                partnerId = value;
                if (!string.IsNullOrWhiteSpace(partnerId))
                {
                    LoadPartner(partnerId);
                }
            }
        }

        public PartnerDetailPage()
        {
            InitializeComponent();
        }

        private void LoadPartner(string id)
        {
            // В реальном проекте ты можешь загрузить данные из базы
            PartnerName.Text = $"Партнёр №{id}";
            PartnerCategory.Text = "Категория: Еда и напитки";
            PartnerDescription.Text = "Описание партнёра, информация о скидках, адрес и контакты.";
            PartnerLogo.Source = "default_partner_logo.png";
        }

        private async void OnMapClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Карта", "Показать местоположение партнёра.", "OK");
        }

        private async void OnCallClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Звонок", "Позвонить партнёру.", "OK");
        }
    }
}
