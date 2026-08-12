namespace VoltElectronics.Application.Common.Abstractions;

/// <summary>
/// Mints the human-facing order reference. A port rather than a static helper so checkout stays
/// deterministic under test.
/// </summary>
public interface IOrderNumberGenerator
{
    string Next();
}
