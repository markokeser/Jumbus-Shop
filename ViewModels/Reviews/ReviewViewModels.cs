using System.ComponentModel.DataAnnotations;

namespace WebShop.ViewModels.Reviews;

public enum OwnReviewState
{
    None = 0,
    Pending = 1,
    Published = 2
}

public class ReviewViewModel
{
    public int Rating { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }

    /// <summary>"Marko P." — never the full surname or the email.</summary>
    public string AuthorName { get; set; } = string.Empty;

    public bool IsVerifiedPurchase { get; set; }

    public DateTime CreatedUtc { get; set; }
}

public class ReviewSummaryViewModel
{
    public int ProductId { get; set; }

    public int Count { get; set; }

    public decimal Average { get; set; }

    /// <summary>Index 0 holds the one-star count, index 4 the five-star count.</summary>
    public int[] Distribution { get; set; } = new int[5];

    public List<ReviewViewModel> Reviews { get; set; } = [];

    public bool IsSignedIn { get; set; }

    public OwnReviewState OwnState { get; set; }

    public bool CanReview => IsSignedIn && OwnState == OwnReviewState.None;

    public int Percent(int stars) =>
        Count == 0 ? 0 : (int)Math.Round(Distribution[stars - 1] * 100.0 / Count);
}

public class ReviewFormViewModel
{
    [Range(1, 5, ErrorMessage = "Validation_Rating")]
    public int Rating { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Body { get; set; }
}

public class PendingReviewViewModel
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string AuthorEmail { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }

    public bool IsVerifiedPurchase { get; set; }

    public DateTime CreatedUtc { get; set; }
}
