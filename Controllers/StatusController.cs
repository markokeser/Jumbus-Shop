using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebShop.Controllers;

/// <summary>
/// The page shown for 404 and other error statuses, in the shop's design and
/// language instead of the browser's blank error.
///
/// UseStatusCodePagesWithReExecute in Program.cs runs this for any response
/// that ends with an error status and no body. The status code itself is
/// kept, so a missing page still answers 404 and is not indexed.
/// </summary>
public class StatusController : Controller
{
    // Any method: a POST that ends in a 404 is re-executed as a POST.
    [Route("status/{code:int}")]
    public IActionResult Index(int code)
    {
        if (HttpContext.Features.Get<IStatusCodeReExecuteFeature>() is null)
        {
            // Opened directly rather than re-executed: an unknown page like any other.
            code = StatusCodes.Status404NotFound;
            Response.StatusCode = code;
        }

        return View(code);
    }
}
