using Microsoft.EntityFrameworkCore;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;

namespace WebShop.Services.Catalog;

public interface IProductFilterService
{
    IQueryable<Product> Apply(IQueryable<Product> source, ProductQuery query, string culture);

    IQueryable<Product> Sort(IQueryable<Product> source, ProductSort sort, string culture);
}

/// <summary>
/// Builds the catalog query. Kept apart from CatalogService because this is
/// the part that grows every time a new filter is added, and because the
/// attribute clauses are subtle enough to deserve their own home.
/// </summary>
public class ProductFilterService : IProductFilterService
{
    public IQueryable<Product> Apply(IQueryable<Product> source, ProductQuery query, string culture)
    {
        var now = DateTime.UtcNow;

        source = source.Where(p => p.Status == ProductStatus.Published);

        // A category filter covers the category and anything under it, so
        // "Garderoba" lists dresses, tops and coats together.
        if (query.CategoryId is { } categoryId)
        {
            source = source.Where(p =>
                p.CategoryId == categoryId || p.Category.ParentId == categoryId);
        }

        if (query.Brands.Count > 0)
        {
            var brands = query.Brands;
            source = source.Where(p => p.Brand != null && brands.Contains(p.Brand.Slug));
        }

        if (query.InStockOnly)
        {
            source = source.Where(p => p.Variants.Any(v => v.IsActive && v.StockQuantity > 0));
        }

        if (query.OnSaleOnly)
        {
            source = source.Where(p => p.Variants.Any(v =>
                v.IsActive
                && v.SalePrice != null
                && v.SalePrice < v.Price
                && (v.SaleStartsUtc == null || v.SaleStartsUtc <= now)
                && (v.SaleEndsUtc == null || v.SaleEndsUtc >= now)));
        }

        // The sale-window expression is written out in full here, in Sort below
        // and again in the list projection. EF has to translate it to SQL, and
        // it cannot translate a call into ProductVariant.GetEffectivePrice — so
        // the three copies are deliberate. Change one, change all three.
        if (query.MinPrice is { } min)
        {
            source = source.Where(p => p.Variants.Any(v => v.IsActive
                && (v.SalePrice != null
                    && (v.SaleStartsUtc == null || v.SaleStartsUtc <= now)
                    && (v.SaleEndsUtc == null || v.SaleEndsUtc >= now)
                        ? v.SalePrice.Value
                        : v.Price) >= min));
        }

        if (query.MaxPrice is { } max)
        {
            source = source.Where(p => p.Variants.Any(v => v.IsActive
                && (v.SalePrice != null
                    && (v.SaleStartsUtc == null || v.SaleStartsUtc <= now)
                    && (v.SaleEndsUtc == null || v.SaleEndsUtc >= now)
                        ? v.SalePrice.Value
                        : v.Price) <= max));
        }

        source = ApplyAttributes(source, query);
        source = ApplySearch(source, query, culture);

        return source;
    }

    /// <summary>
    /// One clause per attribute, ANDed; the options inside one attribute are
    /// ORed. So picking black and navy under colour, plus M under size, means
    /// "comes in black or navy, and comes in M".
    ///
    /// Note that the two conditions are checked against the product, not
    /// against one variant: a dress that exists in black/L and navy/M passes.
    /// That is how shoppers read a filtered grid — the exact combination is
    /// settled on the product page, where the picker greys out what is gone.
    /// </summary>
    private static IQueryable<Product> ApplyAttributes(IQueryable<Product> source, ProductQuery query)
    {
        foreach (var (attributeCode, optionCodes) in query.Attributes)
        {
            if (optionCodes.Count == 0)
            {
                continue;
            }

            // Copied into locals so each iteration closes over its own values.
            var code = attributeCode;
            var options = optionCodes;

            source = source.Where(p => p.Variants.Any(v => v.IsActive
                && v.AttributeValues.Any(av =>
                    av.AttributeDefinition.Code == code
                    && av.AttributeOption != null
                    && options.Contains(av.AttributeOption.Code))));
        }

        return source;
    }

    private static IQueryable<Product> ApplySearch(
        IQueryable<Product> source,
        ProductQuery query,
        string culture)
    {
        var terms = SearchTerms.Build(query.Search);

        if (terms.Count == 0)
        {
            return source;
        }

        // At most two patterns, and the second is only ever the đ/dj rewrite.
        // Repeating the first when there is no second keeps the expression a
        // single translatable shape instead of a loop EF cannot follow.
        var first = terms[0];
        var second = terms.Count > 1 ? terms[1] : terms[0];

        return source.Where(p =>
            p.Translations.Any(t => t.Culture == culture
                && (EF.Functions.Like(t.Name, first)
                    || EF.Functions.Like(t.Name, second)
                    || (t.ShortDescription != null
                        && (EF.Functions.Like(t.ShortDescription, first)
                            || EF.Functions.Like(t.ShortDescription, second)))
                    || (t.Ingredients != null
                        && (EF.Functions.Like(t.Ingredients, first)
                            || EF.Functions.Like(t.Ingredients, second)))))
            || (p.Brand != null
                && (EF.Functions.Like(p.Brand.Name, first)
                    || EF.Functions.Like(p.Brand.Name, second)))
            || EF.Functions.Like(p.Sku, first));
    }

    public IQueryable<Product> Sort(IQueryable<Product> source, ProductSort sort, string culture)
    {
        var now = DateTime.UtcNow;

        return sort switch
        {
            ProductSort.PriceLowToHigh => source
                .OrderBy(p => p.Variants
                    .Where(v => v.IsActive)
                    .Min(v => (decimal?)(v.SalePrice != null
                        && (v.SaleStartsUtc == null || v.SaleStartsUtc <= now)
                        && (v.SaleEndsUtc == null || v.SaleEndsUtc >= now)
                            ? v.SalePrice.Value
                            : v.Price)))
                .ThenBy(p => p.Id),

            ProductSort.PriceHighToLow => source
                .OrderByDescending(p => p.Variants
                    .Where(v => v.IsActive)
                    .Min(v => (decimal?)(v.SalePrice != null
                        && (v.SaleStartsUtc == null || v.SaleStartsUtc <= now)
                        && (v.SaleEndsUtc == null || v.SaleEndsUtc >= now)
                            ? v.SalePrice.Value
                            : v.Price)))
                .ThenBy(p => p.Id),

            ProductSort.NameAscending => source
                .OrderBy(p => p.Translations
                    .Where(t => t.Culture == culture)
                    .Select(t => t.Name)
                    .FirstOrDefault())
                .ThenBy(p => p.Id),

            // Newest, and the fallback for anything a hand-edited URL invents.
            _ => source
                .OrderByDescending(p => p.PublishedUtc ?? p.CreatedUtc)
                .ThenByDescending(p => p.Id)
        };
    }
}
