namespace VoltElectronics.Application.Common;

public interface ICurrencyConverter
{
    string BaseCurrency { get; }
    IReadOnlyList<string> SupportedCurrencies { get; }
    bool IsSupported(string currency);
    decimal Rate(string currency);
    /// <summary>Converts an amount priced in <see cref="BaseCurrency"/> into <paramref name="currency"/>.</summary>
    decimal Convert(decimal baseAmount, string currency);
}
