namespace WebShop.Models.Catalog;

public enum SlugOwner
{
    Category = 1,
    Product = 2
}

/// <summary>
/// A slug a category or product used to have. When the admin renames one,
/// the old address is kept here so links in search results, shared posts and
/// bookmarks answer with a permanent redirect instead of a 404.
///
/// No foreign key: the owner is either table. A current slug always wins over
/// a row here, so a new product that takes an old slug simply gets it.
/// </summary>
public class SlugRedirect
{
    public int Id { get; set; }

    public SlugOwner OwnerType { get; set; }

    public int OwnerId { get; set; }

    /// <summary>Short route code: "sr" or "en".</summary>
    public string Culture { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
