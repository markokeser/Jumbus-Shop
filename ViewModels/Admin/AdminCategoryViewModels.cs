using System.ComponentModel.DataAnnotations;

namespace WebShop.ViewModels.Admin;

public class CategoryRow
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public bool IsChild => ParentId is not null;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? SlugSr { get; set; }

    public string? SlugEn { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public int Products { get; set; }

    public int Children { get; set; }

    public bool CanDelete => Products == 0 && Children == 0;
}

public class CategoryFormViewModel
{
    public int Id { get; set; }

    public bool IsNew => Id == 0;

    /// <summary>Internal name used by the seeder and tooling. Fixed once the category exists.</summary>
    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(64)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Validation_Slug")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Empty for a top-level category (Clothing); a top-level one for a subcategory (Dresses).</summary>
    public int? ParentId { get; set; }

    [Range(0, 9999, ErrorMessage = "Validation_Range")]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public CategoryTranslationForm Sr { get; set; } = new();

    public CategoryTranslationForm En { get; set; } = new();

    /// <summary>One row per attribute in the shop; the ticked ones apply to this category.</summary>
    public List<CategoryAttributeField> Attributes { get; set; } = [];

    // Shown only, never bound back.

    public List<AdminOption> Parents { get; set; } = [];

    public bool HasChildren { get; set; }

    public int ProductCount { get; set; }

    public string? PublicUrlSr { get; set; }
}

public class CategoryTranslationForm
{
    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Left empty, it is made from the name.</summary>
    [StringLength(200)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Validation_Slug")]
    public string? Slug { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(400)]
    public string? MetaDescription { get; set; }
}

public class CategoryAttributeField
{
    public int AttributeDefinitionId { get; set; }

    public bool Assigned { get; set; }

    public bool IsRequired { get; set; } = true;

    [Range(0, 999, ErrorMessage = "Validation_Range")]
    public int DisplayOrder { get; set; }

    // Shown only.

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public bool IsVariantDefining { get; set; }

    /// <summary>Variants in this category hold values for it, so it cannot be removed.</summary>
    public bool InUse { get; set; }
}
