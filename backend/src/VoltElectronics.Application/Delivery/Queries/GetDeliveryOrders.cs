using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Delivery.Queries;

/// <summary>Orders assigned to one delivery person, newest first, optionally narrowed by status.</summary>
public sealed record GetDeliveryOrdersQuery(string CourierId, string? Status = null)
    : IQuery<IReadOnlyList<DeliveryOrderDto>>;
