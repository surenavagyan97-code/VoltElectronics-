namespace VoltElectronics.Domain.Common;

/// <summary>
/// Something that happened inside an aggregate that other parts of the system may react to.
/// Raised by the aggregate, dispatched by the persistence layer as part of the same transaction.
/// </summary>
public interface IDomainEvent;
