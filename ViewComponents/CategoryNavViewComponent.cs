using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using WebShop.Infrastructure;
using WebShop.Services.Catalog;

namespace WebShop.ViewComponents;

/// <summary>
/// The category menu in the header (and the mobile menu), or — with
/// variant "pills" — the sibling subcategories shown on a listing page.
/// </summary>
public class CategoryNavViewComponent : ViewComponent
{
    private readonly ICatalogService _catalog;

    public CategoryNavViewComponent(ICatalogService catalog) => _catalog = catalog;

    public async Task<IViewComponentResult> InvokeAsync(string? variant = null, string? current = null)
    {
        var culture = CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);
        var groups = await _catalog.CategoryNavAsync(culture, HttpContext.RequestAborted);

        if (variant != "pills")
        {
            return View(groups);
        }

        var group = groups.FirstOrDefault(g => g.Slug == current || g.Children.Any(c => c.Slug == current));
        return group is null ? Content(string.Empty) : View("Pills", (group, current));
    }
}
