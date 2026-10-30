using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebShop.Infrastructure;
using WebShop.Models.Identity;
using WebShop.Services.Orders;
using WebShop.ViewModels.Account;

namespace WebShop.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;
    private readonly UserManager<ApplicationUser> _users;

    public OrdersController(IOrderService orders, UserManager<ApplicationUser> users)
    {
        _orders = orders;
        _users = users;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        return View(await _orders.ListForUserAsync(userId, ct));
    }

    /// <summary>/sr/orders/details/2026-000148 — only ever the signed-in customer's own order.</summary>
    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var order = await _orders.FindOwnedAsync(id, userId, ct);

        return order is null ? NotFound() : View(order);
    }

    /// <summary>For guests, who have no account to look in.</summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Track() => View(new TrackOrderViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimits.Sensitive)]
    public async Task<IActionResult> Track(TrackOrderViewModel model, CancellationToken ct)
    {
        model.Searched = true;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Order = await _orders.FindForGuestAsync(model.OrderNumber, model.Email, ct);
        return View(model);
    }
}
