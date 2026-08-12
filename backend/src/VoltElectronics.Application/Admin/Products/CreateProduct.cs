using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Admin.Products;

/// <summary>Returns the new product's id.</summary>
public sealed record CreateProductCommand(SaveProductRequest Product) : ICommand<Result<int>>;

internal sealed class CreateProductHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateProductCommand, Result<int>>
{
    public async Task<Result<int>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var request = command.Product;

        // Built first, with a provisional slug: the aggregate rejects a bad name, SKU, price or
        // stock before a single query is spent on the catalog-wide checks below.
        var product = Product.Create(
            request.Name, Slug.From(request.Name), request.Sku, request.CategoryId, request.Description,
            request.Price, request.CompareAtPrice, request.Stock,
            ProductAuthoring.ParseStatus(request.Status), request.Badge);

        if (!await categories.ExistsAsync(request.CategoryId, cancellationToken))
            return Error.Invalid("Category not found.");
        if (await products.SkuExistsAsync(product.Sku, cancellationToken: cancellationToken))
            return Error.Invalid("SKU already in use.");

        product.ChangeSlug(await products.UniqueSlugAsync(request.Name, null, cancellationToken));
        product.ReplaceSpecs(request.Specs.ToSpecPairs());

        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
