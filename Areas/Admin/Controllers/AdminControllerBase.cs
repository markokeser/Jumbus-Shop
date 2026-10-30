using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WebShop.Infrastructure;
using WebShop.Models.Identity;
using WebShop.Services.Admin;

namespace WebShop.Areas.Admin.Controllers;

/// <summary>
/// Every admin page: admins only, and every POST checked for an antiforgery
/// token without having to remember the attribute on each action.
/// </summary>
[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
[AutoValidateAntiforgeryToken]
public abstract class AdminControllerBase : Controller
{
    private IStringLocalizer<SharedResource>? _localizer;

    private IStringLocalizer<SharedResource> L =>
        _localizer ??= HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();

    protected string Culture => CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);

    protected string Text(string key, params object[] args) =>
        args.Length == 0 ? L[key].Value : string.Format(CultureInfo.CurrentCulture, L[key].Value, args);

    /// <summary>A one-line notice shown on the next page.</summary>
    protected void Flash(string key, params object[] args) => TempData["Flash"] = Text(key, args);

    protected void FlashProblem(string key, params object[] args)
    {
        TempData["Flash"] = Text(key, args);
        TempData["FlashBad"] = true;
    }

    protected void AddErrors(IEnumerable<FieldError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.Field, Text(error.MessageKey));
        }
    }

    protected IActionResult BackTo(string? returnUrl, string fallbackAction) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(fallbackAction);
}
