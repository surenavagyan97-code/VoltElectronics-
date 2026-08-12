using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Admin.Products;

/// <summary>
/// Records an already-stored image against a product. Decoding, resizing and writing the three
/// size variants is the API layer's job — by the time this runs, the URLs are live.
/// </summary>
public sealed record AddProductImageCommand(int ProductId, string Url, string ThumbUrl, string CardUrl)
    : ICommand<Result<ProductImageDto>>;

internal sealed class AddProductImageHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : ICommandHandler<AddProductImageCommand, Result<ProductImageDto>>
{
    public async Task<Result<ProductImageDto>> HandleAsync(
        AddProductImageCommand command, CancellationToken cancellationToken)
    {
        var product = await products.GetAggregateAsync(command.ProductId, cancellationToken);
        if (product is null) return Error.Invalid("Product not found.");

        var image = product.AddImage(command.Url, command.ThumbUrl, command.CardUrl);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductImageDto(image.Id, image.Url, image.ThumbUrl, image.CardUrl, image.SortOrder);
    }
}

public sealed record RemoveProductImageCommand(int ProductId, int ImageId) : ICommand<Result>;

internal sealed class RemoveProductImageHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveProductImageCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveProductImageCommand command, CancellationToken cancellationToken)
    {
        var product = await products.GetAggregateAsync(command.ProductId, cancellationToken);
        if (product is null) return Error.Invalid("Product not found.");

        // Throws "Image not found." when the id isn't one of this product's images.
        product.RemoveImage(command.ImageId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
