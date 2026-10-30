using System.ComponentModel.DataAnnotations;
using WebShop.Models.Catalog;

namespace WebShop.ViewModels.Admin;

// ErrorMessage values are resource keys, localised through SharedResource.

public class AdminProductQuery
{
    public string? Q { get; set; }

    public int? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public ProductStatus? Status { get; set; }

    public int Page { get; set; } = 1;
}

public class AdminProductRow
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public ProductStatus Status { get; set; }

    public bool IsFeatured { get; set; }

    public int Variants { get; set; }

    public int ActiveVariants { get; set; }

    public int Stock { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string? ImagePath { get; set; }
}

public class AdminProductListViewModel
{
    public AdminProductQuery Query { get; set; } = new();

    public List<AdminProductRow> Products { get; set; } = [];

    public AdminPage Page { get; set; } = new();

    public List<AdminOption> Categories { get; set; } = [];

    public List<AdminOption> Brands { get; set; } = [];
}

public class ProductTranslationForm
{
    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Left empty, it is made from the name.</summary>
    [StringLength(250)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Validation_Slug")]
    public string? Slug { get; set; }

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    [StringLength(20000)]
    public string? Description { get; set; }

    [StringLength(8000)]
    public string? Ingredients { get; set; }

    [StringLength(4000)]
    public string? CareInstructions { get; set; }

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(400)]
    public string? MetaDescription { get; set; }
}

/// <summary>
/// A value for one attribute: the chosen option, a number or a text,
/// depending on the attribute's type. Used for variant-defining attributes
/// on a variant and for shared ones (skin type) on the whole product.
/// </summary>
public class AttributeValueField
{
    public int AttributeDefinitionId { get; set; }

    public int? OptionId { get; set; }

    public decimal? NumericValue { get; set; }

    [StringLength(200)]
    public string? TextValue { get; set; }

    // Display only; refilled by the service after a failed post.
    public string Label { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public string? Unit { get; set; }

    public bool IsRequired { get; set; }

    public List<AdminOption> Options { get; set; } = [];

    public bool HasValue => OptionId is not null || NumericValue is not null || !string.IsNullOrWhiteSpace(TextValue);
}

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Validation_Required")]
    public int CategoryId { get; set; }

    public int? BrandId { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(64)]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Validation_Sku")]
    public string Sku { get; set; } = string.Empty;

    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    public bool IsFeatured { get; set; }

    public ProductTranslationForm Sr { get; set; } = new();

    public ProductTranslationForm En { get; set; } = new();

    /// <summary>Attributes that describe the product as a whole, such as skin type.</summary>
    public List<AttributeValueField> Shared { get; set; } = [];

    // Display only.
    public List<AdminOption> Categories { get; set; } = [];

    public List<AdminOption> Brands { get; set; } = [];

    public List<AdminVariantRow> Variants { get; set; } = [];

    public List<AdminImageRow> Images { get; set; } = [];

    /// <summary>Colour or shade options used by this product, for tying a photo to one.</summary>
    public List<AdminOption> ImageOptions { get; set; } = [];

    public bool IsNew => Id == 0;

    public bool CategoryLocked => Variants.Count > 0;

    public string? PublicUrlSr { get; set; }
}

public class AdminVariantRow
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public bool OnSaleNow { get; set; }

    public int Stock { get; set; }

    public int LowStockThreshold { get; set; }

    public bool IsActive { get; set; }

    public DateOnly? ExpiryDate { get; set; }
}

public class AdminImageRow
{
    public int Id { get; set; }

    public string Path { get; set; } = string.Empty;

    public string? AltTextSr { get; set; }

    public string? AltTextEn { get; set; }

    public bool IsPrimary { get; set; }

    public int? AttributeOptionId { get; set; }
}

public class VariantFormViewModel
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(80)]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Validation_Sku")]
    public string Sku { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "100000000", ErrorMessage = "Validation_Price")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "1", "100000000", ErrorMessage = "Validation_Price")]
    public decimal? SalePrice { get; set; }

    /// <summary>Belgrade time; converted to UTC on save.</summary>
    public DateTime? SaleStartsLocal { get; set; }

    public DateTime? SaleEndsLocal { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Validation_Stock")]
    public int StockQuantity { get; set; }

    /// <summary>The stock shown when the form opened; a changed value means an order came in meanwhile.</summary>
    public int OriginalStock { get; set; }

    [Range(0, 10_000, ErrorMessage = "Validation_Stock")]
    public int LowStockThreshold { get; set; } = 3;

    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    [Range(1, 100_000, ErrorMessage = "Validation_Weight")]
    public int? WeightGrams { get; set; }

    public bool IsActive { get; set; } = true;

    public List<AttributeValueField> Attributes { get; set; } = [];

    // Display only.
    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public bool HasOrders { get; set; }

    public bool IsNew => Id == 0;
}

public enum VariantSaveOutcome
{
    Saved = 0,
    Invalid = 1,
    NotFound = 2,
    StockConflict = 3
}

public class AdminStockQuery
{
    public string? Q { get; set; }

    /// <summary>Default view: only what needs reordering.</summary>
    public bool LowOnly { get; set; } = true;

    public int Page { get; set; } = 1;
}

public class StockRow
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Stock { get; set; }

    public int Threshold { get; set; }

    public bool IsActive { get; set; }

    public bool IsLow => Stock <= Threshold;
}

public class AdminStockViewModel
{
    public AdminStockQuery Query { get; set; } = new();

    public List<StockRow> Rows { get; set; } = [];

    public AdminPage Page { get; set; } = new();
}

public enum StockSetOutcome
{
    Updated = 0,
    NotFound = 1,
    Invalid = 2,
    Conflict = 3
}

public record StockSetResult(StockSetOutcome Outcome, int Current);

public class BrandRow
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int Products { get; set; }
}

public class BrandFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Validation_Slug")]
    public string? Slug { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AttributeRow
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; }

    public string? Unit { get; set; }

    public bool IsVariantDefining { get; set; }

    public List<string> Categories { get; set; } = [];

    public int Options { get; set; }
}

public class AttributeOptionRow
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string? LabelSr { get; set; }

    public string? LabelEn { get; set; }

    public string? SwatchHex { get; set; }

    public int DisplayOrder { get; set; }

    public int Uses { get; set; }
}

public class AttributeOptionsViewModel
{
    public AttributeRow Attribute { get; set; } = new();

    public List<AttributeOptionRow> Options { get; set; } = [];
}

public class AttributeOptionFormViewModel
{
    public int Id { get; set; }

    public int AttributeDefinitionId { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(64)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Validation_Slug")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(120)]
    public string LabelSr { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(120)]
    public string LabelEn { get; set; } = string.Empty;

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Validation_Hex")]
    public string? SwatchHex { get; set; }

    [Range(0, 10_000)]
    public int DisplayOrder { get; set; }

    // Display only.
    public string AttributeName { get; set; } = string.Empty;

    public bool ShowSwatch { get; set; }
}
