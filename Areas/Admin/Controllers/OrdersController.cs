using Microsoft.AspNetCore.Mvc;
using WebShop.Models.Orders;
using WebShop.Services.Admin;
using WebShop.ViewModels.Admin;

namespace WebShop.Areas.Admin.Controllers;

public class OrdersController : AdminControllerBase
{
    private readonly IAdminOrderService _orders;

    public OrdersController(IAdminOrderService orders) => _orders = orders;

    public async Task<IActionResult> Index([FromQuery] AdminOrderQuery query, CancellationToken ct) =>
        View(await _orders.ListAsync(query, ct));

    /// <summary>/admin/orders/details/2026-000148</summary>
    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var model = await _orders.DetailsAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Status(string id, StatusChangeForm change, CancellationToken ct)
    {
        switch (await _orders.ChangeStatusAsync(id, change, ct))
        {
            case StatusChangeOutcome.NotFound:
                return NotFound();
            case StatusChangeOutcome.Changed:
                Flash("Admin_StatusChanged", Text("OrderStatus_" + change.Target));
                break;
            case StatusChangeOutcome.TrackingRequired:
                FlashProblem("Admin_TrackingRequired");
                break;
            case StatusChangeOutcome.Conflict:
                FlashProblem("Admin_StatusConflict");
                break;
            default:
                FlashProblem("Admin_StatusNotAllowed");
                break;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Payment(string id, PaymentStatus status, CancellationToken ct)
    {
        if (!await _orders.SetPaymentStatusAsync(id, status, ct))
        {
            return NotFound();
        }

        Flash("Admin_Saved");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Note(string id, string? note, CancellationToken ct)
    {
        if (!await _orders.SaveNoteAsync(id, note, ct))
        {
            return NotFound();
        }

        Flash("Admin_Saved");
        return RedirectToAction(nameof(Details), new { id });
    }
}
