using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Models.Catalog;

namespace WebShop.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(64).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
{
    public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
    {
        builder.Property(t => t.Culture).HasMaxLength(8).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(200).IsRequired();
        builder.Property(t => t.MetaTitle).HasMaxLength(200);
        builder.Property(t => t.MetaDescription).HasMaxLength(400);

        // One translation per category per language.
        builder.HasIndex(t => new { t.CategoryId, t.Culture }).IsUnique();

        // Slugs must be unique inside a language, not across languages —
        // "krema" in Serbian and "cream" in English can coexist.
        builder.HasIndex(t => new { t.Culture, t.Slug }).IsUnique();

        builder.HasOne(t => t.Category)
            .WithMany(c => c.Translations)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.Property(b => b.Name).HasMaxLength(120).IsRequired();
        builder.Property(b => b.Slug).HasMaxLength(120).IsRequired();
        builder.HasIndex(b => b.Slug).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Sku).HasMaxLength(64).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        // Catalog pages always filter by category and status together.
        builder.HasIndex(p => new { p.CategoryId, p.Status });
        builder.HasIndex(p => p.IsFeatured);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductTranslationConfiguration : IEntityTypeConfiguration<ProductTranslation>
{
    public void Configure(EntityTypeBuilder<ProductTranslation> builder)
    {
        builder.Property(t => t.Culture).HasMaxLength(8).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(250).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(250).IsRequired();
        builder.Property(t => t.ShortDescription).HasMaxLength(500);
        builder.Property(t => t.MetaTitle).HasMaxLength(200);
        builder.Property(t => t.MetaDescription).HasMaxLength(400);

        builder.HasIndex(t => new { t.ProductId, t.Culture }).IsUnique();
        builder.HasIndex(t => new { t.Culture, t.Slug }).IsUnique();

        builder.HasOne(t => t.Product)
            .WithMany(p => p.Translations)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.Property(i => i.Path).HasMaxLength(400).IsRequired();
        builder.Property(i => i.ThumbnailPath).HasMaxLength(400);
        builder.Property(i => i.AltTextSr).HasMaxLength(250);
        builder.Property(i => i.AltTextEn).HasMaxLength(250);

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.AttributeOption)
            .WithMany()
            .HasForeignKey(i => i.AttributeOptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(v => v.Sku).HasMaxLength(80).IsRequired();
        builder.HasIndex(v => v.Sku).IsUnique();
        builder.HasIndex(v => new { v.ProductId, v.IsActive });

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Computed helpers, not columns.
        builder.Ignore(v => v.IsInStock);
    }
}
