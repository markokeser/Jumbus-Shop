using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebShop.Models.Identity;
using WebShop.Services.Reviews;
using WebShop.ViewModels.Reviews;

namespace WebShop.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviews;
    private readonly UserManager<ApplicationUser> _users;

    public ReviewsController(IReviewService reviews, UserManager<ApplicationUser> users)
    {
        _reviews = reviews;
        _users = users;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        int productId,
        ReviewFormViewModel form,
        string? returnUrl,
        CancellationToken ct)
    {
        var userId = _users.GetUserId(User);

        if (userId is null)
        {
            return Challenge();
        }

        // The product page reads this back to say what happened. A missing
        // rating is the only realistic invalid case; the rest is optional text.
        TempData["ReviewResult"] = ModelState.IsValid
            ? (await _reviews.SubmitAsync(productId, userId, form, ct)).ToString()
            : "Invalid";

        var target = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return LocalRedirect(target + "#reviews");
    }
}
