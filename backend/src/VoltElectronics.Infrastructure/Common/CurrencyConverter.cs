using Microsoft.Extensions.Options;
using VoltElectronics.Application.Common;

namespace VoltElectronics.Infrastructure.Common;

public class CurrencyConverter(IOptions<CurrencyOptions> options) : ICurrencyConverter
{
    private readonly CurrencyOptions _opts = options.Value;

    public string BaseCurrency => _opts.Base;
    public IReadOnlyList<string> SupportedCurrencies => _opts.Supported;

    public bool IsSupported(string currency) =>
        _opts.Supported.Contains(currency, StringComparer.OrdinalIgnoreCase);

    public decimal Rate(string currency) =>
        _opts.Rates.TryGetValue(currency.ToUpperInvariant(), out var rate) ? rate : 1m;

    public decimal Convert(decimal baseAmount, string currency) =>
        Math.Round(baseAmount * Rate(currency), 2);
}
