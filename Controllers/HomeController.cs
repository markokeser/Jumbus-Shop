using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.Services.Catalog;
using WebShop.ViewModels.Catalog;
using WebShop.ViewModels;
using WebShop.ViewModels.Home;

namespace WebShop.Controllers;

public class HomeController : Controller
{
    private const string SizeAttribute = "size";
    private const string ShadeAttribute = "shade";


    private readonly ApplicationDbContext _context;
    private readonly ICatalogService _catalog;
    private readonly ShopSettings _settings;

    public HomeController(
        ApplicationDbContext context,
        ICatalogService catalog,
        IOptions<ShopSettings> settings)
    {
        _context = context;
        _catalog = catalog;
        _settings = settings.Value;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var culture = CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);

        var model = new HomeIndexViewModel
        {
            Hero = await BuildHeroAsync(culture, ct),
            Featured = await _catalog.FeaturedAsync(4, culture, ct),
            CategoryGroups = await BuildCategoryGroupsAsync(culture, ct),
            BuildStatus = new BuildStatusViewModel
            {
                Categories = await _context.Categories.CountAsync(ct),
                Products = await _context.Products.CountAsync(ct),
                Variants = await _context.ProductVariants.CountAsync(ct),
                InStock = await _context.ProductVariants
                    .CountAsync(v => v.IsActive && v.StockQuantity > 0, ct)
            }
        };

        return View(model);
    }

    /// <summary>
    /// The hero's right-hand panel shows the real size run and shade range out
    /// of the attribute tables, so the transparency claim in the headline is
    /// backed by data rather than illustrated with decoration.
    /// </summary>
    private async Task<HeroViewModel> BuildHeroAsync(string culture, CancellationToken ct)
    {
        // One round trip for both attributes, split in memory afterwards.
        var options = await _context.AttributeOptions
            .AsNoTracking()
            .Where(o => o.AttributeDefinition.Code == SizeAttribute
                        || o.AttributeDefinition.Code == ShadeAttribute)
            .OrderBy(o => o.DisplayOrder)
            .Select(o => new
            {
                AttributeCode = o.AttributeDefinition.Code,
                AttributeName = o.AttributeDefinition.Translations
                    .Where(t => t.Culture == culture)
                    .Select(t => t.Name)
                    .FirstOrDefault(),
                Label = o.Translations
                    .Where(t => t.Culture == culture)
                    .Select(t => t.Label)
                    .FirstOrDefault() ?? o.Code,
                o.SwatchHex
            })
            .ToListAsync(ct);

        return new HeroViewModel
        {
            VariantsInStock = await _context.ProductVariants
                .CountAsync(v => v.IsActive && v.StockQuantity > 0, ct),
            FreeShippingThreshold = _settings.FreeShippingThreshold,
            ReturnDays = _settings.ReturnDays,

            SizeAttributeName = options
                .FirstOrDefault(o => o.AttributeCode == SizeAttribute)?.AttributeName
                ?? string.Empty,
            ShadeAttributeName = options
                .FirstOrDefault(o => o.AttributeCode == ShadeAttribute)?.AttributeName
                ?? string.Empty,

            SizeLabels = [.. options
                .Where(o => o.AttributeCode == SizeAttribute)
                .Select(o => o.Label)],

            Shades = [.. options
                .Where(o => o.AttributeCode == ShadeAttribute && o.SwatchHex != null)
                .Select(o => new SwatchViewModel { Label = o.Label, Hex = o.SwatchHex! })]
        };
    }

    private async Task<List<CategoryGroupViewModel>> BuildCategoryGroupsAsync(
        string culture,
        CancellationToken ct) =>
        await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.ParentId == null)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryGroupViewModel
            {
                Name = c.Translations
                    .Where(t => t.Culture == culture)
                    .Select(t => t.Name)
                    .FirstOrDefault() ?? c.Code,
                Slug = c.Translations
                    .Where(t => t.Culture == culture)
                    .Select(t => t.Slug)
                    .FirstOrDefault() ?? c.Code,
                Children = c.Children
                    .Where(child => child.IsActive)
                    .OrderBy(child => child.DisplayOrder)
                    .Select(child => new CategoryLinkViewModel
                    {
                        Name = child.Translations
                            .Where(t => t.Culture == culture)
                            .Select(t => t.Name)
                            .FirstOrDefault() ?? child.Code,
                        Slug = child.Translations
                            .Where(t => t.Culture == culture)
                            .Select(t => t.Slug)
                            .FirstOrDefault() ?? child.Code,
                        Description = child.Translations
                            .Where(t => t.Culture == culture)
                            .Select(t => t.Description)
                            .FirstOrDefault(),
                        ProductCount = child.Products.Count(p => p.Status == ProductStatus.Published)
                    })
                    .ToList()
            })
            .ToListAsync(ct);

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
}
