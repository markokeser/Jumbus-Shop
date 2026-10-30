namespace WebShop.ViewModels.Navigation;

public class CategoryNavGroup
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public List<CategoryNavLink> Children { get; set; } = [];
}

public class CategoryNavLink
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}
