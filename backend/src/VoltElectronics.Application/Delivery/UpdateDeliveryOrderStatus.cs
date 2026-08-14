using FluentValidation;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Application.Delivery;

/// <summary>
/// Progress reported from the road: a courier may only mark their own orders, and only as
/// picked up (Shipped) or handed over (Delivered) — everything else stays admin territory.
/// </summary>
public sealed record UpdateDeliveryOrderStatusCommand(string OrderNumber, string CourierId, string Status)
    : ICommand<Result>;

internal sealed class UpdateDeliveryOrderStatusValidator : AbstractValidator<UpdateDeliveryOrderStatusCommand>
{
    public UpdateDeliveryOrderStatusValidator()
    {
        RuleFor(c => c.OrderNumber).NotEmpty();
        RuleFor(c => c.CourierId).NotEmpty();
        RuleFor(c => c.Status).NotEmpty();
    }
}

internal sealed class UpdateDeliveryOrderStatusHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateDeliveryOrderStatusCommand, Result>
{
    private static readonly OrderStatus[] AllowedStatuses = [OrderStatus.Shipped, OrderStatus.Delivered];

    public async Task<Result> HandleAsync(
        UpdateDeliveryOrderStatusCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(command.Status, ignoreCase: true, out var status))
            return Error.Invalid($"Unknown status \"{command.Status}\".");
        if (!AllowedStatuses.Contains(status))
            return Error.Invalid($"Couriers cannot set the {status} status.");

        var order = await orders.GetByOrderNumberAsync(command.OrderNumber, cancellationToken);
        if (order is null || !order.IsAssignedTo(command.CourierId))
            return Error.Invalid("Order not found among your assignments.");

        order.ChangeStatus(status);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
