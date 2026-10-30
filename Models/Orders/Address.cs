using WebShop.Models.Identity;

namespace WebShop.Models.Orders;

/// <summary>
/// An address in the customer's address book. Orders do not point at this
/// row — they copy the fields, so editing a saved address never rewrites
/// the history of a shipped order.
/// </summary>
public class Address
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public AddressType Type { get; set; } = AddressType.Shipping;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    /// <summary>House or flat number, kept separate for courier label printing.</summary>
    public string? StreetNumber { get; set; }

    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = "RS";

    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
