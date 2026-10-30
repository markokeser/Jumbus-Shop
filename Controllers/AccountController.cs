using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using WebShop.Infrastructure;
using WebShop.Models.Identity;
using WebShop.Services.Customers;
using WebShop.Services.Email;
using WebShop.Services.Orders;
using WebShop.ViewModels.Account;

namespace WebShop.Controllers;

/// <summary>
/// Sign-in, registration and the customer's own account pages.
///
/// These replace the pages that ship with ASP.NET Identity. Those are compiled
/// into the Identity UI package with English text baked in, so they cannot be
/// translated through the shop's resource files — which an earlier note in
/// this project wrongly assumed they could. They also sit outside the
/// language prefix and the shop's design. The Identity services underneath
/// (password hashing, lockout, tokens) are unchanged; only the pages are ours.
/// </summary>
[Authorize]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IOrderService _orders;
    private readonly IAddressService _addresses;
    private readonly IWishlistService _wishlist;
    private readonly IAccountEmailSender _email;
    private readonly IStringLocalizer<SharedResource> _l;
    private readonly IWebHostEnvironment _environment;
    private readonly IAccountDataService _data;

    public AccountController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        IOrderService orders,
        IAddressService addresses,
        IWishlistService wishlist,
        IAccountEmailSender email,
        IStringLocalizer<SharedResource> localizer,
        IWebHostEnvironment environment,
        IAccountDataService data)
    {
        _signIn = signIn;
        _users = users;
        _orders = orders;
        _addresses = addresses;
        _wishlist = wishlist;
        _email = email;
        _l = localizer;
        _environment = environment;
        _data = data;
    }

    // ------------------------------------------------------------ sign in

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return SafeRedirect(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimits.Sensitive)]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Lockout is on: repeated wrong passwords lock the account for a
        // while, which is what stops a password being guessed online.
        var result = await _signIn.PasswordSignInAsync(
            model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return SafeRedirect(model.ReturnUrl);
        }

        // One message for "no such account" and "wrong password", so the form
        // cannot be used to find out who shops here.
        ModelState.AddModelError(string.Empty,
            _l[result.IsLockedOut ? "Account_LockedOut" : "Account_InvalidLogin"].Value);

        return View(model);
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ------------------------------------------------------- registration

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return SafeRedirect(returnUrl);
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimits.Sensitive)]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            AcceptsMarketing = model.AcceptsMarketing,
            PreferredCulture = Culture,
            RegisteredUtc = DateTime.UtcNow
        };

        var result = await _users.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        await _users.AddToRoleAsync(user, ApplicationRoles.Customer);
        await _signIn.SignInAsync(user, isPersistent: true);
        await _email.SendWelcomeAsync(email, user.FirstName, ct);

        // Guest orders placed earlier with this address are deliberately not
        // attached to the new account. The address has not been confirmed, so
        // attaching them would let anyone register with someone else's email
        // and read that person's order history.

        return SafeRedirect(model.ReturnUrl);
    }

    // ----------------------------------------------------- password reset

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimits.Sensitive)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _users.FindByEmailAsync(model.Email.Trim());

        // The page that follows reads the same whether or not an account
        // exists, for the same reason the sign-in error does.
        if (user?.Email is { } email)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = Url.Action(nameof(ResetPassword), "Account", new { code, email }, Request.Scheme)!;

            await _email.SendPasswordResetAsync(email, url, ct);

            // No mail goes out yet, so in Development the link is shown on the
            // next page to keep the flow testable. Never in any other environment.
            if (_environment.IsDevelopment())
            {
                TempData["DevResetLink"] = url;
            }
        }

        return RedirectToAction(nameof(ForgotPasswordSent));
    }

    [AllowAnonymous]
    public IActionResult ForgotPasswordSent() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string? code, string? email)
    {
        if (string.IsNullOrEmpty(code))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Code = code, Email = email ?? string.Empty });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimits.Sensitive)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _users.FindByEmailAsync(model.Email.Trim());

        if (user is null)
        {
            return RedirectToAction(nameof(ResetPasswordDone));
        }

        string token;

        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, _l["Identity_InvalidToken"].Value);
            return View(model);
        }

        var result = await _users.ResetPasswordAsync(user, token, model.Password);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        return RedirectToAction(nameof(ResetPasswordDone));
    }

    [AllowAnonymous]
    public IActionResult ResetPasswordDone() => View();

    // ------------------------------------------------------ account pages

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await _users.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var orders = await _orders.ListForUserAsync(user.Id, ct);

        return View(new AccountOverviewViewModel
        {
            FirstName = user.FirstName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            RecentOrders = [.. orders.Take(3)],
            OrderCount = orders.Count,
            DefaultAddress = await _addresses.DefaultShippingAsync(user.Id, ct),
            WishlistCount = await _wishlist.CountAsync(user.Id, ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            AcceptsMarketing = user.AcceptsMarketing
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _users.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        // The email field is display-only; never trust the posted copy.
        model.Email = user.Email ?? string.Empty;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.AcceptsMarketing = model.AcceptsMarketing;

        var result = await _users.UpdateAsync(user);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        TempData["Saved"] = true;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult Password() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Password(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _users.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        // Re-issue the cookie; otherwise the security stamp change signs the
        // customer out on their next request.
        await _signIn.RefreshSignInAsync(user);

        TempData["Saved"] = true;
        return RedirectToAction(nameof(Password));
    }

    // ------------------------------------------------------------ my data

    /// <summary>Download and delete: the two rights a customer can use without writing to anyone.</summary>
    [HttpGet]
    public IActionResult Data() => View(new DeleteAccountViewModel());

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var user = await _users.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var json = await _data.ExportAsync(user, ct);
        var name = $"jumbus-{DateTime.UtcNow:yyyy-MM-dd}.json";

        return File(json, "application/json", name);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimits.Sensitive)]
    public async Task<IActionResult> Delete(DeleteAccountViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Data), model);
        }

        var user = await _users.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        if (!await _users.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), _l["Account_DeleteWrongPassword"]);
            return View(nameof(Data), model);
        }

        switch (await _data.DeleteAsync(user, ct))
        {
            case AccountDeletion.IsAdmin:
                ModelState.AddModelError(string.Empty, _l["Account_DeleteAdmin"]);
                return View(nameof(Data), model);

            case AccountDeletion.Failed:
                ModelState.AddModelError(string.Empty, _l["Account_DeleteFailed"]);
                return View(nameof(Data), model);
        }

        await _signIn.SignOutAsync();
        HttpContext.Session.Clear();

        return RedirectToAction(nameof(Deleted));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Deleted() => View();

    // ------------------------------------------------------------ helpers

    private string Culture => CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);

    /// <summary>
    /// Identity reports a taken username and a taken email as two errors, and
    /// here those are the same thing. Distinct keeps the page from saying it twice.
    /// </summary>
    private void AddErrors(IdentityResult result)
    {
        foreach (var description in result.Errors.Select(e => e.Description).Distinct())
        {
            ModelState.AddModelError(string.Empty, description);
        }
    }

    /// <summary>Only ever back into this site — an open redirect after sign-in is a phishing tool.</summary>
    private IActionResult SafeRedirect(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
}
