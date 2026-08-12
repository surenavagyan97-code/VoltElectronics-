using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Carts.Queries;

public sealed record GetCartQuery(CartKey Key) : IQuery<CartDto>;

/// <summary>
/// Thin by design: reading a cart is exactly what <see cref="ICartReader"/> does, and the command
/// handlers need that same projection to answer with. This keeps one implementation of it.
/// </summary>
internal sealed class GetCartHandler(ICartReader reader) : IQueryHandler<GetCartQuery, CartDto>
{
    public Task<CartDto> HandleAsync(GetCartQuery query, CancellationToken cancellationToken) =>
        reader.ReadAsync(query.Key, cancellationToken);
}
