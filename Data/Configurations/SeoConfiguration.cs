using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Models.Catalog;

namespace WebShop.Data.Configurations;

public class SlugRedirectConfiguration : IEntityTypeConfiguration<SlugRedirect>
{
    public void Configure(EntityTypeBuilder<SlugRedirect> builder)
    {
        builder.Property(r => r.Culture).HasMaxLength(8).IsRequired();
        builder.Property(r => r.Slug).HasMaxLength(250).IsRequired();

        // One old address points at one owner per language.
        builder.HasIndex(r => new { r.OwnerType, r.Culture, r.Slug }).IsUnique();

        // Clean-up when a category is deleted.
        builder.HasIndex(r => new { r.OwnerType, r.OwnerId });
    }
}
