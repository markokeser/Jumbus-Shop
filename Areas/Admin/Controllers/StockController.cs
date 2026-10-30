using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;
using WebShop.ViewModels.Admin;

namespace WebShop.Areas.Admin.Controllers;

public class StockController : AdminControllerBase
{
    private readonly IAdminStockService _stock;

    public StockController(IAdminStockService stock) => _stock = stock;

    public async Task<IActionResult> Index([FromQuery] AdminStockQuery query, CancellationToken ct) =>
        View(await _stock.ListAsync(query, Culture, ct));

    [HttpPost]
    public async Task<IActionResult> Set(int variantId, int original, int quantity, string? returnUrl, CancellationToken ct)
    {
        var result = await _stock.SetAsync(variantId, original, quantity, ct);

        switch (result.Outcome)
        {
            case StockSetOutcome.Updated:
                Flash("Admin_StockSaved");
                break;
            case StockSetOutcome.Conflict:
                FlashProblem("Admin_StockConflict", result.Current);
                break;
            case StockSetOutcome.Invalid:
                FlashProblem("Validation_Stock");
                break;
            default:
                return NotFound();
        }

        return BackTo(returnUrl, nameof(Index));
    }
}
