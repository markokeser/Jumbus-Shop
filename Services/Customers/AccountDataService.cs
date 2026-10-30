using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Identity;

namespace WebShop.Services.Customers;

public enum AccountDeletion
{
    Deleted,

    /// <summary>Admins cannot remove themselves from the shop side; another admin or the database has to.</summary>
    IsAdmin,

    Failed
}

public interface IAccountDataService
{
    /// <summary>Everything the shop holds about this customer, as JSON (right of access and portability).</summary>
    Task<byte[]> ExportAsync(ApplicationUser user, CancellationToken ct);

    Task<AccountDeletion> DeleteAsync(ApplicationUser user, CancellationToken ct);
}

/// <summary>
/// The customer's own copy of their data, and deleting the account.
///
/// Deleting relies on the foreign keys in the schema: addresses, wishlist and
/// reviews go with the account (ON DELETE CASCADE); orders stay, unlinked
/// (ON DELETE SET NULL), because accounting and consumer law require the
/// shop to keep them. The name and address on an order stay with the order
/// for the same reason — the privacy policy says so.
/// </summary>
public class AccountDataService : IAccountDataService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<AccountDataService> _logger;

    public AccountDataService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> users,
        ILogger<AccountDataService> logger)
    {
        _context = context;
        _users = users;
        _logger = logger;
    }

    public async Task<byte[]> ExportAsync(ApplicationUser user, CancellationToken ct)
    {
        var addresses = await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == user.Id)
            .OrderBy(a => a.Id)
            .Select(a => new
            {
                Type = a.Type.ToString(),
                a.FirstName,
                a.LastName,
                a.Street,
                a.StreetNumber,
                a.PostalCode,
                a.City,
                a.Country,
                a.PhoneNumber,
                a.IsDefault
            })
            .ToListAsync(ct);

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == user.Id)
            .OrderBy(o => o.PlacedUtc)
            .Select(o => new
            {
                o.OrderNumber,
                o.PlacedUtc,
                Status = o.Status.ToString(),
                o.Email,
                o.FirstName,
                o.LastName,
                o.PhoneNumber,
                ShippingAddress = new
                {
                    o.ShippingStreet,
                    o.ShippingStreetNumber,
                    o.ShippingPostalCode,
                    o.ShippingCity,
                    o.ShippingCountry
                },
                o.BillingStreet,
                o.BillingPostalCode,
                o.BillingCity,
                o.CompanyName,
                o.CompanyTaxNumber,
                o.CustomerNote,
                PaymentMethod = o.PaymentMethod.ToString(),
                o.CourierName,
                o.TrackingNumber,
                o.Subtotal,
                o.DiscountAmount,
                o.ShippingFee,
                o.Total,
                o.Currency,
                Items = o.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new { i.ProductName, i.VariantDescription, i.Sku, i.Quantity, i.UnitPrice })
                    .ToList()
            })
            .ToListAsync(ct);

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.UserId == user.Id)
            .OrderBy(r => r.CreatedUtc)
            .Select(r => new { Product = r.Product.Sku, r.Rating, r.Title, r.Body, r.CreatedUtc, r.IsApproved })
            .ToListAsync(ct);

        var wishlist = await _context.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == user.Id)
            .OrderBy(w => w.AddedUtc)
            .Select(w => new { Product = w.Product.Sku, w.AddedUtc })
            .ToListAsync(ct);

        var export = new
        {
            ExportedUtc = DateTime.UtcNow,
            Account = new
            {
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.RegisteredUtc,
                user.PreferredCulture,
                user.AcceptsMarketing
            },
            Addresses = addresses,
            Orders = orders,
            Reviews = reviews,
            Wishlist = wishlist
        };

        return JsonSerializer.SerializeToUtf8Bytes(export, Json);
    }

    public async Task<AccountDeletion> DeleteAsync(ApplicationUser user, CancellationToken ct)
    {
        if (await _users.IsInRoleAsync(user, ApplicationRoles.Admin))
        {
            return AccountDeletion.IsAdmin;
        }

        var result = await _users.DeleteAsync(user);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Deleting account {UserId} failed: {Errors}",
                user.Id, string.Join("; ", result.Errors.Select(e => e.Code)));
            return AccountDeletion.Failed;
        }

        // No email or name in the log: the point was to forget them.
        _logger.LogInformation("Account {UserId} deleted at the customer's request.", user.Id);
        return AccountDeletion.Deleted;
    }
}
