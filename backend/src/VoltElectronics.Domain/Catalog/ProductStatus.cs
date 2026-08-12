namespace VoltElectronics.Domain.Catalog;

public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    // Hidden from the storefront but kept for order history.
    Archived = 2
}
