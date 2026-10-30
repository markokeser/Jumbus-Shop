using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.Services.Catalog;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

public interface IAdminProductService
{
    Task<AdminProductListViewModel> ListAsync(AdminProductQuery query, string culture, CancellationToken ct);

    Task<ProductFormViewModel> NewFormAsync(string culture, CancellationToken ct);

    Task<ProductFormViewModel?> GetFormAsync(int id, string culture, CancellationToken ct);

    /// <summary>Refills everything a posted form does not carry: lists, variants, images.</summary>
    Task FillAsync(ProductFormViewModel form, string culture, CancellationToken ct);

    Task<SaveResult> SaveAsync(ProductFormViewModel form, string culture, CancellationToken ct);
}

public class AdminProductService : IAdminProductService
{
    private const int PageSize = 25;
    private static readonly string[] Cultures = ["sr", "en"];

    private readonly ApplicationDbContext _context;
    private readonly LinkGenerator _links;

    public AdminProductService(ApplicationDbContext context, LinkGenerator links)
    {
        _context = context;
        _links = links;
    }

    public async Task<AdminProductListViewModel> ListAsync(AdminProductQuery query, string culture, CancellationToken ct)
    {
        var products = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            products = products.Where(p => p.Sku.Contains(term)
                || p.Translations.Any(t => t.Name.Contains(term))
                || p.Variants.Any(v => v.Sku.Contains(term)));
        }

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId || p.Category.ParentId == categoryId);
        }

        if (query.BrandId is { } brandId)
        {
            products = products.Where(p => p.BrandId == brandId);
        }

        if (query.Status is { } status)
        {
            products = products.Where(p => p.Status == status);
        }

        var total = await products.CountAsync(ct);
        var page = AdminPage.Of(query.Page, PageSize, total);

        var rows = await products
            .OrderByDescending(p => p.CreatedUtc)
            .ThenByDescending(p => p.Id)
            .Skip(page.Skip)
            .Take(page.Size)
            .Select(p => new AdminProductRow
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? p.Sku,
                Category = p.Category.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? p.Category.Code,
                Brand = p.Brand != null ? p.Brand.Name : null,
                Status = p.Status,
                IsFeatured = p.IsFeatured,
                Variants = p.Variants.Count(),
                ActiveVariants = p.Variants.Count(v => v.IsActive),
                Stock = p.Variants.Where(v => v.IsActive).Sum(v => (int?)v.StockQuantity) ?? 0,
                MinPrice = p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price),
                MaxPrice = p.Variants.Where(v => v.IsActive).Max(v => (decimal?)v.Price),
                ImagePath = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.Path).FirstOrDefault()
            })
            .ToListAsync(ct);

        return new AdminProductListViewModel
        {
            Query = query,
            Page = page,
            Products = rows,
            Categories = await CategoryOptionsAsync(culture, leavesOnly: false, ct),
            Brands = await BrandOptionsAsync(ct)
        };
    }

    public async Task<ProductFormViewModel> NewFormAsync(string culture, CancellationToken ct)
    {
        var form = new ProductFormViewModel();
        await FillAsync(form, culture, ct);
        return form;
    }

    public async Task<ProductFormViewModel?> GetFormAsync(int id, string culture, CancellationToken ct)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return null;
        }

        var form = new ProductFormViewModel
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            Sku = product.Sku,
            Status = product.Status,
            IsFeatured = product.IsFeatured,
            Sr = ToForm(product.Translations.FirstOrDefault(t => t.Culture == "sr")),
            En = ToForm(product.Translations.FirstOrDefault(t => t.Culture == "en"))
        };

        // Shared attributes live on every variant with the same value, so any
        // one variant tells us the product's value.
        var stored = await _context.VariantAttributeValues
            .AsNoTracking()
            .Where(v => v.ProductVariant.ProductId == id && !v.AttributeDefinition.IsVariantDefining)
            .Select(v => new { v.AttributeDefinitionId, v.AttributeOptionId, v.NumericValue, v.TextValue })
            .ToListAsync(ct);

        form.Shared = [.. stored
            .GroupBy(v => v.AttributeDefinitionId)
            .Select(g => g.First())
            .Select(v => new AttributeValueField
            {
                AttributeDefinitionId = v.AttributeDefinitionId,
                OptionId = v.AttributeOptionId,
                NumericValue = v.NumericValue,
                TextValue = v.TextValue
            })];

        await FillAsync(form, culture, ct);
        return form;
    }

    public async Task FillAsync(ProductFormViewModel form, string culture, CancellationToken ct)
    {
        form.Categories = await CategoryOptionsAsync(culture, leavesOnly: true, ct);
        form.Brands = await BrandOptionsAsync(ct);

        var categoryId = form.CategoryId;

        if (form.Id > 0)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == form.Id)
                .Select(p => new
                {
                    p.CategoryId,
                    p.Status,
                    CategorySlug = p.Category.Translations.Where(t => t.Culture == "sr").Select(t => t.Slug).FirstOrDefault(),
                    Slug = p.Translations.Where(t => t.Culture == "sr").Select(t => t.Slug).FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);

            if (product is not null)
            {
                // The stored category, not the posted one: a refused category
                // change must not swap the attribute fields under the admin.
                categoryId = product.CategoryId;

                if (product.Status == ProductStatus.Published && product.CategorySlug is not null && product.Slug is not null)
                {
                    form.PublicUrlSr = _links.GetPathByAction("Details", "Products", new
                    {
                        area = "",
                        culture = "sr",
                        categorySlug = product.CategorySlug,
                        productSlug = product.Slug
                    });
                }
            }

            form.Variants = await VariantRowsAsync(form.Id, culture, ct);
            form.Images = await _context.ProductImages
                .AsNoTracking()
                .Where(i => i.ProductId == form.Id)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .ThenBy(i => i.Id)
                .Select(i => new AdminImageRow
                {
                    Id = i.Id,
                    Path = i.Path,
                    AltTextSr = i.AltTextSr,
                    AltTextEn = i.AltTextEn,
                    IsPrimary = i.IsPrimary,
                    AttributeOptionId = i.AttributeOptionId
                })
                .ToListAsync(ct);

            // Photos can be tied to a colour or shade the product actually comes in.
            var used = await _context.VariantAttributeValues
                .AsNoTracking()
                .Where(v => v.ProductVariant.ProductId == form.Id
                    && v.AttributeOptionId != null
                    && v.AttributeOption!.SwatchHex != null)
                .Select(v => new
                {
                    Id = v.AttributeOptionId!.Value,
                    v.AttributeOption!.Code,
                    Label = v.AttributeOption.Translations.Where(t => t.Culture == culture).Select(t => t.Label).FirstOrDefault()
                })
                .ToListAsync(ct);

            form.ImageOptions = [.. used
                .DistinctBy(o => o.Id)
                .OrderBy(o => o.Id)
                .Select(o => new AdminOption(o.Id.ToString(CultureInfo.InvariantCulture), o.Label ?? o.Code))];
        }

        form.Shared = categoryId > 0
            ? AttributeFields.Merge(
                await AttributeFields.ForCategoryAsync(_context, categoryId, variantDefining: false, culture, ct),
                form.Shared)
            : [];
    }

    public async Task<SaveResult> SaveAsync(ProductFormViewModel form, string culture, CancellationToken ct)
    {
        var errors = new List<FieldError>();
        var sku = form.Sku.Trim();

        if (await _context.Products.AnyAsync(p => p.Sku == sku && p.Id != form.Id, ct))
        {
            errors.Add(new(nameof(form.Sku), "Admin_SkuTaken"));
        }

        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == form.CategoryId)
            .Select(c => new { c.Id, c.ParentId })
            .FirstOrDefaultAsync(ct);

        // Products sit in subcategories (Dresses, Skincare), never in a top
        // level one: the catalogue's attributes and filters are set per subcategory.
        if (category is null || category.ParentId is null)
        {
            errors.Add(new(nameof(form.CategoryId), "Admin_CategoryLeaf"));
        }

        if (form.BrandId is { } brandId && !await _context.Brands.AnyAsync(b => b.Id == brandId, ct))
        {
            errors.Add(new(nameof(form.BrandId), "Validation_Required"));
        }

        Product? product = null;

        if (form.Id > 0)
        {
            product = await _context.Products
                .Include(p => p.Translations)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == form.Id, ct);

            if (product is null)
            {
                return SaveResult.Missing();
            }

            // Variants are described by the category's attributes (size and
            // colour for dresses, shade for makeup), so moving a product with
            // variants to another kind of category would leave them meaningless.
            if (product.CategoryId != form.CategoryId && product.Variants.Count > 0)
            {
                errors.Add(new(nameof(form.CategoryId), "Admin_CategoryLocked"));
            }
        }

        if (form.Status == ProductStatus.Published && (product is null || !product.Variants.Any(v => v.IsActive)))
        {
            errors.Add(new(nameof(form.Status), "Admin_PublishNeedsVariant"));
        }

        var slugs = new Dictionary<string, string>();

        foreach (var code in Cultures)
        {
            var translation = code == "sr" ? form.Sr : form.En;
            var field = code == "sr" ? nameof(form.Sr) : nameof(form.En);
            var typed = AdminText.Clean(translation.Slug);
            var slug = typed ?? SlugGenerator.Generate(translation.Name);

            if (string.IsNullOrEmpty(slug))
            {
                errors.Add(new($"{field}.{nameof(translation.Slug)}", "Validation_Slug"));
                continue;
            }

            if (typed is not null)
            {
                if (await SlugTakenAsync(code, slug, form.Id, ct))
                {
                    errors.Add(new($"{field}.{nameof(translation.Slug)}", "Admin_SlugTaken"));
                }
            }
            else
            {
                slug = await FreeSlugAsync(code, slug, form.Id, ct);
            }

            slugs[code] = slug;
        }

        // Shared attributes are checked against the stored category's own list.
        var shared = new List<AttributeValueField>();

        if (product is not null && product.Variants.Count > 0)
        {
            shared = AttributeFields.Merge(
                await AttributeFields.ForCategoryAsync(_context, product.CategoryId, variantDefining: false, culture, ct),
                form.Shared);

            for (var i = 0; i < shared.Count; i++)
            {
                if (AttributeFields.Problem(shared[i]) is { } problem)
                {
                    errors.Add(new($"{nameof(form.Shared)}[{i}].{FieldFor(shared[i])}", problem));
                }
            }
        }

        if (errors.Count > 0)
        {
            return SaveResult.Fail(errors);
        }

        // Read before this save can publish the product: only an address
        // that has been public is worth redirecting.
        var wasPublic = product?.PublishedUtc is not null;
        var oldSlugs = product?.Translations.ToDictionary(t => t.Culture, t => t.Slug)
            ?? new Dictionary<string, string>();

        if (product is null)
        {
            product = new Product { CreatedUtc = DateTime.UtcNow };
            _context.Products.Add(product);
        }

        foreach (var code in Cultures)
        {
            await SlugHistory.RecordAsync(
                _context, SlugOwner.Product, product.Id, code,
                wasPublic ? oldSlugs.GetValueOrDefault(code) : null, slugs[code], ct);
        }

        product.Sku = sku;
        product.CategoryId = form.CategoryId;
        product.BrandId = form.BrandId;
        product.IsFeatured = form.IsFeatured;
        product.Status = form.Status;

        if (form.Status == ProductStatus.Published)
        {
            product.PublishedUtc ??= DateTime.UtcNow;
        }

        Apply(product, "sr", form.Sr, slugs["sr"]);
        Apply(product, "en", form.En, slugs["en"]);

        if (shared.Count > 0)
        {
            await ApplySharedAsync(product, shared, ct);
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A unique index caught what the checks above raced against —
            // the same SKU or slug saved from another tab a moment ago.
            return SaveResult.Fail([new(string.Empty, "Admin_SaveConflict")]);
        }

        return SaveResult.Ok(product.Id);
    }

    // ------------------------------------------------------------- helpers

    private static string FieldFor(AttributeValueField field) => field.DataType switch
    {
        AttributeDataType.Option => nameof(field.OptionId),
        AttributeDataType.Numeric => nameof(field.NumericValue),
        _ => nameof(field.TextValue)
    };

    private static ProductTranslationForm ToForm(ProductTranslation? t) => t is null
        ? new ProductTranslationForm()
        : new ProductTranslationForm
        {
            Name = t.Name,
            Slug = t.Slug,
            ShortDescription = t.ShortDescription,
            Description = t.Description,
            Ingredients = t.Ingredients,
            CareInstructions = t.CareInstructions,
            MetaTitle = t.MetaTitle,
            MetaDescription = t.MetaDescription
        };

    private static void Apply(Product product, string culture, ProductTranslationForm form, string slug)
    {
        var translation = product.Translations.FirstOrDefault(t => t.Culture == culture);

        if (translation is null)
        {
            translation = new ProductTranslation { Culture = culture };
            product.Translations.Add(translation);
        }

        translation.Name = form.Name.Trim();
        translation.Slug = slug;
        translation.ShortDescription = AdminText.Clean(form.ShortDescription);
        translation.Description = AdminText.Clean(form.Description);
        translation.Ingredients = AdminText.Clean(form.Ingredients);
        translation.CareInstructions = AdminText.Clean(form.CareInstructions);
        translation.MetaTitle = AdminText.Clean(form.MetaTitle);
        translation.MetaDescription = AdminText.Clean(form.MetaDescription);
    }

    /// <summary>Writes each shared value onto every variant, which is how the catalogue filters read it.</summary>
    private async Task ApplySharedAsync(Product product, List<AttributeValueField> shared, CancellationToken ct)
    {
        var variantIds = product.Variants.Select(v => v.Id).ToList();
        var definitionIds = shared.Select(s => s.AttributeDefinitionId).ToList();

        var existing = await _context.VariantAttributeValues
            .Where(v => variantIds.Contains(v.ProductVariantId) && definitionIds.Contains(v.AttributeDefinitionId))
            .ToListAsync(ct);

        foreach (var field in shared)
        {
            foreach (var variantId in variantIds)
            {
                var row = existing.FirstOrDefault(r => r.ProductVariantId == variantId && r.AttributeDefinitionId == field.AttributeDefinitionId);

                if (!AttributeFields.HasTypedValue(field))
                {
                    if (row is not null)
                    {
                        _context.VariantAttributeValues.Remove(row);
                    }

                    continue;
                }

                if (row is null)
                {
                    row = new VariantAttributeValue { ProductVariantId = variantId, AttributeDefinitionId = field.AttributeDefinitionId };
                    _context.VariantAttributeValues.Add(row);
                }

                AttributeFields.Apply(field, row);
            }
        }
    }

    private Task<bool> SlugTakenAsync(string culture, string slug, int productId, CancellationToken ct) =>
        _context.ProductTranslations.AnyAsync(t => t.Culture == culture && t.Slug == slug && t.ProductId != productId, ct);

    /// <summary>"koktel-haljina", then "koktel-haljina-2", and so on.</summary>
    private async Task<string> FreeSlugAsync(string culture, string slug, int productId, CancellationToken ct)
    {
        var candidate = slug;

        for (var n = 2; await SlugTakenAsync(culture, candidate, productId, ct); n++)
        {
            candidate = $"{slug}-{n.ToString(CultureInfo.InvariantCulture)}";
        }

        return candidate;
    }

    private async Task<List<AdminVariantRow>> VariantRowsAsync(int productId, string culture, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var variants = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Id)
            .ToListAsync(ct);

        var labels = await VariantLabels.LoadAsync(_context, [.. variants.Select(v => v.Id)], culture, ct);

        return [.. variants.Select(v => new AdminVariantRow
        {
            Id = v.Id,
            Sku = v.Sku,
            Label = labels.GetValueOrDefault(v.Id, string.Empty),
            Price = v.Price,
            SalePrice = v.SalePrice,
            OnSaleNow = v.IsOnSale(now),
            Stock = v.StockQuantity,
            LowStockThreshold = v.LowStockThreshold,
            IsActive = v.IsActive,
            ExpiryDate = v.ExpiryDate
        })];
    }

    private async Task<List<AdminOption>> CategoryOptionsAsync(string culture, bool leavesOnly, CancellationToken ct)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.ParentId,
                c.DisplayOrder,
                Name = c.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? c.Code
            })
            .ToListAsync(ct);

        var roots = categories.Where(c => c.ParentId is null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);
        var options = new List<AdminOption>();

        foreach (var root in roots)
        {
            if (!leavesOnly)
            {
                options.Add(new AdminOption(root.Id.ToString(CultureInfo.InvariantCulture), root.Name));
            }

            foreach (var child in categories.Where(c => c.ParentId == root.Id).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name))
            {
                options.Add(new AdminOption(child.Id.ToString(CultureInfo.InvariantCulture), $"{root.Name} › {child.Name}"));
            }
        }

        return options;
    }

    internal async Task<List<AdminOption>> BrandOptionsAsync(CancellationToken ct) =>
        await _context.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new AdminOption(b.Id.ToString(), b.Name))
            .ToListAsync(ct);
}
