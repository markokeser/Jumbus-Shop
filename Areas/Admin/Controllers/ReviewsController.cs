using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Reviews;

namespace WebShop.Areas.Admin.Controllers;

/// <summary>
/// The moderation queue. The rest of the admin panel is phase 5; this part
/// comes early because reviews are held until approved, so without it no
/// review would ever appear.
/// </summary>
public class ReviewsController : AdminControllerBase
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews) => _reviews = reviews;

    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await _reviews.PendingAsync(Culture, ct));

    [HttpPost]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        if (await _reviews.ApproveAsync(id, ct))
        {
            Flash("Admin_ReviewApproved");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        if (await _reviews.RejectAsync(id, ct))
        {
            Flash("Admin_ReviewRejected");
        }

        return RedirectToAction(nameof(Index));
    }
}
