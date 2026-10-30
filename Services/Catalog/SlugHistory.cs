using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Catalog;

namespace WebShop.Services.Catalog;

/// <summary>
/// Keeps old slugs pointing at their category or product. Called by the
/// admin services before they save; the rows go out in the same SaveChanges.
/// </summary>
public static class SlugHistory
{
    public static async Task RecordAsync(
        ApplicationDbContext context,
        SlugOwner owner,
        int ownerId,
        string culture,
        string? oldSlug,
        string newSlug,
        CancellationToken ct)
    {
        // The new slug is a live address again, so nothing may send it elsewhere.
        var revived = await context.SlugRedirects
            .Where(r => r.OwnerType == owner && r.Culture == culture && r.Slug == newSlug)
            .ToListAsync(ct);

        context.SlugRedirects.RemoveRange(revived);

        if (ownerId == 0 || string.IsNullOrEmpty(oldSlug) || string.Equals(oldSlug, newSlug, StringComparison.Ordinal))
        {
            return;
        }

        var existing = await context.SlugRedirects
            .FirstOrDefaultAsync(r => r.OwnerType == owner && r.Culture == culture && r.Slug == oldSlug, ct);

        if (existing is null)
        {
            context.SlugRedirects.Add(new SlugRedirect
            {
                OwnerType = owner,
                OwnerId = ownerId,
                Culture = culture,
                Slug = oldSlug
            });
        }
        else
        {
            existing.OwnerId = ownerId;
            existing.CreatedUtc = DateTime.UtcNow;
        }
    }
}
