using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WebShop;
using WebShop.Data;
using WebShop.Data.Seed;
using WebShop.Infrastructure;
using WebShop.Models.Identity;
using WebShop.Services.Cart;
using WebShop.Services.Admin;
using WebShop.Services.Catalog;
using WebShop.Services.Customers;
using WebShop.Services.Reviews;
using WebShop.Services.Email;
using WebShop.Services.Orders;
using WebShop.Services.Payment;
using WebShop.Services.Seo;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────
builder.Services.Configure<ShopSettings>(
    builder.Configuration.GetSection(ShopSettings.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing from appsettings.json.");

// ServerVersion.AutoDetect opens a connection at startup to read the server
// banner. That means the database has to be reachable before the app boots.
// Swap in an explicit version — new MySqlServerVersion(new Version(8, 4, 0)) —
// if you would rather not pay that round trip or have a fixed server.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ── Identity ─────────────────────────────────────────────────────────────
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;

        // Eight characters with a digit. Demanding upper case and symbols as
        // well mostly produces "Password1!" and abandoned registrations.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = 10;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// The shop's own account pages, not the Identity UI package's. Paths are
// written without a language; the prefix middleware adds the visitor's own.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/accessdenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    // Signing in is something the visitor asked for, so the cookie is
    // essential and is set without waiting for cookie consent.
    options.Cookie.IsEssential = true;
});

// ── Cookie consent ───────────────────────────────────────────────────────
// Cookies not marked essential are dropped until the visitor accepts in the
// banner, which stores "all" in the consent cookie. Everything the shop sets
// today is essential — sign-in, cart, language, form tokens, one-off notices
// — so this is the guard for whatever gets added later.
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = _ => true;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.ConsentCookie.Name = CookieConsent.CookieName;
    options.ConsentCookieValue = CookieConsent.AllValue;
});

// TempData carries the one-line notices after a redirect ("Saved").
builder.Services.Configure<CookieTempDataProviderOptions>(options => options.Cookie.IsEssential = true);

// ── Localisation ─────────────────────────────────────────────────────────
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture =
        new RequestCulture(CultureConstants.RouteToCulture[CultureConstants.Default]);
    options.SupportedCultures = [.. CultureConstants.SupportedCultures];
    options.SupportedUICultures = [.. CultureConstants.SupportedCultures];

    // The language prefix in the URL wins over everything else. Reading the
    // raw path rather than route data means this works no matter where in the
    // pipeline localisation ends up sitting.
    options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
    {
        // An error page re-executed for /en/... runs at /status/404; the
        // address that was asked for still decides its language.
        var path = context.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath
            ?? context.Request.Path.Value;

        var segments = path?
            .Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];

        if (segments.Length > 0
            && CultureConstants.RouteToCulture.TryGetValue(segments[0], out var culture))
        {
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture));
        }

        return Task.FromResult<ProviderCultureResult?>(null);
    }));
});

// ── MVC ──────────────────────────────────────────────────────────────────
builder.Services
    .AddControllersWithViews(options =>
    {
        // Decimals typed in the admin read the same in both languages; the
        // default binder turned "6990.50" into 699050 under Serbian culture.
        options.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
    })
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(SharedResource));
    });

// Identity ships its UI as Razor Pages, so they have to be mapped as well.
builder.Services.AddRazorPages();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
    options.ConstraintMap["culture"] = typeof(CultureRouteConstraint);
    options.ConstraintMap["catalogslug"] = typeof(CatalogSlugConstraint);
});

// ── Catalog ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProductFilterService, ProductFilterService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

// ── Cart, pricing and orders ─────────────────────────────────────────────
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Both providers are registered; OrderService picks the one whose Method
// matches what the shopper chose at checkout.
builder.Services.AddScoped<IPaymentProvider, CashOnDeliveryProvider>();
builder.Services.AddScoped<IPaymentProvider, MockCardProvider>();

builder.Services.AddScoped<IOrderEmailSender, LoggingOrderEmailSender>();

// ── Customer account ─────────────────────────────────────────────────────
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAccountEmailSender, LoggingAccountEmailSender>();

// ── Admin ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IAdminProductService, AdminProductService>();
builder.Services.AddScoped<IAdminVariantService, AdminVariantService>();
builder.Services.AddScoped<IAdminStockService, AdminStockService>();
builder.Services.AddScoped<IAdminCouponService, AdminCouponService>();
builder.Services.AddScoped<IAdminSetupService, AdminSetupService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();

// ── SEO and privacy ──────────────────────────────────────────────────────
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<IAccountDataService, AccountDataService>();

// ── Rate limiting ────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimits.Sensitive, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ── Session, for the guest cart ──────────────────────────────────────────
// In-memory cache is fine on a single machine. Behind a load balancer this
// has to become a distributed cache (Redis or SQL Server), or carts will
// vanish whenever a request lands on a different node.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".WebShop.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(7);
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ── Pipeline ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/sr/home/error");
    app.UseHsts();
}

// A 404 or any other error status without a body gets the shop's own page,
// keeping the status code. Placed before routing so the error page is routed.
app.UseStatusCodePagesWithReExecute("/status/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization();

// Anything that reaches the site without a language prefix gets one.
app.UseMiddleware<CulturePrefixRedirectMiddleware>();

app.UseCookiePolicy();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

// Admin lives outside the language prefix: it is one internal tool used by
// one team, and giving it two URL trees would only double the routing surface.
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}");

// Catalog URLs carry translated slugs: /sr/haljine/koktel-haljina-vera and
// /en/dresses/vera-cocktail-dress.
//
// These are registered BEFORE the default route, and the order is the whole
// point. Matching would work either way, because the catalogslug constraint
// rejects anything that is really a controller name. Link generation would
// not: Url.Action picks the first route able to produce a URL, and the
// default route can produce one for Products/Category — it just looks like
// /sr/products/category?categorySlug=haljine instead of /sr/haljine.
app.MapControllerRoute(
    name: "catalog-product",
    pattern: "{culture:culture}/{categorySlug:catalogslug}/{productSlug}",
    defaults: new { controller = "Products", action = "Details" });

app.MapControllerRoute(
    name: "catalog-category",
    pattern: "{culture:culture}/{categorySlug:catalogslug}",
    defaults: new { controller = "Products", action = "Category" });

app.MapControllerRoute(
    name: "default",
    pattern: "{culture:culture}/{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ── Seeding ──────────────────────────────────────────────────────────────
var shopSettings = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<ShopSettings>>().Value;

if (shopSettings.SeedOnStartup)
{
    var startupLogger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");

    try
    {
        await DatabaseSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        // Almost always one of two things: no migration has been generated
        // yet, or LocalDB is not running. Neither should read as a stack trace.
        startupLogger.LogError(
            ex,
            "Seeding failed. If this is a fresh checkout, run: dotnet ef migrations add InitialCreate");
    }
}

app.Run();
