using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

/// <summary>Brands and attribute options: the lists products are described with.</summary>
public interface IAdminSetupService
{
    Task<List<BrandRow>> BrandsAsync(CancellationToken ct);

    Task<BrandFormViewModel?> BrandFormAsync(int id, CancellationToken ct);

    Task<SaveResult> SaveBrandAsync(BrandFormViewModel form, CancellationToken ct);

    Task<DeleteOutcome> DeleteBrandAsync(int id, CancellationToken ct);

    Task<List<AttributeRow>> AttributesAsync(string culture, CancellationToken ct);

    Task<AttributeOptionsViewModel?> OptionsAsync(int attributeId, string culture, CancellationToken ct);

    Task<AttributeOptionFormViewModel?> NewOptionFormAsync(int attributeId, string culture, CancellationToken ct);

    Task<AttributeOptionFormViewModel?> OptionFormAsync(int optionId, string culture, CancellationToken ct);

    /// <summary>Refills the attribute's name after a failed post; false if the attribute is gone.</summary>
    Task<bool> FillOptionAsync(AttributeOptionFormViewModel form, string culture, CancellationToken ct);

    Task<SaveResult> SaveOptionAsync(AttributeOptionFormViewModel form, CancellationToken ct);

    Task<DeleteOutcome> DeleteOptionAsync(int optionId, CancellationToken ct);
}

public class AdminSetupService : IAdminSetupService
{
    private readonly ApplicationDbContext _context;

    public AdminSetupService(ApplicationDbContext context) => _context = context;

    // -------------------------------------------------------------- brands

    public async Task<List<BrandRow>> BrandsAsync(CancellationToken ct) =>
        await _context.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BrandRow
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                IsActive = b.IsActive,
                Products = b.Products.Count()
            })
            .ToListAsync(ct);

    public async Task<BrandFormViewModel?> BrandFormAsync(int id, CancellationToken ct) =>
        await _context.Brands
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BrandFormViewModel { Id = b.Id, Name = b.Name, Slug = b.Slug, IsActive = b.IsActive })
            .FirstOrDefaultAsync(ct);

    public async Task<SaveResult> SaveBrandAsync(BrandFormViewModel form, CancellationToken ct)
    {
        var name = form.Name.Trim();
        var typed = AdminText.Clean(form.Slug);
        var slug = typed ?? SlugGenerator.Generate(name);
        var errors = new List<FieldError>();

        if (string.IsNullOrEmpty(slug))
        {
            errors.Add(new(nameof(form.Slug), "Validation_Slug"));
        }
        else if (await _context.Brands.AnyAsync(b => b.Slug == slug && b.Id != form.Id, ct))
        {
            errors.Add(new(typed is null ? nameof(form.Name) : nameof(form.Slug), "Admin_BrandTaken"));
        }

        Brand? brand = null;

        if (form.Id > 0)
        {
            brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == form.Id, ct);

            if (brand is null)
            {
                return SaveResult.Missing();
            }
        }

        if (errors.Count > 0)
        {
            return SaveResult.Fail(errors);
        }

        if (brand is null)
        {
            brand = new Brand();
            _context.Brands.Add(brand);
        }

        brand.Name = name;
        brand.Slug = slug;
        brand.IsActive = form.IsActive;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return SaveResult.Fail([new(nameof(form.Name), "Admin_BrandTaken")]);
        }

        return SaveResult.Ok(brand.Id);
    }

    /// <summary>A brand with products stays; deleting it would quietly strip them of their brand.</summary>
    public async Task<DeleteOutcome> DeleteBrandAsync(int id, CancellationToken ct)
    {
        var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);

        if (brand is null)
        {
            return DeleteOutcome.NotFound;
        }

        if (await _context.Products.AnyAsync(p => p.BrandId == id, ct))
        {
            return DeleteOutcome.InUse;
        }

        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync(ct);
        return DeleteOutcome.Deleted;
    }

    // ---------------------------------------------------------- attributes

    public async Task<List<AttributeRow>> AttributesAsync(string culture, CancellationToken ct)
    {
        var rows = await _context.AttributeDefinitions
            .AsNoTracking()
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new
            {
                a.Id,
                a.Code,
                a.DataType,
                a.Unit,
                a.IsVariantDefining,
                Name = a.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault(),
                Options = a.Options.Count(),
                Categories = a.CategoryAttributes
                    .Select(ca => ca.Category.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? ca.Category.Code)
                    .ToList()
            })
            .ToListAsync(ct);

        return [.. rows.Select(r => new AttributeRow
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name ?? r.Code,
            DataType = r.DataType,
            Unit = r.Unit,
            IsVariantDefining = r.IsVariantDefining,
            Options = r.Options,
            Categories = r.Categories
        })];
    }

    public async Task<AttributeOptionsViewModel?> OptionsAsync(int attributeId, string culture, CancellationToken ct)
    {
        var attribute = (await AttributesAsync(culture, ct)).FirstOrDefault(a => a.Id == attributeId);

        if (attribute is null || attribute.DataType != AttributeDataType.Option)
        {
            return null;
        }

        var options = await _context.AttributeOptions
            .AsNoTracking()
            .Where(o => o.AttributeDefinitionId == attributeId)
            .OrderBy(o => o.DisplayOrder)
            .ThenBy(o => o.Id)
            .Select(o => new AttributeOptionRow
            {
                Id = o.Id,
                Code = o.Code,
                SwatchHex = o.SwatchHex,
                DisplayOrder = o.DisplayOrder,
                LabelSr = o.Translations.Where(t => t.Culture == "sr").Select(t => t.Label).FirstOrDefault(),
                LabelEn = o.Translations.Where(t => t.Culture == "en").Select(t => t.Label).FirstOrDefault(),
                Uses = o.VariantValues.Count()
            })
            .ToListAsync(ct);

        return new AttributeOptionsViewModel { Attribute = attribute, Options = options };
    }

    public async Task<AttributeOptionFormViewModel?> NewOptionFormAsync(int attributeId, string culture, CancellationToken ct)
    {
        var form = new AttributeOptionFormViewModel { AttributeDefinitionId = attributeId };

        if (!await FillOptionAsync(form, culture, ct))
        {
            return null;
        }

        form.DisplayOrder = await _context.AttributeOptions
            .Where(o => o.AttributeDefinitionId == attributeId)
            .Select(o => (int?)o.DisplayOrder)
            .MaxAsync(ct) is { } last ? last + 1 : 0;

        return form;
    }

    public async Task<AttributeOptionFormViewModel?> OptionFormAsync(int optionId, string culture, CancellationToken ct)
    {
        var option = await _context.AttributeOptions
            .AsNoTracking()
            .Include(o => o.Translations)
            .FirstOrDefaultAsync(o => o.Id == optionId, ct);

        if (option is null)
        {
            return null;
        }

        var form = new AttributeOptionFormViewModel
        {
            Id = option.Id,
            AttributeDefinitionId = option.AttributeDefinitionId,
            Code = option.Code,
            SwatchHex = option.SwatchHex,
            DisplayOrder = option.DisplayOrder,
            LabelSr = option.Translations.FirstOrDefault(t => t.Culture == "sr")?.Label ?? string.Empty,
            LabelEn = option.Translations.FirstOrDefault(t => t.Culture == "en")?.Label ?? string.Empty
        };

        await FillOptionAsync(form, culture, ct);
        return form;
    }

    public async Task<bool> FillOptionAsync(AttributeOptionFormViewModel form, string culture, CancellationToken ct)
    {
        var attribute = await _context.AttributeDefinitions
            .AsNoTracking()
            .Where(a => a.Id == form.AttributeDefinitionId && a.DataType == AttributeDataType.Option)
            .Select(a => new
            {
                a.Code,
                Name = a.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault(),
                HasSwatches = a.Options.Any(o => o.SwatchHex != null)
            })
            .FirstOrDefaultAsync(ct);

        if (attribute is null)
        {
            return false;
        }

        form.AttributeName = attribute.Name ?? attribute.Code;

        // Colours and shades are shown as swatches; sizes and skin types are not.
        form.ShowSwatch = attribute.HasSwatches || form.SwatchHex is not null;
        return true;
    }

    public async Task<SaveResult> SaveOptionAsync(AttributeOptionFormViewModel form, CancellationToken ct)
    {
        if (!await _context.AttributeDefinitions.AnyAsync(a => a.Id == form.AttributeDefinitionId && a.DataType == AttributeDataType.Option, ct))
        {
            return SaveResult.Missing();
        }

        var code = form.Code.Trim();

        if (await _context.AttributeOptions.AnyAsync(o => o.AttributeDefinitionId == form.AttributeDefinitionId && o.Code == code && o.Id != form.Id, ct))
        {
            return SaveResult.Fail([new(nameof(form.Code), "Admin_OptionTaken")]);
        }

        AttributeOption? option;

        if (form.Id > 0)
        {
            option = await _context.AttributeOptions
                .Include(o => o.Translations)
                .FirstOrDefaultAsync(o => o.Id == form.Id && o.AttributeDefinitionId == form.AttributeDefinitionId, ct);

            if (option is null)
            {
                return SaveResult.Missing();
            }
        }
        else
        {
            option = new AttributeOption { AttributeDefinitionId = form.AttributeDefinitionId };
            _context.AttributeOptions.Add(option);
        }

        option.Code = code;
        option.SwatchHex = AdminText.Clean(form.SwatchHex)?.ToUpperInvariant();
        option.DisplayOrder = form.DisplayOrder;

        foreach (var (culture, label) in new[] { ("sr", form.LabelSr), ("en", form.LabelEn) })
        {
            var translation = option.Translations.FirstOrDefault(t => t.Culture == culture);

            if (translation is null)
            {
                translation = new AttributeOptionTranslation { Culture = culture };
                option.Translations.Add(translation);
            }

            translation.Label = label.Trim();
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return SaveResult.Fail([new(nameof(form.Code), "Admin_OptionTaken")]);
        }

        return SaveResult.Ok(option.Id);
    }

    /// <summary>An option in use stays; the database refuses to orphan variants anyway.</summary>
    public async Task<DeleteOutcome> DeleteOptionAsync(int optionId, CancellationToken ct)
    {
        var option = await _context.AttributeOptions.FirstOrDefaultAsync(o => o.Id == optionId, ct);

        if (option is null)
        {
            return DeleteOutcome.NotFound;
        }

        if (await _context.VariantAttributeValues.AnyAsync(v => v.AttributeOptionId == optionId, ct))
        {
            return DeleteOutcome.InUse;
        }

        _context.AttributeOptions.Remove(option);
        await _context.SaveChangesAsync(ct);
        return DeleteOutcome.Deleted;
    }
}
