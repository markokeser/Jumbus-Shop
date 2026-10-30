namespace WebShop.Infrastructure;

/// <summary>
/// Courier services offered at checkout and when an order is shipped. One
/// list, so the admin cannot record a courier the shop never offered.
/// </summary>
public static class Couriers
{
    public static readonly IReadOnlyList<string> All =
        ["Pošta Srbije", "BEX", "D Express", "City Express", "AKS"];
}
