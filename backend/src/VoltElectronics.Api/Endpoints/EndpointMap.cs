using VoltElectronics.Api.Endpoints.Admin;

namespace VoltElectronics.Api.Endpoints;

public static class EndpointMap
{
    /// <summary>Every route the API serves, one Map call per feature area.</summary>
    public static IEndpointRouteBuilder MapVoltEndpoints(this IEndpointRouteBuilder app)
    {
        AuthEndpoints.Map(app);
        CatalogEndpoints.Map(app);
        CartEndpoints.Map(app);
        OrderEndpoints.Map(app);
        PaymentEndpoints.Map(app);
        ConfigEndpoints.Map(app);

        AdminProductEndpoints.Map(app);
        AdminCategoryEndpoints.Map(app);
        AdminOrderEndpoints.Map(app);

        return app;
    }
}
