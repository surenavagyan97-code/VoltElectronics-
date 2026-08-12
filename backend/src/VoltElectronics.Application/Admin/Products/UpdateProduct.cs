using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Admin.Products;

public sealed record UpdateProductCommand(int Id, SaveProductRequest Product) : ICommand<Result>;

internal sealed class UpdateProductHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateProductCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var request = command.Product;

        var product = await products.GetAggregateAsync(command.Id, cancellationToken);
        if (product is null) return Error.Invalid("Product not found.");

        // Asked before Describe overwrites the current name.
        var renamed = product.NeedsNewSlug(request.Name);

        product.Describe(
            request.Name, request.Sku, request.CategoryId, request.Description,
            request.Price, request.CompareAtPrice, request.Stock,
            ProductAuthoring.ParseStatus(request.Status), request.Badge);

        if (!await categories.ExistsAsync(request.CategoryId, cancellationToken))
            return Error.Invalid("Category not found.");
        if (await products.SkuExistsAsync(product.Sku, command.Id, cancellationToken))
            return Error.Invalid("SKU already in use.");

        // The name drives the URL, so a rename needs a freshly resolved unique slug.
        if (renamed)
            product.ChangeSlug(await products.UniqueSlugAsync(request.Name, command.Id, cancellationToken));

        product.ReplaceSpecs(request.Specs.ToSpecPairs());

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
