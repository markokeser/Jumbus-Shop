using Microsoft.AspNetCore.Mvc;
using WebShop.Services.Cart;

namespace WebShop.ViewComponents;

/// <summary>
/// The count beside the cart in the header. Reads session only, no database,
/// because it renders on every single page.
/// </summary>
public class CartBadgeViewComponent : ViewComponent
{
    private readonly ICartService _cart;

    public CartBadgeViewComponent(ICartService cart) => _cart = cart;

    public IViewComponentResult Invoke() => View(_cart.ItemCount());
}
