using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.Models.Identity;
using WebShop.Models.Orders;

namespace WebShop.Data.Seed;

/// <summary>
/// Fills an empty database with roles, an admin account and the demo catalog.
/// Every step checks first, so running it against a populated database is a
/// no-op rather than a duplicate-key crash.
/// </summary>
public static class DatabaseSeeder
{
    private const string Sr = CultureConstants.Serbian;
    private const string En = CultureConstants.English;

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var settings = provider.GetRequiredService<IOptions<ShopSettings>>().Value;
        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DatabaseSeeder));

        // The schema is created by sql/01-schema.sql, not by EF migrations,
        // so nothing is migrated here. If the tables are missing, say so
        // plainly rather than failing on the first insert.
        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            logger.LogError(
                "Cannot reach the database. Check ConnectionStrings:DefaultConnection.");
            return;
        }

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager, settings, logger);

        var attributes = await SeedAttributesAsync(context, cancellationToken);
        var categories = await SeedCategoriesAsync(context, attributes, cancellationToken);
        var brands = await SeedBrandsAsync(context, cancellationToken);
        await SeedProductsAsync(context, categories, brands, attributes, cancellationToken);
        await SeedCouponsAsync(context, cancellationToken);

        logger.LogInformation("Seeding finished.");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        ShopSettings settings,
        ILogger logger)
    {
        if (await userManager.FindByEmailAsync(settings.AdminEmail) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = settings.AdminEmail,
            Email = settings.AdminEmail,
            EmailConfirmed = true,
            FirstName = "Shop",
            LastName = "Admin",
            PreferredCulture = Sr
        };

        var result = await userManager.CreateAsync(admin, settings.AdminPassword);

        if (!result.Succeeded)
        {
            logger.LogError(
                "Could not create the admin account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
        logger.LogWarning(
            "Admin account created with the password from appsettings.json. Change it before going live.");
    }

    private static async Task<Dictionary<string, AttributeDefinition>> SeedAttributesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var existing = await context.AttributeDefinitions
            .Include(a => a.Options)
            .ToDictionaryAsync(a => a.Code, cancellationToken);

        foreach (var seed in SeedCatalog.Attributes)
        {
            if (existing.ContainsKey(seed.Code))
            {
                continue;
            }

            var definition = new AttributeDefinition
            {
                Code = seed.Code,
                DataType = seed.DataType,
                IsVariantDefining = seed.IsVariantDefining,
                IsFilterable = seed.IsFilterable,
                Unit = seed.Unit,
                DisplayOrder = seed.DisplayOrder,
                Translations =
                [
                    new AttributeDefinitionTranslation { Culture = Sr, Name = seed.NameSr },
                    new AttributeDefinitionTranslation { Culture = En, Name = seed.NameEn }
                ]
            };

            var order = 0;
            foreach (var (code, labelSr, labelEn, swatch) in seed.Options)
            {
                definition.Options.Add(new AttributeOption
                {
                    Code = code,
                    SwatchHex = swatch,
                    DisplayOrder = order += 10,
                    Translations =
                    [
                        new AttributeOptionTranslation { Culture = Sr, Label = labelSr },
                        new AttributeOptionTranslation { Culture = En, Label = labelEn }
                    ]
                });
            }

            context.AttributeDefinitions.Add(definition);
            existing[seed.Code] = definition;
        }

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private static async Task<Dictionary<string, Category>> SeedCategoriesAsync(
        ApplicationDbContext context,
        Dictionary<string, AttributeDefinition> attributes,
        CancellationToken cancellationToken)
    {
        var existing = await context.Categories.ToDictionaryAsync(c => c.Code, cancellationToken);

        // Two passes: parents first, so children can resolve ParentId.
        foreach (var seed in SeedCatalog.Categories.Where(c => c.ParentCode is null))
        {
            AddCategory(context, existing, seed, parent: null);
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var seed in SeedCatalog.Categories.Where(c => c.ParentCode is not null))
        {
            var parent = existing[seed.ParentCode!];
            AddCategory(context, existing, seed, parent);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Attach the attribute set to each category once the ids exist.
        var links = await context.CategoryAttributes
            .Select(ca => new { ca.CategoryId, ca.AttributeDefinitionId })
            .ToListAsync(cancellationToken);

        var linkSet = links.Select(l => (l.CategoryId, l.AttributeDefinitionId)).ToHashSet();

        foreach (var seed in SeedCatalog.Categories)
        {
            var category = existing[seed.Code];
            var order = 0;

            foreach (var attributeCode in seed.AttributeCodes)
            {
                var definition = attributes[attributeCode];

                if (linkSet.Contains((category.Id, definition.Id)))
                {
                    continue;
                }

                context.CategoryAttributes.Add(new CategoryAttribute
                {
                    CategoryId = category.Id,
                    AttributeDefinitionId = definition.Id,
                    IsRequired = definition.IsVariantDefining,
                    DisplayOrder = order += 10
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private static void AddCategory(
        ApplicationDbContext context,
        Dictionary<string, Category> existing,
        SeedCatalog.CategorySeed seed,
        Category? parent)
    {
        if (existing.ContainsKey(seed.Code))
        {
            return;
        }

        var category = new Category
        {
            Code = seed.Code,
            Parent = parent,
            DisplayOrder = seed.DisplayOrder,
            IsActive = true,
            Translations =
            [
                new CategoryTranslation
                {
                    Culture = Sr,
                    Name = seed.NameSr,
                    Slug = seed.SlugSr,
                    Description = seed.DescriptionSr,
                    MetaTitle = seed.NameSr,
                    MetaDescription = seed.DescriptionSr
                },
                new CategoryTranslation
                {
                    Culture = En,
                    Name = seed.NameEn,
                    Slug = seed.SlugEn,
                    Description = seed.DescriptionEn,
                    MetaTitle = seed.NameEn,
                    MetaDescription = seed.DescriptionEn
                }
            ]
        };

        context.Categories.Add(category);
        existing[seed.Code] = category;
    }

    private static async Task<Dictionary<string, Brand>> SeedBrandsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var existing = await context.Brands.ToDictionaryAsync(b => b.Name, cancellationToken);

        foreach (var seed in SeedCatalog.Brands)
        {
            if (existing.ContainsKey(seed.Name))
            {
                continue;
            }

            var brand = new Brand { Name = seed.Name, Slug = seed.Slug };
            context.Brands.Add(brand);
            existing[seed.Name] = brand;
        }

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private static async Task SeedProductsAsync(
        ApplicationDbContext context,
        Dictionary<string, Category> categories,
        Dictionary<string, Brand> brands,
        Dictionary<string, AttributeDefinition> attributes,
        CancellationToken cancellationToken)
    {
        var existingSkus = await context.Products
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken);

        var skuSet = existingSkus.ToHashSet();

        // Deterministic pseudo-random stock, so repeated seeding gives the
        // same numbers and screenshots stay comparable between runs.
        var random = new Random(20260910);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var seed in SeedCatalog.Products)
        {
            if (skuSet.Contains(seed.Sku))
            {
                continue;
            }

            var product = new Product
            {
                Sku = seed.Sku,
                Category = categories[seed.CategoryCode],
                Brand = brands[seed.BrandName],
                Status = ProductStatus.Published,
                IsFeatured = seed.IsFeatured,
                PublishedUtc = DateTime.UtcNow,
                Translations =
                [
                    new ProductTranslation
                    {
                        Culture = Sr,
                        Name = seed.NameSr,
                        Slug = SlugGenerator.Generate(seed.NameSr),
                        ShortDescription = seed.ShortSr,
                        Description = seed.DescriptionSr,
                        Ingredients = seed.IngredientsSr,
                        CareInstructions = seed.CareSr,
                        MetaTitle = $"{seed.NameSr} | {seed.BrandName}",
                        MetaDescription = seed.ShortSr
                    },
                    new ProductTranslation
                    {
                        Culture = En,
                        Name = seed.NameEn,
                        Slug = SlugGenerator.Generate(seed.NameEn),
                        ShortDescription = seed.ShortEn,
                        Description = seed.DescriptionEn,
                        Ingredients = seed.IngredientsEn,
                        CareInstructions = seed.CareEn,
                        MetaTitle = $"{seed.NameEn} | {seed.BrandName}",
                        MetaDescription = seed.ShortEn
                    }
                ]
            };

            // Placeholder image paths. Phase 5 replaces these through the admin
            // upload; until then the views fall back to a neutral placeholder.
            product.Images.Add(new ProductImage
            {
                Path = $"media/products/{seed.Sku.ToLowerInvariant()}-01.jpg",
                ThumbnailPath = $"media/products/thumbs/{seed.Sku.ToLowerInvariant()}-01.jpg",
                AltTextSr = seed.NameSr,
                AltTextEn = seed.NameEn,
                IsPrimary = true,
                DisplayOrder = 10
            });

            foreach (var variant in BuildVariants(seed, attributes, random, today))
            {
                product.Variants.Add(variant);
            }

            context.Products.Add(product);
            skuSet.Add(seed.Sku);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Expands a product's axes into concrete variants: sizes crossed with
    /// colours for clothing, shades or volumes for cosmetics. A product with
    /// no axes at all still gets one variant, because the cart can only ever
    /// hold variants.
    /// </summary>
    private static IEnumerable<ProductVariant> BuildVariants(
        SeedCatalog.ProductSeed seed,
        Dictionary<string, AttributeDefinition> attributes,
        Random random,
        DateOnly today)
    {
        var sizeAttribute = attributes["size"];
        var colorAttribute = attributes["color"];
        var shadeAttribute = attributes["shade"];
        var volumeAttribute = attributes["volume"];
        var skinTypeAttribute = attributes["skin-type"];

        // Each axis becomes a list of "apply this to the variant" actions.
        var axes = new List<List<(string Suffix, Action<ProductVariant> Apply)>>();

        if (seed.Colors.Length > 0)
        {
            axes.Add(seed.Colors.Select(code =>
            {
                var option = FindOption(colorAttribute, code);
                return (
                    code.ToUpperInvariant(),
                    new Action<ProductVariant>(v => v.AttributeValues.Add(new VariantAttributeValue
                    {
                        AttributeDefinition = colorAttribute,
                        AttributeOption = option
                    })));
            }).ToList());
        }

        if (seed.Sizes.Length > 0)
        {
            axes.Add(seed.Sizes.Select(code =>
            {
                var option = FindOption(sizeAttribute, code);
                return (
                    code.ToUpperInvariant(),
                    new Action<ProductVariant>(v => v.AttributeValues.Add(new VariantAttributeValue
                    {
                        AttributeDefinition = sizeAttribute,
                        AttributeOption = option
                    })));
            }).ToList());
        }

        if (seed.Shades.Length > 0)
        {
            axes.Add(seed.Shades.Select(code =>
            {
                var option = FindOption(shadeAttribute, code);
                return (
                    code.Split('-')[0].ToUpperInvariant(),
                    new Action<ProductVariant>(v => v.AttributeValues.Add(new VariantAttributeValue
                    {
                        AttributeDefinition = shadeAttribute,
                        AttributeOption = option
                    })));
            }).ToList());
        }

        if (seed.Volumes.Length > 0)
        {
            axes.Add(seed.Volumes.Select(volume => (
                $"{volume:0}ML",
                new Action<ProductVariant>(v => v.AttributeValues.Add(new VariantAttributeValue
                {
                    AttributeDefinition = volumeAttribute,
                    NumericValue = volume
                })))).ToList());
        }

        // Cartesian product of every axis.
        var combinations = new List<(List<string> Suffixes, List<Action<ProductVariant>> Actions)>
        {
            ([], [])
        };

        foreach (var axis in axes)
        {
            combinations =
            [
                .. from combination in combinations
                   from step in axis
                   select (
                       Suffixes: new List<string>(combination.Suffixes) { step.Suffix },
                       Actions: new List<Action<ProductVariant>>(combination.Actions) { step.Apply })
            ];
        }

        foreach (var (suffixes, actions) in combinations)
        {
            var suffix = suffixes.Count > 0 ? "-" + string.Join('-', suffixes) : "-STD";

            // Larger volumes cost more; the seed price refers to the smallest.
            var priceMultiplier = 1m;
            if (seed.Volumes.Length > 1)
            {
                var volumeSuffix = suffixes.FirstOrDefault(s => s.EndsWith("ML", StringComparison.Ordinal));
                if (volumeSuffix is not null
                    && decimal.TryParse(volumeSuffix[..^2], out var volume)
                    && volume > seed.Volumes.Min())
                {
                    priceMultiplier = 1.7m;
                }
            }

            var variant = new ProductVariant
            {
                Sku = seed.Sku + suffix,
                Price = Math.Round(seed.Price * priceMultiplier / 10m, 0) * 10m,
                SalePrice = seed.SalePrice is { } sale
                    ? Math.Round(sale * priceMultiplier / 10m, 0) * 10m
                    : null,
                SaleStartsUtc = seed.SalePrice is null ? null : DateTime.UtcNow.AddDays(-7),
                SaleEndsUtc = seed.SalePrice is null ? null : DateTime.UtcNow.AddDays(30),
                StockQuantity = random.Next(0, 26),
                ExpiryDate = seed.ShelfLifeMonths is { } months
                    ? today.AddMonths(months)
                    : null,
                WeightGrams = seed.Volumes.Length > 0 ? 250 : 500,
                IsActive = true
            };

            foreach (var apply in actions)
            {
                apply(variant);
            }

            // Skin type is the same across every variant of a product, but it
            // still lives on the variant so the filter has a single place to look.
            if (seed.SkinType is { } skinTypeCode)
            {
                variant.AttributeValues.Add(new VariantAttributeValue
                {
                    AttributeDefinition = skinTypeAttribute,
                    AttributeOption = FindOption(skinTypeAttribute, skinTypeCode)
                });
            }

            yield return variant;
        }
    }

    private static AttributeOption FindOption(AttributeDefinition definition, string code) =>
        definition.Options.FirstOrDefault(o => o.Code == code)
        ?? throw new InvalidOperationException(
            $"Seed data refers to option '{code}' which is not defined under attribute '{definition.Code}'.");

    private static async Task SeedCouponsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        if (await context.Coupons.AnyAsync(cancellationToken))
        {
            return;
        }

        context.Coupons.AddRange(
            new Coupon
            {
                Code = "DOBRODOSLI10",
                DiscountType = DiscountType.Percentage,
                Value = 10m,
                MinimumSubtotal = 3000m,
                MaximumDiscount = 3000m,
                StartsUtc = DateTime.UtcNow.AddDays(-1),
                EndsUtc = DateTime.UtcNow.AddMonths(6),
                IsActive = true
            },
            new Coupon
            {
                Code = "BESPLATNADOSTAVA",
                DiscountType = DiscountType.FreeShipping,
                Value = 0m,
                MinimumSubtotal = 2000m,
                StartsUtc = DateTime.UtcNow.AddDays(-1),
                EndsUtc = DateTime.UtcNow.AddMonths(3),
                UsageLimit = 500,
                IsActive = true
            },
            new Coupon
            {
                Code = "KOZMETIKA500",
                DiscountType = DiscountType.FixedAmount,
                Value = 500m,
                MinimumSubtotal = 4000m,
                StartsUtc = DateTime.UtcNow.AddDays(-1),
                EndsUtc = DateTime.UtcNow.AddMonths(2),
                IsActive = true
            });

        await context.SaveChangesAsync(cancellationToken);
    }
}
