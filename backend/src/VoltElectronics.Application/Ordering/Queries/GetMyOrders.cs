using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Ordering.Queries;

public sealed record GetMyOrdersQuery(string UserId) : IQuery<IReadOnlyList<OrderSummaryDto>>;
