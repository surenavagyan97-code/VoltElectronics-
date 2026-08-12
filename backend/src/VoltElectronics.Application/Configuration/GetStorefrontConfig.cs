using VoltElectronics.Application.Common.Abstractions;
using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Configuration;

/// <summary>Runtime config the storefront needs — keeps build-time secrets out of the frontend bundle.</summary>
public record StorefrontConfigDto(
    string PaymentProvider,
    string Currency,
    IReadOnlyList<string> SupportedCurrencies,
    IReadOnlyDictionary<string, decimal> Rates);

public sealed record GetStorefrontConfigQuery : IQuery<StorefrontConfigDto>;

/// <summary>Composed from the wired-up adapters, so it always reports what the API will actually do.</summary>
internal sealed class GetStorefrontConfigHandler(
    IPaymentGateway gateway,
    ICurrencyConverter currency) : IQueryHandler<GetStorefrontConfigQuery, StorefrontConfigDto>
{
    public Task<StorefrontConfigDto> HandleAsync(GetStorefrontConfigQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(new StorefrontConfigDto(
            gateway.Name,
            currency.BaseCurrency,
            currency.SupportedCurrencies,
            currency.SupportedCurrencies.ToDictionary(c => c, currency.Rate)));
}
