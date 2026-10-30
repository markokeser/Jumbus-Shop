using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;

namespace WebShop.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly IAdminDashboardService _dashboard;

    public DashboardController(IAdminDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _dashboard.GetAsync(Culture, ct));
}
