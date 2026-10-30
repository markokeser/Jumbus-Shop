using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;

namespace WebShop.Infrastructure;

/// <summary>
/// Reads decimals the same way in both languages.
///
/// MVC's own binder parses with the request culture. In Serbian the comma is
/// the decimal mark and the dot groups thousands, so "6990.50" — exactly what
/// a browser's number field sends — came out as 699050. A price typed in the
/// admin panel could be off by a factor of a hundred without any error.
///
/// Accepted: 6990, 6990.5, 6990,5, 6.990,50, 6,990.50, "6 990". When both
/// marks appear, the last one is the decimal mark.
/// </summary>
public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var name = bindingContext.ModelName;
        var result = bindingContext.ValueProvider.GetValue(name);

        if (result == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(name, result);
        var raw = result.FirstValue;

        if (string.IsNullOrWhiteSpace(raw))
        {
            // Empty is "no value": null for decimal?; for a plain decimal nothing
            // is bound and the property's [Range] or [Required] decides.
            if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        if (TryParse(raw, out var value))
        {
            bindingContext.Result = ModelBindingResult.Success(value);
            return Task.CompletedTask;
        }

        var localizer = bindingContext.HttpContext.RequestServices.GetService<IStringLocalizer<SharedResource>>();
        bindingContext.ModelState.TryAddModelError(name, localizer?["Validation_Number"].Value ?? "Enter a number.");
        return Task.CompletedTask;
    }

    public static bool TryParse(string raw, out decimal value)
    {
        var text = new string([.. raw.Where(c => !char.IsWhiteSpace(c))]);

        var lastDot = text.LastIndexOf('.');
        var lastComma = text.LastIndexOf(',');

        if (lastDot >= 0 && lastComma >= 0)
        {
            var decimalMark = lastDot > lastComma ? '.' : ',';
            var groupMark = decimalMark == '.' ? ',' : '.';
            text = text.Replace(groupMark.ToString(), string.Empty).Replace(',', '.');
        }
        else if (lastComma >= 0)
        {
            // One comma is a decimal mark (Serbian); several can only be groups.
            text = text.Count(c => c == ',') == 1 ? text.Replace(',', '.') : text.Replace(",", string.Empty);
        }
        else if (lastDot >= 0 && text.Count(c => c == '.') > 1)
        {
            text = text.Replace(".", string.Empty);
        }

        return decimal.TryParse(
            text,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
    }
}

public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type == typeof(decimal) ? new FlexibleDecimalModelBinder() : null;
    }
}
