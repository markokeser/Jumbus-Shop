using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;
using WebShop.ViewModels.Admin;

namespace WebShop.Areas.Admin.Controllers;

public class BrandsController : AdminControllerBase
{
    private readonly IAdminSetupService _setup;

    public BrandsController(IAdminSetupService setup) => _setup = setup;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _setup.BrandsAsync(ct));

    public IActionResult Create() => View("Edit", new BrandFormViewModel());

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _setup.BrandFormAsync(id, ct);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Save(BrandFormViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", form);
        }

        var result = await _setup.SaveBrandAsync(form, ct);

        if (result.NotFound)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View("Edit", form);
        }

        Flash("Admin_Saved");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        switch (await _setup.DeleteBrandAsync(id, ct))
        {
            case DeleteOutcome.Deleted:
                Flash("Admin_Deleted");
                break;
            case DeleteOutcome.InUse:
                FlashProblem("Admin_BrandInUse");
                break;
        }

        return RedirectToAction(nameof(Index));
    }
}
