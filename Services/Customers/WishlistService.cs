using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Catalog;
using WebShop.Models.Community;
using WebShop.Services.Catalog;
using WebShop.ViewModels.Catalog;

namespace WebShop.Services.Customers;

public interface IWishlistService
{
    Task<bool> ContainsAsync(string userId, int productId, CancellationToken ct);

    /// <summary>Saves the product if it is not saved, removes it if it is. Returns whether it is now saved.</summary>
    Task<bool> ToggleAsync(string userId, int productId, CancellationToken ct);

    Task<List<ProductCardViewModel>> ListAsync(string userId, string culture, CancellationToken ct);

    Task<int> CountAsync(string userId, CancellationToken ct);
}

/// <summary>
/// Saved products, kept at product level (ProductVariantId left null).
///
/// A caveat about the schema: the unique index covers (UserId, ProductId,
/// ProductVariantId), and MySQL treats every NULL as distinct inside a unique
/// index — so it does not stop the same product being saved twice. The
/// guard is here instead: Toggle removes every matching row, which cleans up
/// a duplicate from a double click as a side effect, and reads collapse to
/// distinct products.
/// </summary>
public class WishlistService : IWishlistService
{
    private readonly ApplicationDbContext _context;
    private readonly ICatalogService _catalog;

    public WishlistService(ApplicationDbContext context, ICatalogService catalog)
    {
        _context = context;
        _catalog = catalog;
    }

    public async Task<bool> ContainsAsync(string userId, int productId, CancellationToken ct) =>
        await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId, ct);

    public async Task<bool> ToggleAsync(string userId, int productId, CancellationToken ct)
    {
        var existing = await _context.WishlistItems
            .Where(w => w.UserId == userId && w.ProductId == productId)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            _context.WishlistItems.RemoveRange(existing);
            await _context.SaveChangesAsync(ct);
            return false;
        }

        // Checked first so a hand-crafted id fails quietly instead of as a
        // foreign-key error.
        var exists = await _context.Products
            .AnyAsync(p => p.Id == productId && p.Status == ProductStatus.Published, ct);

        if (!exists)
        {
            return false;
        }

        _context.WishlistItems.Add(new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            AddedUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<ProductCardViewModel>> ListAsync(string userId, string culture, CancellationToken ct)
    {
        var ids = await _context.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedUtc)
            .Select(w => w.ProductId)
            .ToListAsync(ct);

        return await _catalog.CardsAsync([.. ids.Distinct()], culture, ct);
    }

    public async Task<int> CountAsync(string userId, CancellationToken ct) =>
        await _context.WishlistItems
            .Where(w => w.UserId == userId)
            .Select(w => w.ProductId)
            .Distinct()
            .CountAsync(ct);
}
