using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Admin;

namespace WebShop.ViewComponents;

/// <summary>The admin sidebar, with counts of what is waiting.</summary>
public class AdminNavViewComponent : ViewComponent
{
    private readonly IAdminDashboardService _dashboard;

    public AdminNavViewComponent(IAdminDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await _dashboard.NavCountsAsync(HttpContext.RequestAborted));
}
