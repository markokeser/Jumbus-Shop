using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;
using WebShop.ViewModels.Admin;

namespace WebShop.Areas.Admin.Controllers;

public class ProductsController : AdminControllerBase
{
    private const long UploadLimit = ProductImageService.MaxBytes * ProductImageService.MaxFilesPerUpload + 1_000_000;

    private readonly IAdminProductService _products;
    private readonly IProductImageService _images;

    public ProductsController(IAdminProductService products, IProductImageService images)
    {
        _products = products;
        _images = images;
    }

    public async Task<IActionResult> Index([FromQuery] AdminProductQuery query, CancellationToken ct) =>
        View(await _products.ListAsync(query, Culture, ct));

    public async Task<IActionResult> Create(CancellationToken ct) =>
        View("Edit", await _products.NewFormAsync(Culture, ct));

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _products.GetFormAsync(id, Culture, ct);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Save(ProductFormViewModel form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _products.SaveAsync(form, Culture, ct);

            if (result.NotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Flash(form.IsNew ? "Admin_ProductCreated" : "Admin_Saved");
                return RedirectToAction(nameof(Edit), new { id = result.Id });
            }

            AddErrors(result.Errors);
        }

        await _products.FillAsync(form, Culture, ct);
        return View("Edit", form);
    }

    [HttpPost]
    [RequestSizeLimit(UploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadLimit)]
    public async Task<IActionResult> Upload(int id, List<IFormFile> files, CancellationToken ct)
    {
        var result = await _images.UploadAsync(id, files, ct);

        if (result.Rejected.Count > 0)
        {
            FlashProblem("Admin_ImagesRejected", result.Saved, string.Join(", ", result.Rejected));
        }
        else
        {
            Flash("Admin_ImagesSaved", result.Saved);
        }

        return RedirectToImages(id);
    }

    [HttpPost]
    public async Task<IActionResult> ImageSave(int id, int imageId, string? altSr, string? altEn, int? optionId, CancellationToken ct)
    {
        if (await _images.UpdateAsync(id, imageId, altSr, altEn, optionId, ct))
        {
            Flash("Admin_Saved");
        }

        return RedirectToImages(id);
    }

    [HttpPost]
    public async Task<IActionResult> ImagePrimary(int id, int imageId, CancellationToken ct)
    {
        await _images.SetPrimaryAsync(id, imageId, ct);
        return RedirectToImages(id);
    }

    [HttpPost]
    public async Task<IActionResult> ImageMove(int id, int imageId, int direction, CancellationToken ct)
    {
        await _images.MoveAsync(id, imageId, direction, ct);
        return RedirectToImages(id);
    }

    [HttpPost]
    public async Task<IActionResult> ImageDelete(int id, int imageId, CancellationToken ct)
    {
        if (await _images.DeleteAsync(id, imageId, ct))
        {
            Flash("Admin_ImageDeleted");
        }

        return RedirectToImages(id);
    }

    private IActionResult RedirectToImages(int id) =>
        Redirect(Url.Action(nameof(Edit), new { id }) + "#images");
}
