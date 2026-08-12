using Microsoft.Extensions.Logging;
using VoltElectronics.Application.Common.Abstractions;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Application.Ordering.Commands;

/// <summary>The raw query string the gateway redirected back with — verified, never trusted.</summary>
public sealed record ProcessPaymentCallbackCommand(IReadOnlyDictionary<string, string?> Query)
    : ICommand<PaymentCallbackOutcome>;

/// <summary>
/// Finalizes an order from a gateway redirect. Idempotent: a refreshed or replayed callback reports
/// the order's existing state instead of processing it twice.
/// </summary>
internal sealed class ProcessPaymentCallbackHandler(
    IOrderRepository orders,
    IPaymentGateway gateway,
    IUnitOfWork unitOfWork,
    ILogger<ProcessPaymentCallbackHandler> logger)
    : ICommandHandler<ProcessPaymentCallbackCommand, PaymentCallbackOutcome>
{
    public async Task<PaymentCallbackOutcome> HandleAsync(
        ProcessPaymentCallbackCommand command, CancellationToken cancellationToken)
    {
        var verify = await gateway.VerifyCallbackAsync(command.Query, cancellationToken);
        if (verify.PaymentId is null)
        {
            logger.LogWarning("Payment callback without a payment id: {Query}",
                string.Join("&", command.Query.Select(kv => $"{kv.Key}={kv.Value}")));
            return new PaymentCallbackOutcome(null, false);
        }

        var order = await orders.GetByPaymentIdAsync(verify.PaymentId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("Payment callback for unknown payment {PaymentId}", verify.PaymentId);
            return new PaymentCallbackOutcome(null, false);
        }

        if (!order.IsAwaitingPayment)
            return new PaymentCallbackOutcome(order.OrderNumber, order.PaidAt is not null);

        if (!verify.IsPaid)
        {
            // The order stays payable so the shopper can retry with another card.
            order.RecordPaymentFailure(verify.FailureReason);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Payment failed for {OrderNumber}: {Reason}",
                order.OrderNumber, verify.FailureReason);
            return new PaymentCallbackOutcome(order.OrderNumber, false);
        }

        // Raises OrderPaidDomainEvent; reserving stock and emptying the cart happen in its handler,
        // inside this same transaction.
        order.MarkPaid(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Order {OrderNumber} paid and moved to Processing", order.OrderNumber);
        return new PaymentCallbackOutcome(order.OrderNumber, true);
    }
}
