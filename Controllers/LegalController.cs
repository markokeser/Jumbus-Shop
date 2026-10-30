using Microsoft.AspNetCore.Mvc;
using WebShop.Infrastructure;

namespace WebShop.Controllers;

/// <summary>
/// The information pages a Serbian distance seller has to publish, plus the
/// cookie choice.
///
/// Each page is a pair of views: Terms.sr.cshtml in Serbian and Terms.cshtml
/// in English, picked by view localisation. Long legal prose does not belong
/// in resource strings, and a lawyer can edit one file per language.
///
/// The texts follow the Zakon o zaštiti potrošača (Sl. glasnik RS 88/2021)
/// and the Zakon o zaštiti podataka o ličnosti (87/2018), but they are a
/// starting point: have them checked by a lawyer before launch, and fill in
/// the company details in appsettings.json.
/// </summary>
public class LegalController : Controller
{
    public IActionResult Terms() => View();

    public IActionResult Privacy() => View();

    public IActionResult Cookies() => View(CookieConsent.Read(HttpContext));

    /// <summary>Withdrawal, complaints, out-of-court disputes and the withdrawal form.</summary>
    public IActionResult Returns() => View();

    /// <summary>The official withdrawal form, filled in with the seller's details, ready to print.</summary>
    public IActionResult WithdrawalForm() => View();

    public IActionResult Shipping() => View();

    public IActionResult Contact() => View();

    public IActionResult SizeGuide() => View();

    /// <summary>Answer from the cookie banner or the cookie page. Changing it later is as easy as giving it.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Consent(string? choice, string? returnUrl)
    {
        CookieConsent.Write(HttpContext, choice == CookieConsent.AllValue ? ConsentChoice.All : ConsentChoice.Necessary);

        // A withdrawn consent also takes away what it allowed. Nothing
        // optional is set today; add the names here when something is.
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Cookies));
    }
}
