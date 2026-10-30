using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Catalog;

namespace WebShop.Services.Admin;

public record UploadResult(int Saved, IReadOnlyList<string> Rejected);

public interface IProductImageService
{
    Task<UploadResult> UploadAsync(int productId, IReadOnlyList<IFormFile> files, CancellationToken ct);

    Task<bool> UpdateAsync(int productId, int imageId, string? altSr, string? altEn, int? optionId, CancellationToken ct);

    Task<bool> SetPrimaryAsync(int productId, int imageId, CancellationToken ct);

    /// <summary>Swaps the photo with its neighbour: -1 earlier, +1 later.</summary>
    Task<bool> MoveAsync(int productId, int imageId, int direction, CancellationToken ct);

    Task<bool> DeleteAsync(int productId, int imageId, CancellationToken ct);
}

/// <summary>
/// Product photos on local disk under wwwroot/media/products/{productId}/.
///
/// A file is accepted only if its first bytes are a JPEG, PNG or WebP
/// signature, and it is stored under a generated name with the extension of
/// what it actually is. The name and type the browser sent are ignored: a
/// file called photo.jpg that is really HTML or SVG would otherwise be served
/// from the shop's own domain, able to run script in a visitor's session.
///
/// Behind a load balancer every server needs to see the same folder (shared
/// storage or a blob store); this class is the one place that would change.
/// </summary>
public class ProductImageService : IProductImageService
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int MaxFilesPerUpload = 12;

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProductImageService> _logger;

    public ProductImageService(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<ProductImageService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<UploadResult> UploadAsync(int productId, IReadOnlyList<IFormFile> files, CancellationToken ct)
    {
        var rejected = new List<string>();

        if (!await _context.Products.AnyAsync(p => p.Id == productId, ct))
        {
            return new UploadResult(0, rejected);
        }

        var images = await _context.ProductImages.Where(i => i.ProductId == productId).ToListAsync(ct);
        var nextOrder = images.Count == 0 ? 0 : images.Max(i => i.DisplayOrder) + 1;
        var hasPrimary = images.Any(i => i.IsPrimary);

        var folder = Path.Combine(_environment.WebRootPath, "media", "products", productId.ToString());
        Directory.CreateDirectory(folder);

        var written = new List<string>();
        var saved = 0;

        foreach (var file in files.Take(MaxFilesPerUpload))
        {
            if (file.Length is 0 or > MaxBytes)
            {
                rejected.Add(file.FileName);
                continue;
            }

            var header = new byte[12];
            int read;

            await using (var stream = file.OpenReadStream())
            {
                read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            }

            if (Detect(header.AsSpan(0, read)) is not { } extension)
            {
                rejected.Add(file.FileName);
                continue;
            }

            var name = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, name);

            await using (var target = File.Create(fullPath))
            {
                await file.CopyToAsync(target, ct);
            }

            written.Add(fullPath);

            _context.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                Path = $"media/products/{productId}/{name}",
                DisplayOrder = nextOrder++,
                IsPrimary = !hasPrimary
            });

            hasPrimary = true;
            saved++;
        }

        rejected.AddRange(files.Skip(MaxFilesPerUpload).Select(f => f.FileName));

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch
        {
            // No rows, so no orphaned files either.
            foreach (var path in written)
            {
                TryDelete(path);
            }

            throw;
        }

        return new UploadResult(saved, rejected);
    }

    public async Task<bool> UpdateAsync(int productId, int imageId, string? altSr, string? altEn, int? optionId, CancellationToken ct)
    {
        var image = await _context.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct);

        if (image is null)
        {
            return false;
        }

        // Only a colour or shade this product is actually sold in.
        if (optionId is { } id && !await _context.VariantAttributeValues
                .AnyAsync(v => v.ProductVariant.ProductId == productId && v.AttributeOptionId == id, ct))
        {
            optionId = null;
        }

        image.AltTextSr = Limit(AdminText.Clean(altSr), 250);
        image.AltTextEn = Limit(AdminText.Clean(altEn), 250);
        image.AttributeOptionId = optionId;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetPrimaryAsync(int productId, int imageId, CancellationToken ct)
    {
        var images = await _context.ProductImages.Where(i => i.ProductId == productId).ToListAsync(ct);

        if (images.All(i => i.Id != imageId))
        {
            return false;
        }

        foreach (var image in images)
        {
            image.IsPrimary = image.Id == imageId;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveAsync(int productId, int imageId, int direction, CancellationToken ct)
    {
        var images = await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

        var index = images.FindIndex(i => i.Id == imageId);
        var other = index + Math.Sign(direction);

        if (index < 0 || direction == 0 || other < 0 || other >= images.Count)
        {
            return false;
        }

        // Renumber first so equal DisplayOrder values cannot make a swap a no-op.
        for (var i = 0; i < images.Count; i++)
        {
            images[i].DisplayOrder = i;
        }

        (images[index].DisplayOrder, images[other].DisplayOrder) = (images[other].DisplayOrder, images[index].DisplayOrder);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int productId, int imageId, CancellationToken ct)
    {
        var image = await _context.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct);

        if (image is null)
        {
            return false;
        }

        _context.ProductImages.Remove(image);

        // The card needs a primary photo; the next one in line takes over.
        if (image.IsPrimary)
        {
            var next = await _context.ProductImages
                .Where(i => i.ProductId == productId && i.Id != imageId)
                .OrderBy(i => i.DisplayOrder)
                .ThenBy(i => i.Id)
                .FirstOrDefaultAsync(ct);

            if (next is not null)
            {
                next.IsPrimary = true;
            }
        }

        await _context.SaveChangesAsync(ct);

        // Only files this service wrote, so a stored path can never point
        // the delete anywhere else on disk.
        if (image.Path.StartsWith("media/products/", StringComparison.Ordinal) && !image.Path.Contains(".."))
        {
            TryDelete(Path.Combine(_environment.WebRootPath, image.Path.Replace('/', Path.DirectorySeparatorChar)));
        }

        return true;
    }

    /// <summary>The real type from the file's first bytes, or null if it is not a supported image.</summary>
    public static string? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        // RIFF....WEBP
        if (header.Length >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }

    private static string? Limit(string? value, int length) =>
        value is not null && value.Length > length ? value[..length] : value;

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete {Path}.", path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Could not delete {Path}.", path);
        }
    }
}
