using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;
using WebShop.ViewModels.Admin;

namespace WebShop.Areas.Admin.Controllers;

/// <summary>
/// The values an attribute can take: a new colour, a new lipstick shade.
/// The attributes themselves (size, colour, shade, volume, skin type) are
/// structural — filters and variant pickers are built on them — so they are
/// fixed here and changed in the database by a developer.
/// </summary>
public class AttributesController : AdminControllerBase
{
    private readonly IAdminSetupService _setup;

    public AttributesController(IAdminSetupService setup) => _setup = setup;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _setup.AttributesAsync(Culture, ct));

    public async Task<IActionResult> Options(int id, CancellationToken ct)
    {
        var model = await _setup.OptionsAsync(id, Culture, ct);
        return model is null ? NotFound() : View(model);
    }

    public async Task<IActionResult> CreateOption(int attributeId, CancellationToken ct)
    {
        var form = await _setup.NewOptionFormAsync(attributeId, Culture, ct);
        return form is null ? NotFound() : View("EditOption", form);
    }

    public async Task<IActionResult> EditOption(int id, CancellationToken ct)
    {
        var form = await _setup.OptionFormAsync(id, Culture, ct);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> SaveOption(AttributeOptionFormViewModel form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _setup.SaveOptionAsync(form, ct);

            if (result.NotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Flash("Admin_Saved");
                return RedirectToAction(nameof(Options), new { id = form.AttributeDefinitionId });
            }

            AddErrors(result.Errors);
        }

        return await _setup.FillOptionAsync(form, Culture, ct) ? View("EditOption", form) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOption(int id, int attributeId, CancellationToken ct)
    {
        switch (await _setup.DeleteOptionAsync(id, ct))
        {
            case DeleteOutcome.Deleted:
                Flash("Admin_Deleted");
                break;
            case DeleteOutcome.InUse:
                FlashProblem("Admin_OptionInUse");
                break;
        }

        return RedirectToAction(nameof(Options), new { id = attributeId });
    }
}
