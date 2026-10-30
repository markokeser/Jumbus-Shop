using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Catalog;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

public interface IAdminStockService
{
    Task<AdminStockViewModel> ListAsync(AdminStockQuery query, string culture, CancellationToken ct);

    Task<StockSetResult> SetAsync(int variantId, int original, int quantity, CancellationToken ct);
}

public class AdminStockService : IAdminStockService
{
    private const int PageSize = 50;

    private readonly ApplicationDbContext _context;

    public AdminStockService(ApplicationDbContext context) => _context = context;

    public static async Task<List<StockRow>> ToRowsAsync(
        ApplicationDbContext context,
        IQueryable<ProductVariant> variants,
        string culture,
        CancellationToken ct)
    {
        var rows = await variants
            .Select(v => new StockRow
            {
                VariantId = v.Id,
                ProductId = v.ProductId,
                ProductName = v.Product.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? v.Product.Sku,
                Sku = v.Sku,
                Stock = v.StockQuantity,
                Threshold = v.LowStockThreshold,
                IsActive = v.IsActive
            })
            .ToListAsync(ct);

        var labels = await VariantLabels.LoadAsync(context, [.. rows.Select(r => r.VariantId)], culture, ct);

        foreach (var row in rows)
        {
            row.Label = labels.GetValueOrDefault(row.VariantId, string.Empty);
        }

        return rows;
    }

    public async Task<AdminStockViewModel> ListAsync(AdminStockQuery query, string culture, CancellationToken ct)
    {
        var variants = _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Product.Status != ProductStatus.Archived);

        if (query.LowOnly)
        {
            variants = variants.Where(v => v.IsActive && v.StockQuantity <= v.LowStockThreshold);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            variants = variants.Where(v => v.Sku.Contains(term)
                || v.Product.Sku.Contains(term)
                || v.Product.Translations.Any(t => t.Name.Contains(term)));
        }

        var total = await variants.CountAsync(ct);
        var page = AdminPage.Of(query.Page, PageSize, total);

        return new AdminStockViewModel
        {
            Query = query,
            Page = page,
            Rows = await ToRowsAsync(
                _context,
                variants.OrderBy(v => v.StockQuantity).ThenBy(v => v.ProductId).ThenBy(v => v.Id).Skip(page.Skip).Take(page.Size),
                culture,
                ct)
        };
    }

    /// <summary>
    /// Sets a counted quantity, but only if the stock is still what the admin
    /// was looking at. An order placed while they were counting would
    /// otherwise be silently undone: 5 on screen, 4 after the order, and the
    /// admin's "5" puts the sold item back on sale.
    /// </summary>
    public async Task<StockSetResult> SetAsync(int variantId, int original, int quantity, CancellationToken ct)
    {
        if (quantity is < 0 or > 1_000_000)
        {
            return new StockSetResult(StockSetOutcome.Invalid, original);
        }

        // Same number: nothing to write. Also sidesteps MySQL reporting an
        // UPDATE that changes nothing as zero rows, which would read as a conflict.
        if (quantity == original
            && await _context.ProductVariants.AnyAsync(v => v.Id == variantId && v.StockQuantity == original, ct))
        {
            return new StockSetResult(StockSetOutcome.Updated, quantity);
        }

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE ProductVariants SET StockQuantity = {quantity} WHERE Id = {variantId} AND StockQuantity = {original}",
            ct);

        if (affected == 1)
        {
            return new StockSetResult(StockSetOutcome.Updated, quantity);
        }

        var current = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => (int?)v.StockQuantity)
            .FirstOrDefaultAsync(ct);

        return current is null
            ? new StockSetResult(StockSetOutcome.NotFound, 0)
            : new StockSetResult(StockSetOutcome.Conflict, current.Value);
    }
}
