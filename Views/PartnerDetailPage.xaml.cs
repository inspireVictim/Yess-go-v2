using Microsoft.Maui.Controls;
using YessGoFront.Models;
using YessGoFront.Services.Domain;
using Microsoft.Extensions.DependencyInjection;
using YessGoFront.Converters;

namespace YessGoFront.Views
{
    [QueryProperty(nameof(PartnerId), "partnerId")]
    public partial class PartnerDetailPage : ContentPage
    {
        private string? partnerId;
        private IPartnersService? _partnersService;

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
            // Получаем сервис из DI
            _partnersService = MauiProgram.Services.GetService<IPartnersService>();
        }

        private async void LoadPartner(string id)
        {
            try
            {
                if (_partnersService != null)
                {
                    var partner = await _partnersService.GetPartnerByIdAsync(id);
                    
                    if (partner != null)
                    {
                        PartnerName.Text = partner.Name;
                        PartnerCategory.Text = $"Категория: {partner.Category}";
                        PartnerDescription.Text = partner.Description ?? "Описание отсутствует";
                        
                        if (!string.IsNullOrWhiteSpace(partner.LogoUrl))
                        {
                            // Используем конвертер для правильной загрузки изображения (URL или локальный файл)
                            var converter = new StringToImageSourceConverter();
                            PartnerLogo.Source = converter.Convert(partner.LogoUrl, typeof(ImageSource), null, System.Globalization.CultureInfo.CurrentCulture) as ImageSource;
                        }
                        else
                        {
                            PartnerLogo.Source = "default_partner_logo.png";
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Загружен партнёр: {partner.Name}");
                    }
                    else
                    {
                        PartnerName.Text = $"Партнёр №{id}";
                        PartnerCategory.Text = "Категория: Не указана";
                        PartnerDescription.Text = "Информация о партнёре не найдена.";
                        PartnerLogo.Source = "default_partner_logo.png";
                    }
                }
                else
                {
                    // Fallback, если сервис недоступен
                    PartnerName.Text = $"Партнёр №{id}";
                    PartnerCategory.Text = "Категория: Еда и напитки";
                    PartnerDescription.Text = "Описание партнёра, информация о скидках, адрес и контакты.";
                    PartnerLogo.Source = "default_partner_logo.png";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartnerDetailPage] Ошибка загрузки партнёра: {ex.Message}");
                PartnerName.Text = $"Партнёр №{id}";
                PartnerCategory.Text = "Ошибка загрузки";
                PartnerDescription.Text = "Не удалось загрузить информацию о партнёре.";
                PartnerLogo.Source = "default_partner_logo.png";
            }
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
