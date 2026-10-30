using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebShop.Infrastructure;
using WebShop.Models.Identity;
using WebShop.Services.Customers;

namespace WebShop.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlist;
    private readonly UserManager<ApplicationUser> _users;

    public WishlistController(IWishlistService wishlist, UserManager<ApplicationUser> users)
    {
        _wishlist = wishlist;
        _users = users;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var culture = CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);
        return View(await _wishlist.ListAsync(userId, culture, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId, string? returnUrl, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        await _wishlist.ToggleAsync(userId, productId, ct);

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
