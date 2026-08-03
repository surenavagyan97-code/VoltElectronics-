using Microsoft.AspNetCore.Identity;

namespace VoltElectronics.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
