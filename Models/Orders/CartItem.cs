namespace WebShop.Models.Orders;

/// <summary>
/// A cart line. Not an entity — the cart lives in session so guests can shop
/// without an account. Only the variant id and quantity are stored; price and
/// stock are read fresh from the database on every request, so a cart left
/// open overnight never checks out at yesterday's price.
/// </summary>
public class CartItem
{
    public int ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;
}
