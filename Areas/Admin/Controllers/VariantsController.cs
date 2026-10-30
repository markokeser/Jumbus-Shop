using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;
using WebShop.ViewModels.Admin;

namespace WebShop.Areas.Admin.Controllers;

public class VariantsController : AdminControllerBase
{
    private readonly IAdminVariantService _variants;

    public VariantsController(IAdminVariantService variants) => _variants = variants;

    public async Task<IActionResult> Create(int productId, CancellationToken ct)
    {
        var form = await _variants.NewFormAsync(productId, Culture, ct);
        return form is null ? NotFound() : View("Edit", form);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _variants.GetFormAsync(id, Culture, ct);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Save(VariantFormViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return await _variants.FillAsync(form, Culture, ct) ? View("Edit", form) : NotFound();
        }

        var result = await _variants.SaveAsync(form, Culture, ct);

        switch (result.Outcome)
        {
            case VariantSaveOutcome.NotFound:
                return NotFound();

            case VariantSaveOutcome.Saved:
                Flash(form.IsNew ? "Admin_VariantCreated" : "Admin_Saved");
                return Redirect(Url.Action("Edit", "Products", new { id = form.ProductId }) + "#variants");

            case VariantSaveOutcome.StockConflict:
                // Show the real number and take it as the new starting point,
                // so a second save is a deliberate decision, not a race.
                ModelState.AddModelError(nameof(form.StockQuantity), Text("Admin_StockConflict", result.CurrentStock));
                ModelState.Remove(nameof(form.OriginalStock));
                form.OriginalStock = result.CurrentStock;
                break;

            default:
                AddErrors(result.Errors);
                break;
        }

        return View("Edit", form);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int productId, CancellationToken ct)
    {
        switch (await _variants.DeleteAsync(id, ct))
        {
            case DeleteOutcome.Deleted:
                Flash("Admin_VariantDeleted");
                break;
            case DeleteOutcome.InUse:
                FlashProblem("Admin_VariantInUse");
                break;
        }

        return Redirect(Url.Action("Edit", "Products", new { id = productId }) + "#variants");
    }
}
