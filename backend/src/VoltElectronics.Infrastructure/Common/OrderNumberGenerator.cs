using VoltElectronics.Application.Common.Abstractions;

namespace VoltElectronics.Infrastructure.Common;

public sealed class OrderNumberGenerator : IOrderNumberGenerator
{
    public string Next() => $"ORD-{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}";
}
