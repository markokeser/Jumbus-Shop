using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Models.Catalog;

namespace WebShop.Data.Configurations;

public class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.Property(a => a.Code).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Unit).HasMaxLength(16);
        builder.HasIndex(a => a.Code).IsUnique();
    }
}

public class AttributeDefinitionTranslationConfiguration
    : IEntityTypeConfiguration<AttributeDefinitionTranslation>
{
    public void Configure(EntityTypeBuilder<AttributeDefinitionTranslation> builder)
    {
        builder.Property(t => t.Culture).HasMaxLength(8).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(120).IsRequired();

        builder.HasIndex(t => new { t.AttributeDefinitionId, t.Culture }).IsUnique();

        builder.HasOne(t => t.AttributeDefinition)
            .WithMany(a => a.Translations)
            .HasForeignKey(t => t.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttributeOptionConfiguration : IEntityTypeConfiguration<AttributeOption>
{
    public void Configure(EntityTypeBuilder<AttributeOption> builder)
    {
        builder.Property(o => o.Code).HasMaxLength(64).IsRequired();
        builder.Property(o => o.SwatchHex).HasMaxLength(9);

        // "m" is a valid code under size and could also be one under shade,
        // so uniqueness is scoped to the attribute rather than global.
        builder.HasIndex(o => new { o.AttributeDefinitionId, o.Code }).IsUnique();

        builder.HasOne(o => o.AttributeDefinition)
            .WithMany(a => a.Options)
            .HasForeignKey(o => o.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttributeOptionTranslationConfiguration
    : IEntityTypeConfiguration<AttributeOptionTranslation>
{
    public void Configure(EntityTypeBuilder<AttributeOptionTranslation> builder)
    {
        builder.Property(t => t.Culture).HasMaxLength(8).IsRequired();
        builder.Property(t => t.Label).HasMaxLength(120).IsRequired();

        builder.HasIndex(t => new { t.AttributeOptionId, t.Culture }).IsUnique();

        builder.HasOne(t => t.AttributeOption)
            .WithMany(o => o.Translations)
            .HasForeignKey(t => t.AttributeOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
{
    public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {
        builder.HasKey(ca => new { ca.CategoryId, ca.AttributeDefinitionId });

        builder.HasOne(ca => ca.Category)
            .WithMany(c => c.CategoryAttributes)
            .HasForeignKey(ca => ca.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ca => ca.AttributeDefinition)
            .WithMany(a => a.CategoryAttributes)
            .HasForeignKey(ca => ca.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VariantAttributeValueConfiguration : IEntityTypeConfiguration<VariantAttributeValue>
{
    public void Configure(EntityTypeBuilder<VariantAttributeValue> builder)
    {
        builder.Property(v => v.TextValue).HasMaxLength(400);

        // A variant holds one value per attribute — never two sizes.
        builder.HasIndex(v => new { v.ProductVariantId, v.AttributeDefinitionId }).IsUnique();

        // The filter service joins on these two columns constantly.
        builder.HasIndex(v => new { v.AttributeDefinitionId, v.AttributeOptionId });

        builder.HasOne(v => v.ProductVariant)
            .WithMany(pv => pv.AttributeValues)
            .HasForeignKey(v => v.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict rather than Cascade: deleting an option that is in use
        // should fail loudly instead of quietly wiping variant data.
        builder.HasOne(v => v.AttributeDefinition)
            .WithMany()
            .HasForeignKey(v => v.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.AttributeOption)
            .WithMany(o => o.VariantValues)
            .HasForeignKey(v => v.AttributeOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
