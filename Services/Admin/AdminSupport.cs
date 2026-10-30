using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Catalog;
using WebShop.Models.Orders;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

/// <summary>A validation problem found by a service: the form field and a resource key.</summary>
public record FieldError(string Field, string MessageKey);

public record SaveResult(int Id, IReadOnlyList<FieldError> Errors, bool NotFound = false)
{
    public bool Succeeded => !NotFound && Errors.Count == 0;

    public static SaveResult Ok(int id) => new(id, []);

    public static SaveResult Fail(IEnumerable<FieldError> errors) => new(0, [.. errors]);

    public static SaveResult Missing() => new(0, [], NotFound: true);
}

public enum DeleteOutcome
{
    Deleted = 0,
    NotFound = 1,

    /// <summary>Still referenced — orders, products, variants — so it was left alone.</summary>
    InUse = 2
}

/// <summary>Where an order can go next. Cancelled and Returned are final.</summary>
public static class OrderWorkflow
{
    public static IReadOnlyList<OrderStatus> NextSteps(OrderStatus status) => status switch
    {
        OrderStatus.New => [OrderStatus.Confirmed, OrderStatus.Cancelled],
        OrderStatus.Confirmed => [OrderStatus.Shipped, OrderStatus.Cancelled],
        OrderStatus.Shipped => [OrderStatus.Delivered, OrderStatus.Returned],
        OrderStatus.Delivered => [OrderStatus.Returned],
        _ => []
    };
}

internal static class AdminText
{
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>"Crna · M", "50 ml" — a variant described by its defining attributes.</summary>
public static class VariantLabels
{
    public static async Task<Dictionary<int, string>> LoadAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<int> variantIds,
        string culture,
        CancellationToken ct)
    {
        if (variantIds.Count == 0)
        {
            return [];
        }

        var ids = variantIds.Distinct().ToList();

        var rows = await context.VariantAttributeValues
            .AsNoTracking()
            .Where(v => ids.Contains(v.ProductVariantId) && v.AttributeDefinition.IsVariantDefining)
            .Select(v => new
            {
                v.ProductVariantId,
                v.AttributeDefinition.DisplayOrder,
                v.AttributeDefinition.Unit,
                v.NumericValue,
                v.TextValue,
                OptionCode = v.AttributeOption != null ? v.AttributeOption.Code : null,
                OptionLabel = v.AttributeOption != null
                    ? v.AttributeOption.Translations.Where(t => t.Culture == culture).Select(t => t.Label).FirstOrDefault()
                    : null
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.ProductVariantId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(" · ", g
                    .OrderBy(r => r.DisplayOrder)
                    .Select(r => r.OptionLabel ?? r.OptionCode
                        ?? (r.NumericValue is { } n
                            ? n.ToString("0.##", CultureInfo.CurrentCulture) + (r.Unit is null ? string.Empty : " " + r.Unit)
                            : r.TextValue ?? string.Empty))));
    }
}

/// <summary>The attributes a category's products are described by, as empty form fields.</summary>
public static class AttributeFields
{
    public static async Task<List<AttributeValueField>> ForCategoryAsync(
        ApplicationDbContext context,
        int categoryId,
        bool variantDefining,
        string culture,
        CancellationToken ct)
    {
        var rows = await context.CategoryAttributes
            .AsNoTracking()
            .Where(ca => ca.CategoryId == categoryId && ca.AttributeDefinition.IsVariantDefining == variantDefining)
            .OrderBy(ca => ca.DisplayOrder)
            .Select(ca => new
            {
                ca.AttributeDefinitionId,
                ca.IsRequired,
                ca.AttributeDefinition.DataType,
                ca.AttributeDefinition.Unit,
                ca.AttributeDefinition.Code,
                Name = ca.AttributeDefinition.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault(),
                Options = ca.AttributeDefinition.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new
                    {
                        o.Id,
                        o.Code,
                        Label = o.Translations.Where(t => t.Culture == culture).Select(t => t.Label).FirstOrDefault()
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        return [.. rows.Select(r => new AttributeValueField
        {
            AttributeDefinitionId = r.AttributeDefinitionId,
            Label = r.Name ?? r.Code,
            DataType = r.DataType,
            Unit = r.Unit,
            IsRequired = r.IsRequired,
            Options = [.. r.Options.Select(o => new AdminOption(o.Id.ToString(CultureInfo.InvariantCulture), o.Label ?? o.Code))]
        })];
    }

    /// <summary>
    /// Copies posted values onto freshly loaded fields, matched by attribute.
    /// Posted labels and options are never trusted — only the values.
    /// </summary>
    public static List<AttributeValueField> Merge(List<AttributeValueField> template, IEnumerable<AttributeValueField> posted)
    {
        var values = posted.GroupBy(p => p.AttributeDefinitionId).ToDictionary(g => g.Key, g => g.First());

        foreach (var field in template)
        {
            if (values.TryGetValue(field.AttributeDefinitionId, out var value))
            {
                field.OptionId = value.OptionId;
                field.NumericValue = value.NumericValue;
                field.TextValue = value.TextValue;
            }
        }

        return template;
    }

    /// <summary>Checks one posted value against its attribute: type, option ownership, required.</summary>
    public static string? Problem(AttributeValueField field)
    {
        switch (field.DataType)
        {
            case AttributeDataType.Option when field.OptionId is { } optionId:
                if (!field.Options.Any(o => o.Value == optionId.ToString(CultureInfo.InvariantCulture)))
                {
                    return "Validation_Required";
                }
                break;

            case AttributeDataType.Numeric when field.NumericValue is { } number:
                if (number <= 0)
                {
                    return "Validation_Number";
                }
                break;
        }

        return field.IsRequired && !HasTypedValue(field) ? "Validation_Required" : null;
    }

    public static bool HasTypedValue(AttributeValueField field) => field.DataType switch
    {
        AttributeDataType.Option => field.OptionId is not null,
        AttributeDataType.Numeric => field.NumericValue is not null,
        _ => !string.IsNullOrWhiteSpace(field.TextValue)
    };

    /// <summary>Writes a field onto a stored value, keeping only the column its type uses.</summary>
    public static void Apply(AttributeValueField field, VariantAttributeValue target)
    {
        target.AttributeOptionId = field.DataType == AttributeDataType.Option ? field.OptionId : null;
        target.NumericValue = field.DataType == AttributeDataType.Numeric ? field.NumericValue : null;
        target.TextValue = field.DataType == AttributeDataType.Text ? AdminText.Clean(field.TextValue) : null;
    }

    /// <summary>A comparable form of a value, for spotting two variants that are the same thing.</summary>
    public static string Signature(int definitionId, int? optionId, decimal? number, string? text) =>
        definitionId.ToString(CultureInfo.InvariantCulture) + "=" +
        (optionId?.ToString(CultureInfo.InvariantCulture)
            ?? number?.ToString("0.####", CultureInfo.InvariantCulture)
            ?? text?.Trim().ToLowerInvariant()
            ?? string.Empty);
}
