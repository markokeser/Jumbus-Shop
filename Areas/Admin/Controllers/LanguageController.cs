using Microsoft.AspNetCore.Mvc;
using WebShop.Infrastructure;

namespace WebShop.Areas.Admin.Controllers;

/// <summary>The admin has no language in its URLs, so the choice goes in the language cookie.</summary>
public class LanguageController : AdminControllerBase
{
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (CultureConstants.IsSupported(culture))
        {
            CultureCookie.Write(HttpContext, culture);
        }

        return BackTo(returnUrl, "Index");
    }

    public IActionResult Index() => RedirectToAction("Index", "Dashboard");
}
