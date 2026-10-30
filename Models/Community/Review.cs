using WebShop.Models.Catalog;
using WebShop.Models.Identity;

namespace WebShop.Models.Community;

public class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    /// <summary>One to five.</summary>
    public int Rating { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }

    /// <summary>
    /// Reviews are held until an admin approves them. Anything published
    /// straight away turns the product page into a spam target.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>True when the reviewer has a delivered order containing this product.</summary>
    public bool IsVerifiedPurchase { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedUtc { get; set; }
}
