using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Catalog;
using WebShop.Models.Community;
using WebShop.Models.Orders;
using WebShop.ViewModels.Reviews;

namespace WebShop.Services.Reviews;

public enum ReviewSubmitResult
{
    Submitted = 0,
    AlreadyReviewed = 1,
    ProductNotFound = 2
}

public interface IReviewService
{
    Task<ReviewSummaryViewModel> SummaryAsync(int productId, string? userId, CancellationToken ct);

    Task<ReviewSubmitResult> SubmitAsync(int productId, string userId, ReviewFormViewModel form, CancellationToken ct);

    Task<List<PendingReviewViewModel>> PendingAsync(string culture, CancellationToken ct);

    Task<bool> ApproveAsync(int reviewId, CancellationToken ct);

    Task<bool> RejectAsync(int reviewId, CancellationToken ct);
}

/// <summary>
/// Reviews are held until someone approves them. Publishing straight away
/// turns every product page into a target for spam and for competitors, and
/// under Serbian consumer law a shop is answerable for what it publishes.
/// </summary>
public class ReviewService : IReviewService
{
    private const int ShownOnPage = 10;

    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context) => _context = context;

    public async Task<ReviewSummaryViewModel> SummaryAsync(
        int productId,
        string? userId,
        CancellationToken ct)
    {
        var summary = new ReviewSummaryViewModel
        {
            ProductId = productId,
            IsSignedIn = userId is not null
        };

        var ratings = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.IsApproved)
            .Select(r => r.Rating)
            .ToListAsync(ct);

        summary.Count = ratings.Count;

        if (ratings.Count > 0)
        {
            summary.Average = Math.Round((decimal)ratings.Average(), 1);

            foreach (var rating in ratings.Where(r => r is >= 1 and <= 5))
            {
                summary.Distribution[rating - 1]++;
            }
        }

        var rows = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.IsApproved)
            .OrderByDescending(r => r.IsVerifiedPurchase)
            .ThenByDescending(r => r.ApprovedUtc ?? r.CreatedUtc)
            .Take(ShownOnPage)
            .Select(r => new
            {
                r.Rating,
                r.Title,
                r.Body,
                r.IsVerifiedPurchase,
                r.CreatedUtc,
                r.User.FirstName,
                r.User.LastName
            })
            .ToListAsync(ct);

        // Composed here rather than in SQL: first name and an initial only.
        // The email and full surname are personal data nobody agreed to publish.
        summary.Reviews = [.. rows.Select(r => new ReviewViewModel
        {
            Rating = r.Rating,
            Title = r.Title,
            Body = r.Body,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            CreatedUtc = r.CreatedUtc,
            AuthorName = string.IsNullOrWhiteSpace(r.LastName)
                ? (r.FirstName ?? string.Empty).Trim()
                : $"{(r.FirstName ?? string.Empty).Trim()} {r.LastName.Trim()[0]}."
        })];

        if (userId is not null)
        {
            var own = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.ProductId == productId && r.UserId == userId)
                .Select(r => (bool?)r.IsApproved)
                .FirstOrDefaultAsync(ct);

            summary.OwnState = own switch
            {
                true => OwnReviewState.Published,
                false => OwnReviewState.Pending,
                null => OwnReviewState.None
            };
        }

        return summary;
    }

    public async Task<ReviewSubmitResult> SubmitAsync(
        int productId,
        string userId,
        ReviewFormViewModel form,
        CancellationToken ct)
    {
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == productId && p.Status == ProductStatus.Published, ct);

        if (!productExists)
        {
            return ReviewSubmitResult.ProductNotFound;
        }

        if (await _context.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId, ct))
        {
            return ReviewSubmitResult.AlreadyReviewed;
        }

        // Verified means this customer has a delivered order containing any
        // variant of this product. Delivered, not merely placed: a review of
        // something still in transit is a review of the packaging.
        var verified = await _context.Orders
            .AnyAsync(o => o.UserId == userId
                && o.Status == OrderStatus.Delivered
                && o.Items.Any(i => i.ProductVariant != null && i.ProductVariant.ProductId == productId), ct);

        _context.Reviews.Add(new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = Math.Clamp(form.Rating, 1, 5),
            Title = string.IsNullOrWhiteSpace(form.Title) ? null : form.Title.Trim(),
            Body = string.IsNullOrWhiteSpace(form.Body) ? null : form.Body.Trim(),
            IsApproved = false,
            IsVerifiedPurchase = verified,
            CreatedUtc = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The unique index on (ProductId, UserId) caught a double submit
            // that slipped past the check above.
            return ReviewSubmitResult.AlreadyReviewed;
        }

        return ReviewSubmitResult.Submitted;
    }

    public async Task<List<PendingReviewViewModel>> PendingAsync(string culture, CancellationToken ct) =>
        await _context.Reviews
            .AsNoTracking()
            .Where(r => !r.IsApproved)
            .OrderBy(r => r.CreatedUtc)
            .Select(r => new PendingReviewViewModel
            {
                Id = r.Id,
                ProductName = r.Product.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? r.Product.Sku,
                AuthorEmail = r.User.Email ?? string.Empty,
                Rating = r.Rating,
                Title = r.Title,
                Body = r.Body,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CreatedUtc = r.CreatedUtc
            })
            .ToListAsync(ct);

    public async Task<bool> ApproveAsync(int reviewId, CancellationToken ct)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        if (review is null)
        {
            return false;
        }

        review.IsApproved = true;
        review.ApprovedUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Rejected reviews are deleted, which also lets the customer write a new one.</summary>
    public async Task<bool> RejectAsync(int reviewId, CancellationToken ct)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        if (review is null)
        {
            return false;
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
