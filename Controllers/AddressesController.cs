using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebShop.Models.Identity;
using WebShop.Services.Customers;
using WebShop.ViewModels.Account;

namespace WebShop.Controllers;

[Authorize]
public class AddressesController : Controller
{
    private readonly IAddressService _addresses;
    private readonly UserManager<ApplicationUser> _users;

    public AddressesController(IAddressService addresses, UserManager<ApplicationUser> users)
    {
        _addresses = addresses;
        _users = users;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = _users.GetUserId(User);
        return userId is null ? Challenge() : View(await _addresses.ListAsync(userId, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        // Start from the account's own name; most people ship to themselves.
        var user = await _users.GetUserAsync(User);

        return View("Edit", new AddressFormViewModel
        {
            FirstName = user?.FirstName ?? string.Empty,
            LastName = user?.LastName ?? string.Empty,
            PhoneNumber = user?.PhoneNumber ?? string.Empty
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        var address = await _addresses.FindAsync(id, userId, ct);

        if (address is null)
        {
            return NotFound();
        }

        return View(new AddressFormViewModel
        {
            Id = address.Id,
            FirstName = address.FirstName,
            LastName = address.LastName,
            Street = address.Street,
            StreetNumber = address.StreetNumber,
            City = address.City,
            PostalCode = address.PostalCode,
            PhoneNumber = address.PhoneNumber,
            IsDefault = address.IsDefault
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AddressFormViewModel form, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View("Edit", form);
        }

        return await _addresses.SaveAsync(form, userId, ct)
            ? RedirectToAction(nameof(Index))
            : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        await _addresses.DeleteAsync(id, userId, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeDefault(int id, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        await _addresses.MakeDefaultAsync(id, userId, ct);
        return RedirectToAction(nameof(Index));
    }
}
