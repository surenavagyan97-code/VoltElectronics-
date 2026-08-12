using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Carts.Commands;

public sealed record ClearCartCommand(CartKey Key) : ICommand<Result<CartDto>>;

internal sealed class ClearCartHandler(
    ICartRepository carts,
    ICartReader reader,
    IUnitOfWork unitOfWork) : ICommandHandler<ClearCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> HandleAsync(ClearCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await carts.FindAsync(command.Key, cancellationToken);
        if (cart is not null)
        {
            cart.Clear();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await reader.ReadAsync(command.Key, cancellationToken);
    }
}
