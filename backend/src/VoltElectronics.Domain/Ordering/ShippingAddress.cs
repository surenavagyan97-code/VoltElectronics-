using VoltElectronics.Domain.Common;

namespace VoltElectronics.Domain.Ordering;

/// <summary>Value object: where the order ships, frozen at checkout.</summary>
public sealed record ShippingAddress(
    string FullName,
    string? Company,
    string Street,
    string City,
    string State,
    string Zip,
    string? Phone)
{
    public static ShippingAddress Create(
        string fullName, string? company, string street, string city, string state, string zip, string? phone)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(street) ||
            string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(zip))
            throw new DomainException("Please fill in all required shipping fields.");

        return new ShippingAddress(
            fullName.Trim(), company?.Trim(), street.Trim(),
            city.Trim(), state.Trim(), zip.Trim(), phone?.Trim());
    }
}
