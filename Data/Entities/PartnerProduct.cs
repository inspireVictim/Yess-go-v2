using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YessGoFront.Data.Entities;

/// <summary>
/// Продукт партнёра
/// </summary>
[Table("partner_products")]
public class PartnerProduct
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("ingredients", TypeName = "text")]
    public string? Ingredients { get; set; }

    [MaxLength(500)]
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [MaxLength(50)]
    [Column("weight")]
    public string? Weight { get; set; }

    [Required]
    [Column("price", TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }

    [Column("original_price", TypeName = "numeric(10,2)")]
    public decimal? OriginalPrice { get; set; }

    [Column("discount_percent", TypeName = "numeric(5,2)")]
    public decimal? DiscountPercent { get; set; }

    [Column("yess_coins", TypeName = "numeric(10,2)")]
    public decimal? YessCoins { get; set; }

    [Column("is_available")]
    public bool IsAvailable { get; set; } = true;

    [MaxLength(100)]
    [Column("category")]
    public string? Category { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PartnerId))]
    public Partner Partner { get; set; } = null!;
}

