using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;
using WebShop.ViewModels.Admin;

namespace WebShop.Areas.Admin.Controllers;

public class CouponsController : AdminControllerBase
{
    private readonly IAdminCouponService _coupons;

    public CouponsController(IAdminCouponService coupons) => _coupons = coupons;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _coupons.ListAsync(Culture, ct));

    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var form = new CouponFormViewModel();
        await _coupons.FillAsync(form, Culture, ct);
        return View("Edit", form);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _coupons.GetFormAsync(id, Culture, ct);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Save(CouponFormViewModel form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _coupons.SaveAsync(form, ct);

            if (result.NotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Flash("Admin_Saved");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result.Errors);
        }

        await _coupons.FillAsync(form, Culture, ct);
        return View("Edit", form);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        switch (await _coupons.DeleteAsync(id, ct))
        {
            case DeleteOutcome.Deleted:
                Flash("Admin_Deleted");
                break;
            case DeleteOutcome.InUse:
                FlashProblem("Admin_CouponInUse");
                break;
        }

        return RedirectToAction(nameof(Index));
    }
}
