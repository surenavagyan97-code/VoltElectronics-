namespace VoltElectronics.Infrastructure.Payments;

public class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>"Ameria" (Ameriabank vPOS) or "Fake" (local dev gateway, no credentials needed).</summary>
    public string Provider { get; set; } = "Fake";

    /// <summary>Public base URL of this API — the gateway redirects the shopper back here.</summary>
    public string CallbackBaseUrl { get; set; } = "http://localhost:5002";

    /// <summary>Where to send the shopper after we've processed the gateway callback.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    public AmeriaVposOptions Ameria { get; set; } = new();
}

public class AmeriaVposOptions
{
    /// <summary>Test: https://servicestest.ameriabank.am/VPOS — production: https://services.ameriabank.am/VPOS</summary>
    public string BaseUrl { get; set; } = "https://servicestest.ameriabank.am/VPOS";
    public string ClientId { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Language { get; set; } = "en";

    /// <summary>Ameriabank assigns test merchants a numeric OrderID range; this shifts our int order ids into it.</summary>
    public long OrderIdOffset { get; set; }
}
