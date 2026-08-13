using FluentValidation;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Application.Admin.Orders;

/// <summary>Fulfilment progress, set by staff — payment state is never changed from here.</summary>
public sealed record UpdateOrderStatusCommand(string OrderNumber, string Status) : ICommand<Result>;

internal sealed class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(c => c.OrderNumber).NotEmpty();
        // The handler parses the value itself; this just rejects the obviously blank request.
        RuleFor(c => c.Status).NotEmpty();
    }
}

internal sealed class UpdateOrderStatusHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateOrderStatusCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(command.Status, ignoreCase: true, out var status))
            return Error.Invalid($"Unknown status \"{command.Status}\".");

        var order = await orders.GetByOrderNumberAsync(command.OrderNumber, cancellationToken);
        if (order is null) return Error.Invalid("Order not found.");

        order.ChangeStatus(status);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
