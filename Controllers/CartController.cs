using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using WebShop.Infrastructure;
using WebShop.Services.Cart;

namespace WebShop.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await _cart.GetAsync(Culture, ct));

    /// <summary>
    /// Every cart change is a POST that redirects, so a refresh cannot add the
    /// same item twice and the back button never replays an action.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        int variantId,
        int quantity,
        string? returnUrl,
        CancellationToken ct)
    {
        var result = await _cart.AddAsync(variantId, quantity, ct);

        if (result is AddToCartResult.NotFound or AddToCartResult.OutOfStock)
        {
            return SafeRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int variantId, int quantity, CancellationToken ct)
    {
        await _cart.SetQuantityAsync(variantId, quantity, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int variantId)
    {
        _cart.Remove(variantId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyCoupon(string? code)
    {
        _cart.CouponCode = code;
        return RedirectToAction(nameof(Index));
    }

    private string Culture => CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);

    /// <summary>
    /// Only ever returns to a path on this site. An open redirect here would
    /// let a crafted "add to cart" link bounce the shopper somewhere else.
    /// </summary>
    private IActionResult SafeRedirect(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
}
