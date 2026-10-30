using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Models.Community;
using WebShop.Models.Orders;

namespace WebShop.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.OrderNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.Culture).HasMaxLength(8);
        builder.Property(o => o.Email).HasMaxLength(256).IsRequired();
        builder.Property(o => o.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(o => o.LastName).HasMaxLength(100).IsRequired();
        builder.Property(o => o.PhoneNumber).HasMaxLength(40).IsRequired();

        builder.Property(o => o.ShippingStreet).HasMaxLength(250).IsRequired();
        builder.Property(o => o.ShippingStreetNumber).HasMaxLength(40);
        builder.Property(o => o.ShippingCity).HasMaxLength(120).IsRequired();
        builder.Property(o => o.ShippingPostalCode).HasMaxLength(20).IsRequired();
        builder.Property(o => o.ShippingCountry).HasMaxLength(2).IsRequired();

        builder.Property(o => o.BillingStreet).HasMaxLength(250);
        builder.Property(o => o.BillingCity).HasMaxLength(120);
        builder.Property(o => o.BillingPostalCode).HasMaxLength(20);
        builder.Property(o => o.CompanyName).HasMaxLength(250);
        builder.Property(o => o.CompanyTaxNumber).HasMaxLength(40);

        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.VatRate).HasColumnType("decimal(5,4)");

        builder.Property(o => o.PaymentReference).HasMaxLength(128);
        builder.Property(o => o.CourierName).HasMaxLength(120);
        builder.Property(o => o.TrackingNumber).HasMaxLength(120);
        builder.Property(o => o.CustomerNote).HasMaxLength(1000);
        builder.Property(o => o.AdminNote).HasMaxLength(2000);

        // The admin order list sorts by date within a status.
        builder.HasIndex(o => new { o.Status, o.PlacedUtc });
        builder.HasIndex(o => o.UserId);

        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Coupon)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CouponId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(i => i.ProductName).HasMaxLength(250).IsRequired();
        builder.Property(i => i.Sku).HasMaxLength(80).IsRequired();
        builder.Property(i => i.VariantDescription).HasMaxLength(250);
        builder.Property(i => i.ImagePath).HasMaxLength(400);

        builder.Ignore(i => i.LineTotal);

        builder.HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting a variant must never delete order history.
        builder.HasOne(i => i.ProductVariant)
            .WithMany()
            .HasForeignKey(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.Property(a => a.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.LastName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Street).HasMaxLength(250).IsRequired();
        builder.Property(a => a.StreetNumber).HasMaxLength(40);
        builder.Property(a => a.City).HasMaxLength(120).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(2).IsRequired();
        builder.Property(a => a.PhoneNumber).HasMaxLength(40).IsRequired();

        builder.HasIndex(a => new { a.UserId, a.Type });

        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(r => r.Title).HasMaxLength(200);
        builder.Property(r => r.Body).HasMaxLength(4000);

        // One review per customer per product.
        builder.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
        builder.HasIndex(r => new { r.ProductId, r.IsApproved });

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.HasIndex(w => new { w.UserId, w.ProductId, w.ProductVariantId }).IsUnique();

        builder.HasOne(w => w.User)
            .WithMany(u => u.WishlistItems)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Product)
            .WithMany()
            .HasForeignKey(w => w.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.ProductVariant)
            .WithMany()
            .HasForeignKey(w => w.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
