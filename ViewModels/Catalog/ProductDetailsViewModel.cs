using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebShop.ViewModels.Reviews;

namespace WebShop.ViewModels.Catalog;

public class ProductDetailsViewModel
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public string? Ingredients { get; set; }

    public string? CareInstructions { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string CategorySlug { get; set; } = string.Empty;

    public string? ParentCategoryName { get; set; }

    public string? ParentCategorySlug { get; set; }

    /// <summary>Raw translations, filled by the query and folded into the dictionaries below.</summary>
    public List<Services.Catalog.CatalogService.SlugPair> SlugPairs { get; set; } = [];

    public List<Services.Catalog.CatalogService.SlugPair> CategorySlugPairs { get; set; } = [];

    /// <summary>This product's slug in every language.</summary>
    public IReadOnlyDictionary<string, string> SlugsByCulture { get; private set; } =
        new Dictionary<string, string>();

    /// <summary>Its category's slug in every language.</summary>
    public IReadOnlyDictionary<string, string> CategorySlugsByCulture { get; private set; } =
        new Dictionary<string, string>();

    public List<ProductImageViewModel> Images { get; set; } = [];

    public List<VariantViewModel> Variants { get; set; } = [];

    public List<PickerGroupViewModel> PickerGroups { get; set; } = [];

    public List<ProductCardViewModel> Related { get; set; } = [];

    public ReviewSummaryViewModel Reviews { get; set; } = new();

    public bool IsInWishlist { get; set; }

    /// <summary>Attributes that are the same across every variant, shown as plain facts.</summary>
    public List<VariantValueViewModel> SharedFacts { get; set; } = [];

    public bool IsInStock => Variants.Any(v => v.StockQuantity > 0);

    public decimal LowestPrice =>
        Variants.Count == 0 ? 0m : Variants.Min(v => v.EffectivePrice);

    public decimal HighestPrice =>
        Variants.Count == 0 ? 0m : Variants.Max(v => v.EffectivePrice);

    public bool HasPriceRange => HighestPrice > LowestPrice;

    public bool IsOnSale => Variants.Any(v => v.IsOnSale);

    /// <summary>
    /// The variant the page opens on: the cheapest one that is actually in
    /// stock, falling back to the cheapest overall so an entirely sold-out
    /// product still renders a price.
    /// </summary>
    public VariantViewModel? DefaultVariant =>
        Variants.Where(v => v.StockQuantity > 0).OrderBy(v => v.EffectivePrice).FirstOrDefault()
        ?? Variants.OrderBy(v => v.EffectivePrice).FirstOrDefault();

    /// <summary>
    /// Every variant as JSON for the picker script: which combination of
    /// options it is, what it costs and whether it can be bought. Doing the
    /// lookup in the browser keeps changing a size from being a round trip.
    /// </summary>
    public string VariantsJson => JsonSerializer.Serialize(
        Variants.Select(v => new VariantPayload
        {
            Id = v.Id,
            Sku = v.Sku,
            Price = v.Price,
            Sale = v.SalePrice,
            Stock = v.StockQuantity,
            Low = v.StockQuantity > 0 && v.StockQuantity <= v.LowStockThreshold,
            Attrs = v.Values
                .Where(x => x.IsVariantDefining)
                .ToDictionary(x => x.AttributeCode, x => x.PickerValue)
        }),
        JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Folds the translation rows into lookups the language switcher can use.
    /// Without this the switcher would send /sr/haljine/koktel-haljina-vera
    /// to /en/haljine/koktel-haljina-vera, which is not a page in English.
    /// </summary>
    public void BuildAlternateSlugs()
    {
        SlugsByCulture = SlugPairs.ToDictionary(
            x => x.Culture, x => x.Slug, StringComparer.OrdinalIgnoreCase);

        CategorySlugsByCulture = CategorySlugPairs.ToDictionary(
            x => x.Culture, x => x.Slug, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Turns the variants into one group of choices per variant-defining
    /// attribute. An attribute with only one distinct value across every
    /// variant is not a choice at all, so it moves to SharedFacts instead of
    /// rendering as a single unclickable button.
    /// </summary>
    public void BuildPickerGroups()
    {
        var defining = Variants
            .SelectMany(v => v.Values)
            .Where(v => v.IsVariantDefining)
            .ToList();

        PickerGroups = [.. defining
            .GroupBy(v => v.AttributeCode)
            .Select(group => new PickerGroupViewModel
            {
                Code = group.Key,
                Name = group.First().AttributeName,
                Unit = group.First().Unit,
                DisplayOrder = group.First().DisplayOrder,
                Options = [.. group
                    .GroupBy(v => v.PickerValue)
                    .Select(byValue => byValue.OrderBy(v => v.OptionOrder).First())
                    .OrderBy(v => v.OptionOrder)
                    .ThenBy(v => v.NumericValue)
                    .Select(v => new PickerOptionViewModel
                    {
                        Value = v.PickerValue,
                        Label = v.DisplayValue,
                        SwatchHex = v.SwatchHex
                    })]
            })
            .Where(g => g.Options.Count > 1)
            .OrderBy(g => g.DisplayOrder)];

        var pickedCodes = PickerGroups.Select(g => g.Code).ToHashSet();

        SharedFacts = [.. Variants
            .SelectMany(v => v.Values)
            .Where(v => !pickedCodes.Contains(v.AttributeCode))
            .GroupBy(v => v.AttributeCode)
            .Select(g => g.First())
            .OrderBy(v => v.DisplayOrder)];
    }

    private sealed class VariantPayload
    {
        public int Id { get; init; }
        public string Sku { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public decimal? Sale { get; init; }
        public int Stock { get; init; }
        public bool Low { get; init; }
        public Dictionary<string, string> Attrs { get; init; } = [];
    }
}

public class ProductImageViewModel
{
    public string Path { get; set; } = string.Empty;

    public string? Alt { get; set; }
}

public class VariantViewModel
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>Already filtered to an active sale window by the catalog service.</summary>
    public decimal? SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public int LowStockThreshold { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public List<VariantValueViewModel> Values { get; set; } = [];

    public bool IsOnSale => SalePrice is { } sale && sale < Price;

    public decimal EffectivePrice => IsOnSale ? SalePrice!.Value : Price;
}

public class VariantValueViewModel
{
    public string AttributeCode { get; set; } = string.Empty;

    public string AttributeName { get; set; } = string.Empty;

    public bool IsVariantDefining { get; set; }

    public int DisplayOrder { get; set; }

    public string? Unit { get; set; }

    public string? OptionCode { get; set; }

    public int OptionOrder { get; set; }

    public string? SwatchHex { get; set; }

    public string? OptionLabel { get; set; }

    public decimal? NumericValue { get; set; }

    public string? TextValue { get; set; }

    /// <summary>
    /// Stable key the picker matches on. Numeric values are formatted with the
    /// invariant culture so the value in the markup and the value in the JSON
    /// agree — a Serbian decimal comma here would break every comparison.
    /// </summary>
    public string PickerValue =>
        OptionCode
        ?? NumericValue?.ToString("0.##", CultureInfo.InvariantCulture)
        ?? TextValue
        ?? string.Empty;

    /// <summary>What the shopper reads on the button.</summary>
    public string DisplayValue =>
        OptionLabel
        ?? (NumericValue is { } n
            ? $"{n.ToString("0.##", CultureInfo.CurrentCulture)} {Unit}".Trim()
            : TextValue)
        ?? string.Empty;
}

public class PickerGroupViewModel
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public int DisplayOrder { get; set; }

    public List<PickerOptionViewModel> Options { get; set; } = [];

    public bool IsSwatchGroup => Options.Any(o => o.SwatchHex is not null);
}

public class PickerOptionViewModel
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? SwatchHex { get; set; }
}
