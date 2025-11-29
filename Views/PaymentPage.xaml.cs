using YessGoFront.ViewModels;
using Microsoft.Extensions.Logging;

namespace YessGoFront.Views;

[QueryProperty(nameof(PartnerIdStr), "partnerId")]
[QueryProperty(nameof(PartnerName), "partnerName")]
[QueryProperty(nameof(QrCode), "qrCode")]
public partial class PaymentPage : ContentPage
{
    public string? PartnerIdStr { get; set; }
    public string? PartnerName { get; set; }
    public string? QrCode { get; set; }

    public PaymentPage()
    {
        InitializeComponent();
        
        var viewModel = MauiProgram.Services.GetRequiredService<PaymentViewModel>();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Получаем параметры навигации
        if (BindingContext is PaymentViewModel viewModel)
        {
            if (!string.IsNullOrWhiteSpace(PartnerIdStr) && 
                int.TryParse(PartnerIdStr, out var partnerId))
            {
                viewModel.PartnerId = partnerId;
            }

            if (!string.IsNullOrWhiteSpace(PartnerName))
            {
                viewModel.PartnerName = Uri.UnescapeDataString(PartnerName);
            }

            if (!string.IsNullOrWhiteSpace(QrCode))
            {
                viewModel.QrCode = Uri.UnescapeDataString(QrCode);
            }
        }
    }
}

