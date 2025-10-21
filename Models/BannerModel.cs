namespace YessGoFront.Models;

public class BannerModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Image { get; set; } = string.Empty;   // картинка баннера
    public string PartnerName { get; set; } = string.Empty; // опционально
}
