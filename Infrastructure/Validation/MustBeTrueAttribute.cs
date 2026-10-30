using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace WebShop.Infrastructure.Validation;

/// <summary>
/// A checkbox that has to be ticked — accepting the terms, typically.
///
/// This replaces [Range(typeof(bool), "true", "true")], which looks right and
/// is broken in the browser: the range adapter writes its bounds as "True",
/// jQuery Validate converts them with Number("True"), gets NaN, and nothing
/// compares greater-or-equal to NaN. A ticked box then fails validation, an
/// unticked one passes it and is refused by the server, and the form can
/// never be submitted. Tested against jquery-validation 1.21.0.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value) => value is true;

    /// <summary>
    /// Emits a rule the browser understands (see wwwroot/js/validation-extras.js).
    /// The message is localised here because, unlike the built-in attributes,
    /// a custom client validator gets no help from MVC's attribute adapters.
    /// </summary>
    public void AddValidation(ClientModelValidationContext context)
    {
        var localizer = context.ActionContext.HttpContext.RequestServices
            .GetService<IStringLocalizer<SharedResource>>();

        var key = ErrorMessage ?? "Validation_Required";
        var message = localizer?[key].Value ?? key;

        context.Attributes.TryAdd("data-val", "true");
        context.Attributes["data-val-mustbetrue"] = message;
    }
}
