using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.Services.Catalog;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

public interface IAdminCategoryService
{
    /// <summary>Every category, each top level followed by its subcategories.</summary>
    Task<List<CategoryRow>> ListAsync(string culture, CancellationToken ct);

    Task<CategoryFormViewModel> NewAsync(int? parentId, string culture, CancellationToken ct);

    Task<CategoryFormViewModel?> FormAsync(int id, string culture, CancellationToken ct);

    /// <summary>Fills the parts of the form that are shown but not posted back.</summary>
    Task FillAsync(CategoryFormViewModel form, string culture, CancellationToken ct);

    Task<SaveResult> SaveAsync(CategoryFormViewModel form, CancellationToken ct);

    Task<DeleteOutcome> DeleteAsync(int id, CancellationToken ct);
}

/// <summary>
/// Categories, their translations and which attributes apply to them.
///
/// The rules keep the catalogue in the shape the rest of the shop expects:
/// two levels only; products only in subcategories, since a subcategory's
/// attributes drive the variant form and the filters; no hiding a category
/// that still has published products, which would leave them reachable but
/// lost from the menu; and no removing an attribute that variants already
/// have values for. A renamed slug keeps answering with a redirect.
/// </summary>
public class AdminCategoryService : IAdminCategoryService
{
    private static readonly string[] Cultures = [CultureConstants.Serbian, CultureConstants.English];

    private readonly ApplicationDbContext _context;
    private readonly LinkGenerator _links;

    public AdminCategoryService(ApplicationDbContext context, LinkGenerator links)
    {
        _context = context;
        _links = links;
    }

    public async Task<List<CategoryRow>> ListAsync(string culture, CancellationToken ct)
    {
        var rows = await _context.Categories
            .AsNoTracking()
            .Select(c => new CategoryRow
            {
                Id = c.Id,
                ParentId = c.ParentId,
                Code = c.Code,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                Name = c.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? c.Code,
                SlugSr = c.Translations.Where(t => t.Culture == CultureConstants.Serbian).Select(t => t.Slug).FirstOrDefault(),
                SlugEn = c.Translations.Where(t => t.Culture == CultureConstants.English).Select(t => t.Slug).FirstOrDefault(),
                Products = c.Products.Count(),
                Children = c.Children.Count()
            })
            .ToListAsync(ct);

        var ordered = new List<CategoryRow>();

        foreach (var top in rows.Where(r => r.ParentId is null).OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name))
        {
            ordered.Add(top);
            ordered.AddRange(rows.Where(r => r.ParentId == top.Id).OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name));
        }

        return ordered;
    }

    public async Task<CategoryFormViewModel> NewAsync(int? parentId, string culture, CancellationToken ct)
    {
        var form = new CategoryFormViewModel { ParentId = parentId };

        // New subcategories go to the end of their group.
        form.DisplayOrder = await _context.Categories
            .Where(c => c.ParentId == parentId)
            .Select(c => (int?)c.DisplayOrder)
            .MaxAsync(ct) is { } last ? last + 1 : 0;

        await FillAsync(form, culture, ct);
        return form;
    }

    public async Task<CategoryFormViewModel?> FormAsync(int id, string culture, CancellationToken ct)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Translations)
            .Include(c => c.CategoryAttributes)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
        {
            return null;
        }

        var form = new CategoryFormViewModel
        {
            Id = category.Id,
            Code = category.Code,
            ParentId = category.ParentId,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            Sr = ToForm(category.Translations.FirstOrDefault(t => t.Culture == CultureConstants.Serbian)),
            En = ToForm(category.Translations.FirstOrDefault(t => t.Culture == CultureConstants.English)),
            Attributes = [.. category.CategoryAttributes.Select(a => new CategoryAttributeField
            {
                AttributeDefinitionId = a.AttributeDefinitionId,
                Assigned = true,
                IsRequired = a.IsRequired,
                DisplayOrder = a.DisplayOrder
            })]
        };

        await FillAsync(form, culture, ct);
        return form;
    }

    public async Task FillAsync(CategoryFormViewModel form, string culture, CancellationToken ct)
    {
        if (form.Id > 0)
        {
            var stored = await _context.Categories
                .AsNoTracking()
                .Where(c => c.Id == form.Id)
                .Select(c => new
                {
                    c.Code,
                    c.IsActive,
                    Slug = c.Translations.Where(t => t.Culture == CultureConstants.Serbian).Select(t => t.Slug).FirstOrDefault(),
                    HasChildren = c.Children.Any(),
                    Products = c.Products.Count()
                })
                .FirstOrDefaultAsync(ct);

            if (stored is not null)
            {
                // The code cannot change once the category exists.
                form.Code = stored.Code;
                form.HasChildren = stored.HasChildren;
                form.ProductCount = stored.Products;

                if (stored is { IsActive: true, Slug: not null })
                {
                    form.PublicUrlSr = _links.GetPathByAction("Category", "Products",
                        new { area = "", culture = CultureConstants.Serbian, categorySlug = stored.Slug });
                }
            }
        }

        // A category with subcategories has to stay at the top: no third level.
        var parents = form.HasChildren
            ? []
            : await _context.Categories
                .AsNoTracking()
                .Where(c => c.ParentId == null && c.Id != form.Id)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new
                {
                    c.Id,
                    Name = c.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? c.Code
                })
                .ToListAsync(ct);

        form.Parents = [.. parents.Select(p => new AdminOption(p.Id.ToString(CultureInfo.InvariantCulture), p.Name))];

        var definitions = await _context.AttributeDefinitions
            .AsNoTracking()
            .OrderBy(d => d.DisplayOrder)
            .Select(d => new
            {
                d.Id,
                d.Code,
                d.IsVariantDefining,
                d.DisplayOrder,
                Name = d.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
            })
            .ToListAsync(ct);

        var inUse = await AttributesInUseAsync(form.Id, ct);

        var posted = form.Attributes
            .GroupBy(a => a.AttributeDefinitionId)
            .ToDictionary(g => g.Key, g => g.First());

        form.Attributes = [.. definitions.Select(d =>
        {
            posted.TryGetValue(d.Id, out var mine);

            return new CategoryAttributeField
            {
                AttributeDefinitionId = d.Id,
                Assigned = mine?.Assigned ?? false,
                IsRequired = mine?.IsRequired ?? true,
                DisplayOrder = mine?.DisplayOrder ?? d.DisplayOrder,
                Name = d.Name ?? d.Code,
                Code = d.Code,
                IsVariantDefining = d.IsVariantDefining,
                InUse = inUse.Contains(d.Id)
            };
        })];
    }

    public async Task<SaveResult> SaveAsync(CategoryFormViewModel form, CancellationToken ct)
    {
        var errors = new List<FieldError>();
        Category? category = null;

        if (form.Id > 0)
        {
            category = await _context.Categories
                .AsSplitQuery()
                .Include(c => c.Translations)
                .Include(c => c.CategoryAttributes)
                .FirstOrDefaultAsync(c => c.Id == form.Id, ct);

            if (category is null)
            {
                return SaveResult.Missing();
            }
        }
        else if (await _context.Categories.AnyAsync(c => c.Code == form.Code.Trim(), ct))
        {
            errors.Add(new(nameof(form.Code), "Admin_CodeTaken"));
        }

        var hasChildren = category is not null && await _context.Categories.AnyAsync(c => c.ParentId == category.Id, ct);
        var hasProducts = category is not null && await _context.Products.AnyAsync(p => p.CategoryId == category.Id, ct);

        if (form.ParentId is { } parentId)
        {
            var parent = await _context.Categories
                .AsNoTracking()
                .Where(c => c.Id == parentId)
                .Select(c => new { c.Id, c.ParentId })
                .FirstOrDefaultAsync(ct);

            if (parent is null || parent.ParentId is not null || parent.Id == form.Id)
            {
                errors.Add(new(nameof(form.ParentId), "Admin_CategoryParentInvalid"));
            }
            else if (hasChildren)
            {
                errors.Add(new(nameof(form.ParentId), "Admin_CategoryHasChildren"));
            }
        }
        else if (hasProducts)
        {
            errors.Add(new(nameof(form.ParentId), "Admin_CategoryNeedsParent"));
        }

        if (!form.IsActive && category is not null
            && await _context.Products.AnyAsync(p => p.Status == ProductStatus.Published
                && (p.CategoryId == category.Id || p.Category.ParentId == category.Id), ct))
        {
            errors.Add(new(nameof(form.IsActive), "Admin_CategoryHasPublished"));
        }

        var slugs = new Dictionary<string, string>();

        foreach (var code in Cultures)
        {
            var translation = code == CultureConstants.Serbian ? form.Sr : form.En;
            var field = $"{(code == CultureConstants.Serbian ? nameof(form.Sr) : nameof(form.En))}.{nameof(translation.Slug)}";
            var typed = AdminText.Clean(translation.Slug);
            var slug = typed ?? SlugGenerator.Generate(translation.Name);

            if (string.IsNullOrEmpty(slug))
            {
                errors.Add(new(field, "Validation_Slug"));
                continue;
            }

            // /sr/cart is the cart, not a category called "cart".
            if (CatalogSlugConstraint.IsReserved(slug) || CultureConstants.IsSupported(slug))
            {
                errors.Add(new(field, "Admin_SlugReserved"));
                continue;
            }

            if (typed is not null)
            {
                if (await SlugTakenAsync(code, slug, form.Id, ct))
                {
                    errors.Add(new(field, "Admin_CategorySlugTaken"));
                }
            }
            else
            {
                slug = await FreeSlugAsync(code, slug, form.Id, ct);
            }

            slugs[code] = slug;
        }

        var inUse = await AttributesInUseAsync(form.Id, ct);
        var known = (await _context.AttributeDefinitions.Select(d => d.Id).ToListAsync(ct)).ToHashSet();

        for (var i = 0; i < form.Attributes.Count; i++)
        {
            var row = form.Attributes[i];

            var removing = !row.Assigned
                && category is not null
                && category.CategoryAttributes.Any(a => a.AttributeDefinitionId == row.AttributeDefinitionId);

            if (removing && inUse.Contains(row.AttributeDefinitionId))
            {
                errors.Add(new($"{nameof(form.Attributes)}[{i}].{nameof(row.Assigned)}", "Admin_AttributeInUse"));
            }
        }

        if (errors.Count > 0)
        {
            return SaveResult.Fail(errors);
        }

        if (category is null)
        {
            category = new Category { Code = form.Code.Trim() };
            _context.Categories.Add(category);
        }

        category.ParentId = form.ParentId;
        category.DisplayOrder = form.DisplayOrder;
        category.IsActive = form.IsActive;

        foreach (var code in Cultures)
        {
            var posted = code == CultureConstants.Serbian ? form.Sr : form.En;
            var translation = category.Translations.FirstOrDefault(t => t.Culture == code);

            await SlugHistory.RecordAsync(_context, SlugOwner.Category, category.Id, code, translation?.Slug, slugs[code], ct);

            if (translation is null)
            {
                translation = new CategoryTranslation { Culture = code };
                category.Translations.Add(translation);
            }

            translation.Name = posted.Name.Trim();
            translation.Slug = slugs[code];
            translation.Description = AdminText.Clean(posted.Description);
            translation.MetaTitle = AdminText.Clean(posted.MetaTitle);
            translation.MetaDescription = AdminText.Clean(posted.MetaDescription);
        }

        var wanted = form.Attributes
            .Where(a => a.Assigned && known.Contains(a.AttributeDefinitionId))
            .GroupBy(a => a.AttributeDefinitionId)
            .Select(g => g.First())
            .ToList();

        foreach (var existing in category.CategoryAttributes.ToList())
        {
            if (wanted.All(w => w.AttributeDefinitionId != existing.AttributeDefinitionId))
            {
                _context.CategoryAttributes.Remove(existing);
            }
        }

        foreach (var row in wanted)
        {
            var existing = category.CategoryAttributes.FirstOrDefault(a => a.AttributeDefinitionId == row.AttributeDefinitionId);

            if (existing is null)
            {
                category.CategoryAttributes.Add(new CategoryAttribute
                {
                    AttributeDefinitionId = row.AttributeDefinitionId,
                    IsRequired = row.IsRequired,
                    DisplayOrder = row.DisplayOrder
                });
            }
            else
            {
                existing.IsRequired = row.IsRequired;
                existing.DisplayOrder = row.DisplayOrder;
            }
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A unique index caught a slug or code saved from another tab a moment ago.
            return SaveResult.Fail([new(string.Empty, "Admin_SaveConflict")]);
        }

        return SaveResult.Ok(category.Id);
    }

    public async Task<DeleteOutcome> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
        {
            return DeleteOutcome.NotFound;
        }

        // Coupons have no foreign key to categories, so check them by hand.
        if (await _context.Products.AnyAsync(p => p.CategoryId == id, ct)
            || await _context.Categories.AnyAsync(c => c.ParentId == id, ct)
            || await _context.Coupons.AnyAsync(c => c.RestrictedToCategoryId == id, ct))
        {
            return DeleteOutcome.InUse;
        }

        var redirects = await _context.SlugRedirects
            .Where(r => r.OwnerType == SlugOwner.Category && r.OwnerId == id)
            .ToListAsync(ct);

        _context.SlugRedirects.RemoveRange(redirects);

        // Translations and attribute links go with it (ON DELETE CASCADE).
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(ct);

        return DeleteOutcome.Deleted;
    }

    // ------------------------------------------------------------- helpers

    private async Task<HashSet<int>> AttributesInUseAsync(int categoryId, CancellationToken ct) =>
        categoryId == 0
            ? []
            : (await _context.VariantAttributeValues
                .AsNoTracking()
                .Where(v => v.ProductVariant.Product.CategoryId == categoryId)
                .Select(v => v.AttributeDefinitionId)
                .Distinct()
                .ToListAsync(ct)).ToHashSet();

    private static CategoryTranslationForm ToForm(CategoryTranslation? t) => t is null
        ? new CategoryTranslationForm()
        : new CategoryTranslationForm
        {
            Name = t.Name,
            Slug = t.Slug,
            Description = t.Description,
            MetaTitle = t.MetaTitle,
            MetaDescription = t.MetaDescription
        };

    private Task<bool> SlugTakenAsync(string culture, string slug, int categoryId, CancellationToken ct) =>
        _context.CategoryTranslations.AnyAsync(t => t.Culture == culture && t.Slug == slug && t.CategoryId != categoryId, ct);

    private async Task<string> FreeSlugAsync(string culture, string slug, int categoryId, CancellationToken ct)
    {
        var candidate = slug;

        for (var n = 2; await SlugTakenAsync(culture, candidate, categoryId, ct); n++)
        {
            candidate = $"{slug}-{n.ToString(CultureInfo.InvariantCulture)}";
        }

        return candidate;
    }
}
