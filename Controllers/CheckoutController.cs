using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WebShop.Infrastructure;
using WebShop.Models.Identity;
using WebShop.Services.Cart;
using WebShop.Services.Customers;
using WebShop.Services.Orders;
using WebShop.ViewModels.Checkout;

namespace WebShop.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly IAddressService _addresses;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IStringLocalizer<SharedResource> _l;

    public CheckoutController(
        ICartService cart,
        IOrderService orders,
        IAddressService addresses,
        UserManager<ApplicationUser> users,
        IStringLocalizer<SharedResource> localizer)
    {
        _cart = cart;
        _orders = orders;
        _addresses = addresses;
        _users = users;
        _l = localizer;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await _cart.GetAsync(Culture, ct);

        if (!cart.CanCheckout)
        {
            return RedirectToAction("Index", "Cart");
        }

        var model = new CheckoutPageViewModel { Cart = cart };

        // Pre-fill what the account already knows, so a returning customer is
        // not retyping their own name and address.
        if (User.Identity?.IsAuthenticated == true && await _users.GetUserAsync(User) is { } user)
        {
            model.Form.FirstName = user.FirstName ?? string.Empty;
            model.Form.LastName = user.LastName ?? string.Empty;
            model.Form.Email = user.Email ?? string.Empty;
            model.Form.PhoneNumber = user.PhoneNumber ?? string.Empty;

            if (await _addresses.DefaultShippingAsync(user.Id, ct) is { } address)
            {
                model.Form.FirstName = address.FirstName;
                model.Form.LastName = address.LastName;
                model.Form.PhoneNumber = address.PhoneNumber;
                model.Form.Street = address.Street;
                model.Form.StreetNumber = address.StreetNumber;
                model.Form.City = address.City;
                model.Form.PostalCode = address.PostalCode;
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(CheckoutFormViewModel form, CancellationToken ct)
    {
        // Two things were wrong here before. The errors were keyed by the bare
        // property name, but the page binds this model under "Form", so it
        // looked for "Form.CompanyTaxNumber" and found nothing — the form came
        // back with no explanation. And the text was a resource key: messages
        // added by hand do not pass through data-annotations localisation.
        foreach (var result in form.ValidateConditional())
        {
            var message = _l[result.ErrorMessage ?? "Validation_Required"].Value;

            foreach (var member in result.MemberNames)
            {
                ModelState.AddModelError($"{nameof(CheckoutPageViewModel.Form)}.{member}", message);
            }
        }

        var cart = await _cart.GetAsync(Culture, ct);

        if (!cart.CanCheckout)
        {
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), new CheckoutPageViewModel { Form = form, Cart = cart });
        }

        var userId = User.Identity?.IsAuthenticated == true ? _users.GetUserId(User) : null;
        var placed = await _orders.PlaceAsync(form, Culture, userId, ct);

        if (placed.Succeeded)
        {
            return RedirectToAction(nameof(Confirmation), new { number = placed.Order!.OrderNumber });
        }

        return View(nameof(Index), new CheckoutPageViewModel
        {
            Form = form,
            Cart = await _cart.GetAsync(Culture, ct),
            Problem = placed.Problem.ToString(),
            SoldOutProduct = placed.OffendingProduct
        });
    }

    /// <summary>
    /// Shown to the customer who owns the order, or to the browser session
    /// that placed it. Previously any guest order was shown to anyone who had
    /// the number — and order numbers are sequential.
    /// </summary>
    public async Task<IActionResult> Confirmation(string number, CancellationToken ct)
    {
        var userId = User.Identity?.IsAuthenticated == true ? _users.GetUserId(User) : null;

        var order = userId is not null
            ? await _orders.FindOwnedAsync(number, userId, ct)
            : null;

        order ??= await _orders.FindPlacedThisSessionAsync(number, ct);

        return order is null ? NotFound() : View(order);
    }

    private string Culture => CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);
}
