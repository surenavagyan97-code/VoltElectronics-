using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Ordering.Queries;

/// <summary>
/// Owner-scoped order detail; null when the order doesn't exist or the caller isn't entitled to it.
/// A user sees their own orders; a guest order is readable by anyone who knows the order number
/// <em>and</em> the checkout email.
/// </summary>
/// <param name="BypassOwnerCheck">Admin endpoints only — reuses this projection without the owner test.</param>
public sealed record GetOrderQuery(
    string OrderNumber,
    string? UserId = null,
    string? Email = null,
    bool BypassOwnerCheck = false) : IQuery<OrderDetailDto?>;
