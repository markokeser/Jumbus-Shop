namespace WebShop.Models.Identity;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public static IReadOnlyList<string> All { get; } = [Admin, Customer];
}
