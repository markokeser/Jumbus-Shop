using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;
using WebShop.ViewModels.Admin;

namespace WebShop.Areas.Admin.Controllers;

public class CategoriesController : AdminControllerBase
{
    private readonly IAdminCategoryService _categories;

    public CategoriesController(IAdminCategoryService categories) => _categories = categories;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _categories.ListAsync(Culture, ct));

    public async Task<IActionResult> Create(int? parentId, CancellationToken ct) =>
        View("Edit", await _categories.NewAsync(parentId, Culture, ct));

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _categories.FormAsync(id, Culture, ct);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Save(CategoryFormViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await _categories.FillAsync(form, Culture, ct);
            return View("Edit", form);
        }

        var result = await _categories.SaveAsync(form, ct);

        if (result.NotFound)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            await _categories.FillAsync(form, Culture, ct);
            return View("Edit", form);
        }

        Flash("Admin_Saved");
        return RedirectToAction(nameof(Edit), new { id = result.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        switch (await _categories.DeleteAsync(id, ct))
        {
            case DeleteOutcome.Deleted:
                Flash("Admin_Deleted");
                break;
            case DeleteOutcome.InUse:
                FlashProblem("Admin_CategoryInUse");
                break;
        }

        return RedirectToAction(nameof(Index));
    }
}
