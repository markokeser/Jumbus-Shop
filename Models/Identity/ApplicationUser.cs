using Microsoft.AspNetCore.Identity;
using WebShop.Models.Community;
using WebShop.Models.Orders;

namespace WebShop.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime RegisteredUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Language the account was created in; used for transactional email.</summary>
    public string PreferredCulture { get; set; } = "sr";

    public bool AcceptsMarketing { get; set; }

    public ICollection<Address> Addresses { get; set; } = [];

    public ICollection<Order> Orders { get; set; } = [];

    public ICollection<WishlistItem> WishlistItems { get; set; } = [];

    public ICollection<Review> Reviews { get; set; } = [];

    public string FullName =>
        string.Join(' ', new[] { FirstName, LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));
}
