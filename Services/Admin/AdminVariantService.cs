using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

public record VariantSaveResult(VariantSaveOutcome Outcome, int Id, IReadOnlyList<FieldError> Errors, int CurrentStock = 0);

public interface IAdminVariantService
{
    Task<VariantFormViewModel?> NewFormAsync(int productId, string culture, CancellationToken ct);

    Task<VariantFormViewModel?> GetFormAsync(int variantId, string culture, CancellationToken ct);

    /// <summary>Refills the product name and the attribute fields after a failed post.</summary>
    Task<bool> FillAsync(VariantFormViewModel form, string culture, CancellationToken ct);

    Task<VariantSaveResult> SaveAsync(VariantFormViewModel form, string culture, CancellationToken ct);

    Task<DeleteOutcome> DeleteAsync(int variantId, CancellationToken ct);
}

public class AdminVariantService : IAdminVariantService
{
    private readonly ApplicationDbContext _context;

    public AdminVariantService(ApplicationDbContext context) => _context = context;

    public async Task<VariantFormViewModel?> NewFormAsync(int productId, string culture, CancellationToken ct)
    {
        var form = new VariantFormViewModel { ProductId = productId };

        if (!await FillAsync(form, culture, ct))
        {
            return null;
        }

        // Most variants of a product share a price and a reorder point.
        var sibling = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Id)
            .Select(v => new { v.Price, v.LowStockThreshold, v.WeightGrams })
            .FirstOrDefaultAsync(ct);

        form.Sku = form.ProductSku + "-";

        if (sibling is not null)
        {
            form.Price = sibling.Price;
            form.LowStockThreshold = sibling.LowStockThreshold;
            form.WeightGrams = sibling.WeightGrams;
        }

        return form;
    }

    public async Task<VariantFormViewModel?> GetFormAsync(int variantId, string culture, CancellationToken ct)
    {
        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.AttributeValues)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);

        if (variant is null)
        {
            return null;
        }

        var form = new VariantFormViewModel
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Sku = variant.Sku,
            Price = variant.Price,
            SalePrice = variant.SalePrice,
            SaleStartsLocal = ShopTime.Local(variant.SaleStartsUtc),
            SaleEndsLocal = ShopTime.Local(variant.SaleEndsUtc),
            StockQuantity = variant.StockQuantity,
            OriginalStock = variant.StockQuantity,
            LowStockThreshold = variant.LowStockThreshold,
            ExpiryDate = variant.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
            WeightGrams = variant.WeightGrams,
            IsActive = variant.IsActive,
            HasOrders = await _context.OrderItems.AnyAsync(i => i.ProductVariantId == variantId, ct),
            Attributes = [.. variant.AttributeValues.Select(v => new AttributeValueField
            {
                AttributeDefinitionId = v.AttributeDefinitionId,
                OptionId = v.AttributeOptionId,
                NumericValue = v.NumericValue,
                TextValue = v.TextValue
            })]
        };

        await FillAsync(form, culture, ct);
        return form;
    }

    public async Task<bool> FillAsync(VariantFormViewModel form, string culture, CancellationToken ct)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == form.ProductId)
            .Select(p => new
            {
                p.Sku,
                p.CategoryId,
                Name = p.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (product is null)
        {
            return false;
        }

        form.ProductName = product.Name ?? product.Sku;
        form.ProductSku = product.Sku;
        form.Attributes = AttributeFields.Merge(
            await AttributeFields.ForCategoryAsync(_context, product.CategoryId, variantDefining: true, culture, ct),
            form.Attributes);

        return true;
    }

    public async Task<VariantSaveResult> SaveAsync(VariantFormViewModel form, string culture, CancellationToken ct)
    {
        // Fields are rebuilt from the database so a tampered option id or a
        // removed required attribute cannot slip through.
        if (!await FillAsync(form, culture, ct))
        {
            return new VariantSaveResult(VariantSaveOutcome.NotFound, 0, []);
        }

        var errors = new List<FieldError>();
        var sku = form.Sku.Trim();

        if (await _context.ProductVariants.AnyAsync(v => v.Sku == sku && v.Id != form.Id, ct))
        {
            errors.Add(new(nameof(form.Sku), "Admin_SkuTaken"));
        }

        if (form.SalePrice is { } sale && sale >= form.Price)
        {
            errors.Add(new(nameof(form.SalePrice), "Admin_SaleNotLower"));
        }

        if (form.SaleStartsLocal is { } starts && form.SaleEndsLocal is { } ends && ends <= starts)
        {
            errors.Add(new(nameof(form.SaleEndsLocal), "Admin_DatesOrder"));
        }

        for (var i = 0; i < form.Attributes.Count; i++)
        {
            var field = form.Attributes[i];

            if (AttributeFields.Problem(field) is { } problem)
            {
                var member = field.DataType switch
                {
                    AttributeDataType.Option => nameof(field.OptionId),
                    AttributeDataType.Numeric => nameof(field.NumericValue),
                    _ => nameof(field.TextValue)
                };

                errors.Add(new($"{nameof(form.Attributes)}[{i}].{member}", problem));
            }
        }

        // Two "Black / M" variants of one dress would split its stock and
        // leave the shopper's choice ambiguous.
        if (form.Attributes.Count > 0 && await DuplicateAsync(form, ct))
        {
            errors.Add(new(string.Empty, "Admin_VariantDuplicate"));
        }

        if (errors.Count > 0)
        {
            return new VariantSaveResult(VariantSaveOutcome.Invalid, form.Id, errors);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        ProductVariant variant;

        if (form.Id > 0)
        {
            var existing = await _context.ProductVariants
                .Include(v => v.AttributeValues)
                .FirstOrDefaultAsync(v => v.Id == form.Id && v.ProductId == form.ProductId, ct);

            if (existing is null)
            {
                return new VariantSaveResult(VariantSaveOutcome.NotFound, 0, []);
            }

            variant = existing;

            // Stock is written only if it still holds the number the admin saw,
            // and never through the tracked entity — EF would write back the
            // value it loaded and undo any order placed in between.
            if (form.StockQuantity != form.OriginalStock)
            {
                var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE ProductVariants SET StockQuantity = {form.StockQuantity} WHERE Id = {variant.Id} AND StockQuantity = {form.OriginalStock}",
                    ct);

                if (affected != 1)
                {
                    await transaction.RollbackAsync(ct);

                    var current = await _context.ProductVariants
                        .AsNoTracking()
                        .Where(v => v.Id == form.Id)
                        .Select(v => v.StockQuantity)
                        .FirstOrDefaultAsync(ct);

                    return new VariantSaveResult(VariantSaveOutcome.StockConflict, form.Id, [], current);
                }
            }
        }
        else
        {
            variant = new ProductVariant { ProductId = form.ProductId, StockQuantity = form.StockQuantity };
            _context.ProductVariants.Add(variant);

            // Shared attributes (skin type) are stored on every variant, so a
            // new one takes them from a sibling.
            var shared = await _context.VariantAttributeValues
                .AsNoTracking()
                .Where(v => v.ProductVariant.ProductId == form.ProductId && !v.AttributeDefinition.IsVariantDefining)
                .ToListAsync(ct);

            foreach (var value in shared.GroupBy(v => v.AttributeDefinitionId).Select(g => g.First()))
            {
                variant.AttributeValues.Add(new VariantAttributeValue
                {
                    AttributeDefinitionId = value.AttributeDefinitionId,
                    AttributeOptionId = value.AttributeOptionId,
                    NumericValue = value.NumericValue,
                    TextValue = value.TextValue
                });
            }
        }

        variant.Sku = sku;
        variant.Price = form.Price;
        variant.SalePrice = form.SalePrice;

        // Dates without a sale price would be meaningless leftovers.
        variant.SaleStartsUtc = form.SalePrice is null ? null : ShopTime.ToUtc(form.SaleStartsLocal);
        variant.SaleEndsUtc = form.SalePrice is null ? null : ShopTime.ToUtc(form.SaleEndsLocal);
        variant.LowStockThreshold = form.LowStockThreshold;
        variant.ExpiryDate = form.ExpiryDate is { } expiry ? DateOnly.FromDateTime(expiry) : null;
        variant.WeightGrams = form.WeightGrams;
        variant.IsActive = form.IsActive;

        foreach (var field in form.Attributes)
        {
            var row = variant.AttributeValues.FirstOrDefault(v => v.AttributeDefinitionId == field.AttributeDefinitionId);

            if (!AttributeFields.HasTypedValue(field))
            {
                if (row is not null)
                {
                    variant.AttributeValues.Remove(row);
                    _context.VariantAttributeValues.Remove(row);
                }

                continue;
            }

            if (row is null)
            {
                row = new VariantAttributeValue { AttributeDefinitionId = field.AttributeDefinitionId };
                variant.AttributeValues.Add(row);
            }

            AttributeFields.Apply(field, row);
        }

        try
        {
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return new VariantSaveResult(VariantSaveOutcome.Invalid, form.Id, [new(string.Empty, "Admin_SaveConflict")]);
        }

        return new VariantSaveResult(VariantSaveOutcome.Saved, variant.Id, []);
    }

    /// <summary>
    /// Only a variant nobody has ordered can be deleted. One that was ordered
    /// stays, switched off: order lines keep pointing at it for returns.
    /// </summary>
    public async Task<DeleteOutcome> DeleteAsync(int variantId, CancellationToken ct)
    {
        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, ct);

        if (variant is null)
        {
            return DeleteOutcome.NotFound;
        }

        if (await _context.OrderItems.AnyAsync(i => i.ProductVariantId == variantId, ct))
        {
            return DeleteOutcome.InUse;
        }

        // The wishlist's variant column has no cascade; clear it first.
        var saved = await _context.WishlistItems.Where(w => w.ProductVariantId == variantId).ToListAsync(ct);
        _context.WishlistItems.RemoveRange(saved);
        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync(ct);
        return DeleteOutcome.Deleted;
    }

    private async Task<bool> DuplicateAsync(VariantFormViewModel form, CancellationToken ct)
    {
        var definitionIds = form.Attributes.Select(a => a.AttributeDefinitionId).ToList();

        var mine = string.Join("|", form.Attributes
            .OrderBy(a => a.AttributeDefinitionId)
            .Select(a => AttributeFields.Signature(
                a.AttributeDefinitionId,
                a.DataType == AttributeDataType.Option ? a.OptionId : null,
                a.DataType == AttributeDataType.Numeric ? a.NumericValue : null,
                a.DataType == AttributeDataType.Text ? a.TextValue : null)));

        var siblings = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == form.ProductId && v.Id != form.Id)
            .Select(v => new
            {
                v.Id,
                Values = v.AttributeValues
                    .Where(a => definitionIds.Contains(a.AttributeDefinitionId))
                    .Select(a => new { a.AttributeDefinitionId, a.AttributeOptionId, a.NumericValue, a.TextValue })
                    .ToList()
            })
            .ToListAsync(ct);

        return siblings.Any(sibling => string.Join("|", definitionIds
            .Order()
            .Select(id =>
            {
                var value = sibling.Values.FirstOrDefault(v => v.AttributeDefinitionId == id);
                return AttributeFields.Signature(id, value?.AttributeOptionId, value?.NumericValue, value?.TextValue);
            })) == mine);
    }
}
