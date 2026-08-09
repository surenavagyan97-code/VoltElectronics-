namespace VoltElectronics.Application.Common;

/// <summary>Fixed exchange rates — all product prices and internal order math are in Base.</summary>
public class CurrencyOptions
{
    public const string SectionName = "Currency";

    public string Base { get; set; } = "USD";

    // Left empty by default (always populated from appsettings' Currency:Supported) rather than
    // pre-seeded — .NET's config binder appends array values onto an existing default array
    // instead of replacing it, which would silently duplicate every entry here.
    public string[] Supported { get; set; } = [];

    /// <summary>Units of each currency per 1 unit of Base (e.g. AMD: 387 means 1 USD = 387 AMD).</summary>
    public Dictionary<string, decimal> Rates { get; set; } = new()
    {
        ["USD"] = 1.0m,
        ["EUR"] = 0.92m,
        ["AMD"] = 387.0m,
    };
}
