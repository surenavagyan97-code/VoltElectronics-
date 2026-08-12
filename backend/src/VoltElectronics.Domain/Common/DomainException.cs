namespace VoltElectronics.Domain.Common;

/// <summary>
/// An aggregate was asked to do something its invariants forbid. Callers that can't prevent this
/// up front (concurrent stock changes, replayed callbacks) let it surface as a 400.
/// </summary>
public class DomainException(string message) : Exception(message);
