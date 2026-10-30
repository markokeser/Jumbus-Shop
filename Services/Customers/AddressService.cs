using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Orders;
using WebShop.ViewModels.Account;

namespace WebShop.Services.Customers;

public interface IAddressService
{
    Task<List<Address>> ListAsync(string userId, CancellationToken ct);

    Task<Address?> FindAsync(int id, string userId, CancellationToken ct);

    Task<Address?> DefaultShippingAsync(string userId, CancellationToken ct);

    Task<bool> SaveAsync(AddressFormViewModel form, string userId, CancellationToken ct);

    Task<bool> DeleteAsync(int id, string userId, CancellationToken ct);

    Task<bool> MakeDefaultAsync(int id, string userId, CancellationToken ct);
}

/// <summary>
/// Every read and write is filtered by the signed-in user's id. An address id
/// arrives from the browser and is trivially changed, so "find address 42"
/// without "that belongs to this user" would let anyone edit anyone's address.
/// </summary>
public class AddressService : IAddressService
{
    private readonly ApplicationDbContext _context;

    public AddressService(ApplicationDbContext context) => _context = context;

    public async Task<List<Address>> ListAsync(string userId, CancellationToken ct) =>
        await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);

    public async Task<Address?> FindAsync(int id, string userId, CancellationToken ct) =>
        await _context.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

    public async Task<Address?> DefaultShippingAsync(string userId, CancellationToken ct) =>
        await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.Type == AddressType.Shipping)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<bool> SaveAsync(AddressFormViewModel form, string userId, CancellationToken ct)
    {
        Address? address;

        if (form.Id > 0)
        {
            address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == form.Id && a.UserId == userId, ct);

            if (address is null)
            {
                return false;
            }
        }
        else
        {
            address = new Address { UserId = userId, Type = AddressType.Shipping };
            _context.Addresses.Add(address);
        }

        address.FirstName = form.FirstName.Trim();
        address.LastName = form.LastName.Trim();
        address.Street = form.Street.Trim();
        address.StreetNumber = form.StreetNumber?.Trim();
        address.City = form.City.Trim();
        address.PostalCode = form.PostalCode.Trim();
        address.PhoneNumber = form.PhoneNumber.Trim();
        address.Country = "RS";

        // The first address a customer saves becomes the default without
        // asking; otherwise checkout would have nothing to pre-fill from.
        var hasOthers = await _context.Addresses
            .AnyAsync(a => a.UserId == userId && a.Type == address.Type && a.Id != address.Id, ct);

        address.IsDefault = form.IsDefault || !hasOthers;

        if (address.IsDefault)
        {
            await ClearOtherDefaultsAsync(userId, address.Type, address.Id, ct);
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, string userId, CancellationToken ct)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

        if (address is null)
        {
            return false;
        }

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync(ct);

        // Deleting the default should not leave the customer with no default
        // while other addresses still exist.
        if (address.IsDefault)
        {
            var next = await _context.Addresses
                .Where(a => a.UserId == userId && a.Type == address.Type)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync(ct);

            if (next is not null)
            {
                next.IsDefault = true;
                await _context.SaveChangesAsync(ct);
            }
        }

        return true;
    }

    public async Task<bool> MakeDefaultAsync(int id, string userId, CancellationToken ct)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

        if (address is null)
        {
            return false;
        }

        await ClearOtherDefaultsAsync(userId, address.Type, address.Id, ct);
        address.IsDefault = true;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private async Task ClearOtherDefaultsAsync(string userId, AddressType type, int keepId, CancellationToken ct)
    {
        var others = await _context.Addresses
            .Where(a => a.UserId == userId && a.Type == type && a.IsDefault && a.Id != keepId)
            .ToListAsync(ct);

        foreach (var other in others)
        {
            other.IsDefault = false;
        }
    }
}
