namespace VoltElectronics.Application.Identity;

/// <summary>
/// Role names, shared by the seeder that creates them and the endpoints that authorize against them.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public static readonly string[] All = [Admin, Customer];
}
