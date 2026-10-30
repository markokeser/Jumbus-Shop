using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Catalog;
using WebShop.ViewModels.Catalog;

namespace WebShop.Services.Catalog;

public interface ICatalogService
{
    Task<ProductListViewModel> ListAsync(ProductQuery query, string culture, CancellationToken ct);

    Task<CategoryContext?> FindCategoryAsync(string slug, string culture, CancellationToken ct);

    Task<ProductDetailsViewModel?> FindProductAsync(string slug, string culture, CancellationToken ct);

    Task<List<ProductCardViewModel>> FeaturedAsync(int take, string culture, CancellationToken ct);

    /// <summary>
    /// Cards for a given list of products, in that order. Withdrawn products
    /// drop out rather than rendering as broken cards.
    /// </summary>
    Task<List<ProductCardViewModel>> CardsAsync(IReadOnlyList<int> productIds, string culture, CancellationToken ct);

    /// <summary>
    /// Attribute codes that may appear as query-string filters. The controller
    /// checks incoming keys against this so a hand-typed "?nonsense=x" cannot
    /// bolt an extra clause onto the query.
    /// </summary>
    Task<IReadOnlyList<string>> FilterableAttributeCodesAsync(CancellationToken ct);

    /// <summary>Top-level categories with their subcategories, for the header menu.</summary>
    Task<List<WebShop.ViewModels.Navigation.CategoryNavGroup>> CategoryNavAsync(string culture, CancellationToken ct);

    /// <summary>
    /// This category's slug in every language, so the language switcher can
    /// send the shopper to the same category rather than to a URL that only
    /// exists in the language they are leaving.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> CategorySlugsAsync(int categoryId, CancellationToken ct);

    /// <summary>This product's category slug and own slug in every language.</summary>
    Task<IReadOnlyDictionary<string, ProductPath>> ProductPathsAsync(int productId, CancellationToken ct);

    /// <summary>
    /// Finds a category by a slug in any language. Used to rescue a URL that
    /// carries the wrong language's slug — an old link, or one someone pasted
    /// after switching languages — by redirecting it to the right address.
    /// </summary>
    Task<int?> CategoryIdBySlugAsync(string slug, CancellationToken ct);

    Task<int?> ProductIdBySlugAsync(string slug, CancellationToken ct);

    /// <summary>
    /// The category or product that used to answer to this slug before the
    /// admin renamed it, preferring the language being viewed.
    /// </summary>
    Task<int?> FormerOwnerAsync(SlugOwner owner, string slug, string culture, CancellationToken ct);
}

/// <summary>Both halves of a product URL in one language.</summary>
public record ProductPath(string CategorySlug, string ProductSlug);

/// <summary>Category resolved from a URL slug, plus what the page needs to title itself.</summary>
public record CategoryContext(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? ParentName,
    bool IsTopLevel)
{
    /// <summary>
    /// This category's slug in every language, so the switcher can point at
    /// the same category rather than at a URL that does not exist.
    /// </summary>
    public IReadOnlyDictionary<string, string> SlugsByCulture { get; init; } =
        new Dictionary<string, string>();

    public string? ParentSlug { get; init; }

    public string? MetaTitle { get; init; }

    public string? MetaDescription { get; init; }
}

public class CatalogService : ICatalogService
{
    private const string SizeAttribute = "size";
    private const string ColorAttribute = "color";
    private const string ShadeAttribute = "shade";
    private const string VolumeAttribute = "volume";
    private const string SkinTypeAttribute = "skin-type";

    /// <summary>More chips than this overflow the card's block.</summary>
    private const int MaxChips = 4;

    private readonly ApplicationDbContext _context;
    private readonly IProductFilterService _filters;

    public CatalogService(ApplicationDbContext context, IProductFilterService filters)
    {
        _context = context;
        _filters = filters;
    }

    public async Task<ProductListViewModel> ListAsync(
        ProductQuery query,
        string culture,
        CancellationToken ct)
    {
        query.Normalise();

        var filtered = _filters.Apply(_context.Products.AsNoTracking(), query, culture);

        var total = await filtered.CountAsync(ct);

        // A filter combination that matches nothing still has to render, and a
        // page number past the end should land on the last real page rather
        // than an empty grid.
        var pageCount = total == 0 ? 1 : (int)Math.Ceiling(total / (double)query.PageSize);
        if (query.Page > pageCount)
        {
            query.Page = pageCount;
        }

        var cards = await ProjectCardsAsync(
            _filters.Sort(filtered, query.Sort, culture)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize),
            culture,
            ct);

        return new ProductListViewModel
        {
            Query = query,
            Products = cards,
            TotalCount = total,
            PageCount = pageCount,
            Filters = await BuildFilterPanelAsync(query, culture, ct)
        };
    }

    public async Task<IReadOnlyList<string>> FilterableAttributeCodesAsync(CancellationToken ct) =>
        await _context.AttributeDefinitions
            .AsNoTracking()
            .Where(a => a.IsFilterable)
            .Select(a => a.Code)
            .ToListAsync(ct);

    public async Task<List<WebShop.ViewModels.Navigation.CategoryNavGroup>> CategoryNavAsync(string culture, CancellationToken ct) =>
        await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.ParentId == null)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new WebShop.ViewModels.Navigation.CategoryNavGroup
            {
                Name = c.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? c.Code,
                Slug = c.Translations.Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault() ?? c.Code,
                Children = c.Children
                    .Where(child => child.IsActive)
                    .OrderBy(child => child.DisplayOrder)
                    .Select(child => new WebShop.ViewModels.Navigation.CategoryNavLink
                    {
                        Name = child.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? child.Code,
                        Slug = child.Translations.Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault() ?? child.Code
                    })
                    .ToList()
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, string>> CategorySlugsAsync(
        int categoryId,
        CancellationToken ct) =>
        await _context.CategoryTranslations
            .AsNoTracking()
            .Where(t => t.CategoryId == categoryId)
            .ToDictionaryAsync(t => t.Culture, t => t.Slug, ct);

    public async Task<IReadOnlyDictionary<string, ProductPath>> ProductPathsAsync(
        int productId,
        CancellationToken ct)
    {
        // Two small queries joined in memory rather than one with a correlated
        // subquery on the culture column: the shape is clearer and there is
        // nothing for EF to get wrong translating it.
        var product = await _context.ProductTranslations
            .AsNoTracking()
            .Where(t => t.ProductId == productId)
            .Select(t => new { t.Culture, t.Slug, t.Product.CategoryId })
            .ToListAsync(ct);

        if (product.Count == 0)
        {
            return new Dictionary<string, ProductPath>();
        }

        var categoryId = product[0].CategoryId;

        var categories = await _context.CategoryTranslations
            .AsNoTracking()
            .Where(t => t.CategoryId == categoryId)
            .ToDictionaryAsync(t => t.Culture, t => t.Slug, ct);

        var paths = new Dictionary<string, ProductPath>();

        foreach (var row in product)
        {
            if (categories.TryGetValue(row.Culture, out var categorySlug))
            {
                paths[row.Culture] = new ProductPath(categorySlug, row.Slug);
            }
        }

        return paths;
    }

    public async Task<int?> CategoryIdBySlugAsync(string slug, CancellationToken ct) =>
        await _context.CategoryTranslations
            .AsNoTracking()
            .Where(t => t.Slug == slug)
            .Select(t => (int?)t.CategoryId)
            .FirstOrDefaultAsync(ct);

    public async Task<int?> ProductIdBySlugAsync(string slug, CancellationToken ct) =>
        await _context.ProductTranslations
            .AsNoTracking()
            .Where(t => t.Slug == slug)
            .Select(t => (int?)t.ProductId)
            .FirstOrDefaultAsync(ct);

    public async Task<int?> FormerOwnerAsync(SlugOwner owner, string slug, string culture, CancellationToken ct) =>
        await _context.SlugRedirects
            .AsNoTracking()
            .Where(r => r.OwnerType == owner && r.Slug == slug)
            .OrderByDescending(r => r.Culture == culture)
            .ThenByDescending(r => r.CreatedUtc)
            .Select(r => (int?)r.OwnerId)
            .FirstOrDefaultAsync(ct);

    public async Task<CategoryContext?> FindCategoryAsync(
        string slug,
        string culture,
        CancellationToken ct)
    {
        var row = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.Translations.Any(t => t.Culture == culture && t.Slug == slug))
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.ParentId,
                Name = c.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault(),
                Description = c.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Description).FirstOrDefault(),
                ParentName = c.Parent == null
                    ? null
                    : c.Parent.Translations
                        .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault(),
                ParentSlug = c.Parent == null
                    ? null
                    : c.Parent.Translations
                        .Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault(),
                MetaTitle = c.Translations
                    .Where(t => t.Culture == culture).Select(t => t.MetaTitle).FirstOrDefault(),
                MetaDescription = c.Translations
                    .Where(t => t.Culture == culture).Select(t => t.MetaDescription).FirstOrDefault(),
                Slugs = c.Translations
                    .Select(t => new SlugPair { Culture = t.Culture, Slug = t.Slug })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (row is null)
        {
            return null;
        }

        return new CategoryContext(
            row.Id,
            row.Name ?? row.Code,
            slug,
            row.Description,
            row.ParentName,
            row.ParentId is null)
        {
            SlugsByCulture = row.Slugs.ToDictionary(
                x => x.Culture, x => x.Slug, StringComparer.OrdinalIgnoreCase),
            ParentSlug = row.ParentSlug,
            MetaTitle = row.MetaTitle,
            MetaDescription = row.MetaDescription
        };
    }

    public async Task<List<ProductCardViewModel>> FeaturedAsync(
        int take,
        string culture,
        CancellationToken ct)
    {
        var source = _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Published
                        && p.IsFeatured
                        && p.Variants.Any(v => v.IsActive))
            .OrderByDescending(p => p.PublishedUtc)
            .Take(take);

        return await ProjectCardsAsync(source, culture, ct);
    }

    public async Task<List<ProductCardViewModel>> CardsAsync(
        IReadOnlyList<int> productIds,
        string culture,
        CancellationToken ct)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.ToList();

        var cards = await ProjectCardsAsync(
            _context.Products
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id) && p.Status == ProductStatus.Published),
            culture,
            ct);

        var position = ids
            .Select((id, index) => (id, index))
            .GroupBy(x => x.id)
            .ToDictionary(g => g.Key, g => g.First().index);

        return [.. cards.OrderBy(c => position[c.Id])];
    }

    // ---------------------------------------------------------------- cards

    private async Task<List<ProductCardViewModel>> ProjectCardsAsync(
        IQueryable<Product> source,
        string culture,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var newSince = now.AddDays(-30);

        var cards = await source
            .Select(p => new ProductCardViewModel
            {
                Id = p.Id,
                Name = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? p.Sku,
                Slug = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault() ?? string.Empty,
                ShortDescription = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.ShortDescription).FirstOrDefault(),
                CategorySlug = p.Category.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault() ?? string.Empty,
                BrandName = p.Brand != null ? p.Brand.Name : string.Empty,
                ImagePath = p.Images.Where(i => i.IsPrimary).Select(i => i.Path).FirstOrDefault(),
                ImageAlt = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault(),

                Price = p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price) ?? 0m,

                // Third copy of the sale-window expression; see ProductFilterService.
                SalePrice = p.Variants
                    .Where(v => v.IsActive
                        && v.SalePrice != null
                        && v.SalePrice < v.Price
                        && (v.SaleStartsUtc == null || v.SaleStartsUtc <= now)
                        && (v.SaleEndsUtc == null || v.SaleEndsUtc >= now))
                    .Min(v => v.SalePrice),

                IsInStock = p.Variants.Any(v => v.IsActive && v.StockQuantity > 0),
                IsNew = p.PublishedUtc != null && p.PublishedUtc >= newSince,
                StockLeft = p.Variants.Where(v => v.IsActive).Sum(v => (int?)v.StockQuantity) ?? 0
            })
            .ToListAsync(ct);

        if (cards.Count > 0)
        {
            await AttachVariantSummariesAsync(cards, culture, ct);
        }

        return cards;
    }

    /// <summary>
    /// Fills in the chips that stand in for a photograph and the size run under
    /// them. Fetched flat for the whole page and assembled in memory — as a
    /// nested projection EF could only translate it by fanning out one join per
    /// attribute, which gets expensive fast on a 48-product page.
    /// </summary>
    private async Task AttachVariantSummariesAsync(
        List<ProductCardViewModel> cards,
        string culture,
        CancellationToken ct)
    {
        var productIds = cards.Select(c => c.Id).ToList();

        var rows = await _context.VariantAttributeValues
            .AsNoTracking()
            .Where(v => productIds.Contains(v.ProductVariant.ProductId) && v.ProductVariant.IsActive)
            .Select(v => new SummaryRow
            {
                ProductId = v.ProductVariant.ProductId,
                AttributeCode = v.AttributeDefinition.Code,
                Unit = v.AttributeDefinition.Unit,
                NumericValue = v.NumericValue,
                OptionOrder = v.AttributeOption != null ? v.AttributeOption.DisplayOrder : 0,
                OptionHex = v.AttributeOption != null ? v.AttributeOption.SwatchHex : null,
                OptionLabel = v.AttributeOption == null
                    ? null
                    : v.AttributeOption.Translations
                        .Where(t => t.Culture == culture).Select(t => t.Label).FirstOrDefault()
            })
            .ToListAsync(ct);

        var byProduct = rows.GroupBy(r => r.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var card in cards)
        {
            if (!byProduct.TryGetValue(card.Id, out var values))
            {
                continue;
            }

            // Colour and shade never both appear on one product, so they share
            // a single chip strip.
            var swatches = values
                .Where(v => v.AttributeCode is ColorAttribute or ShadeAttribute && v.OptionHex is not null)
                .GroupBy(v => v.OptionHex!)
                .Select(g => g.OrderBy(v => v.OptionOrder).First())
                .OrderBy(v => v.OptionOrder)
                .ToList();

            card.SwatchCount = swatches.Count;
            card.Swatches = [.. swatches
                .Take(MaxChips)
                .Select(v => new SwatchViewModel { Label = v.OptionLabel ?? string.Empty, Hex = v.OptionHex! })];

            if (swatches.Count == 0)
            {
                card.TextChips = [.. values
                    .Where(v => v.AttributeCode == VolumeAttribute && v.NumericValue is not null)
                    .Select(v => FormatNumeric(v.NumericValue!.Value, v.Unit))
                    .Distinct()
                    .Take(MaxChips)];
            }

            var sizes = values
                .Where(v => v.AttributeCode == SizeAttribute && v.OptionLabel is not null)
                .GroupBy(v => v.OptionLabel!)
                .Select(g => g.First())
                .OrderBy(v => v.OptionOrder)
                .Select(v => v.OptionLabel!)
                .ToList();

            card.SizeRun = sizes.Count switch
            {
                0 => null,
                1 => sizes[0],
                _ => $"{sizes[0]}\u2013{sizes[^1]}"
            };

            card.Note = values
                .Where(v => v.AttributeCode == SkinTypeAttribute)
                .Select(v => v.OptionLabel)
                .FirstOrDefault(label => label is not null);
        }
    }

    // --------------------------------------------------------- filter panel

    /// <summary>
    /// The panel only offers attributes that the categories in view actually
    /// use, so a cosmetics page never shows a dress-size filter. Counts are
    /// deliberately not shown per option: a truthful count has to be computed
    /// against the other filters already applied, which is a query per option.
    /// </summary>
    private async Task<FilterPanelViewModel> BuildFilterPanelAsync(
        ProductQuery query,
        string culture,
        CancellationToken ct)
    {
        var scope = _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Published);

        if (query.CategoryId is { } categoryId)
        {
            scope = scope.Where(p => p.CategoryId == categoryId || p.Category.ParentId == categoryId);
        }

        var categoryIds = await scope.Select(p => p.CategoryId).Distinct().ToListAsync(ct);

        var groups = await _context.CategoryAttributes
            .AsNoTracking()
            .Where(ca => categoryIds.Contains(ca.CategoryId) && ca.AttributeDefinition.IsFilterable)
            .Select(ca => ca.AttributeDefinition)
            .Distinct()
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new FilterGroupViewModel
            {
                Code = a.Code,
                Name = a.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? a.Code,
                Unit = a.Unit,
                Options = a.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new FilterOptionViewModel
                    {
                        Code = o.Code,
                        Label = o.Translations
                            .Where(t => t.Culture == culture).Select(t => t.Label).FirstOrDefault() ?? o.Code,
                        SwatchHex = o.SwatchHex
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        // Numeric attributes such as volume have no option rows, so they would
        // render as an empty group. Drop them here; the price slider covers
        // the numeric case and phase 5 can add ranged numeric filters.
        groups = [.. groups.Where(g => g.Options.Count > 0)];

        foreach (var group in groups)
        {
            if (query.Attributes.TryGetValue(group.Code, out var selected))
            {
                foreach (var option in group.Options)
                {
                    option.IsSelected = selected.Contains(option.Code, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        var brands = await scope
            .Where(p => p.Brand != null)
            .Select(p => p.Brand!)
            .Distinct()
            .OrderBy(b => b.Name)
            .Select(b => new FilterOptionViewModel { Code = b.Slug, Label = b.Name })
            .ToListAsync(ct);

        foreach (var brand in brands)
        {
            brand.IsSelected = query.Brands.Contains(brand.Code, StringComparer.OrdinalIgnoreCase);
        }

        var prices = await scope
            .SelectMany(p => p.Variants.Where(v => v.IsActive))
            .Select(v => v.Price)
            .ToListAsync(ct);

        return new FilterPanelViewModel
        {
            Groups = groups,
            Brands = brands,
            LowestPrice = prices.Count > 0 ? Math.Floor(prices.Min()) : 0m,
            HighestPrice = prices.Count > 0 ? Math.Ceiling(prices.Max()) : 0m
        };
    }

    // ------------------------------------------------------------- details

    public async Task<ProductDetailsViewModel?> FindProductAsync(
        string slug,
        string culture,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Published
                        && p.Translations.Any(t => t.Culture == culture && t.Slug == slug))
            .Select(p => new ProductDetailsViewModel
            {
                Id = p.Id,
                Sku = p.Sku,
                Slug = slug,
                Name = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? p.Sku,
                ShortDescription = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.ShortDescription).FirstOrDefault(),
                Description = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Description).FirstOrDefault(),
                Ingredients = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Ingredients).FirstOrDefault(),
                CareInstructions = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.CareInstructions).FirstOrDefault(),
                MetaTitle = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.MetaTitle).FirstOrDefault(),
                MetaDescription = p.Translations
                    .Where(t => t.Culture == culture).Select(t => t.MetaDescription).FirstOrDefault(),

                BrandName = p.Brand != null ? p.Brand.Name : string.Empty,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? string.Empty,
                CategorySlug = p.Category.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault() ?? string.Empty,
                ParentCategoryName = p.Category.Parent == null
                    ? null
                    : p.Category.Parent.Translations
                        .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault(),
                ParentCategorySlug = p.Category.Parent == null
                    ? null
                    : p.Category.Parent.Translations
                        .Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault(),

                SlugPairs = p.Translations
                    .Select(t => new SlugPair { Culture = t.Culture, Slug = t.Slug })
                    .ToList(),
                CategorySlugPairs = p.Category.Translations
                    .Select(t => new SlugPair { Culture = t.Culture, Slug = t.Slug })
                    .ToList(),

                Images = p.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => new ProductImageViewModel
                    {
                        Path = i.Path,
                        Alt = culture == "sr" ? i.AltTextSr : i.AltTextEn
                    })
                    .ToList(),

                Variants = p.Variants
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Sku)
                    .Select(v => new VariantViewModel
                    {
                        Id = v.Id,
                        Sku = v.Sku,
                        Price = v.Price,
                        SalePrice = v.SalePrice != null
                            && v.SalePrice < v.Price
                            && (v.SaleStartsUtc == null || v.SaleStartsUtc <= now)
                            && (v.SaleEndsUtc == null || v.SaleEndsUtc >= now)
                                ? v.SalePrice
                                : null,
                        StockQuantity = v.StockQuantity,
                        LowStockThreshold = v.LowStockThreshold,
                        ExpiryDate = v.ExpiryDate,
                        Values = v.AttributeValues
                            .Select(av => new VariantValueViewModel
                            {
                                AttributeCode = av.AttributeDefinition.Code,
                                AttributeName = av.AttributeDefinition.Translations
                                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                                    ?? av.AttributeDefinition.Code,
                                IsVariantDefining = av.AttributeDefinition.IsVariantDefining,
                                DisplayOrder = av.AttributeDefinition.DisplayOrder,
                                Unit = av.AttributeDefinition.Unit,
                                OptionCode = av.AttributeOption != null ? av.AttributeOption.Code : null,
                                OptionOrder = av.AttributeOption != null ? av.AttributeOption.DisplayOrder : 0,
                                SwatchHex = av.AttributeOption != null ? av.AttributeOption.SwatchHex : null,
                                OptionLabel = av.AttributeOption == null
                                    ? null
                                    : av.AttributeOption.Translations
                                        .Where(t => t.Culture == culture).Select(t => t.Label).FirstOrDefault(),
                                NumericValue = av.NumericValue,
                                TextValue = av.TextValue
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (product is null)
        {
            return null;
        }

        product.BuildAlternateSlugs();
        product.BuildPickerGroups();

        product.Related = await ProjectCardsAsync(
            _context.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.Published
                            && p.CategoryId == product.CategoryId
                            && p.Id != product.Id
                            && p.Variants.Any(v => v.IsActive))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.PublishedUtc)
                .Take(4),
            culture,
            ct);

        return product;
    }

    private static string FormatNumeric(decimal value, string? unit) =>
        $"{value.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)} {unit}".Trim();

    /// <summary>One translation of a slug. Named so EF can project into it.</summary>
    public sealed class SlugPair
    {
        public string Culture { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    /// <summary>Flat shape for the summary query. Named so EF can project into it.</summary>
    private sealed class SummaryRow
    {
        public int ProductId { get; init; }
        public string AttributeCode { get; init; } = string.Empty;
        public string? Unit { get; init; }
        public decimal? NumericValue { get; init; }
        public int OptionOrder { get; init; }
        public string? OptionHex { get; init; }
        public string? OptionLabel { get; init; }
    }
}
