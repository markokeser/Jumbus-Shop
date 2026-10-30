using WebShop.Models.Catalog;
using WebShop.Models.Identity;

namespace WebShop.Models.Community;

public class WishlistItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    /// <summary>
    /// Optional. Set when the customer saved a specific size or shade rather
    /// than the product in general, so "back in stock" mail can be precise.
    /// </summary>
    public int? ProductVariantId { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;
}
