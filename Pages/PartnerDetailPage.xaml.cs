namespace YessGoFront.Pages;

[QueryProperty(nameof(PartnerId), "partnerId")]
public partial class PartnerDetailPage : ContentPage
{
    private string partnerId;
    public string PartnerId
    {
        get => partnerId;
        set
        {
            partnerId = value;
            LoadPartnerInfo(partnerId);
        }
    }

    private void LoadPartnerInfo(string id)
    {
        // ������: ���� �������� ������ �� id
        if (id == "p001")
        {
            //PartnerNameLabel.Text = "SIERRA Coffee";
            //PartnerDescriptionLabel.Text = "������� � ������ ����������...";
        }
    }
}

