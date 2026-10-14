
using Microsoft.AspNetCore.Identity;

namespace TeslaMAMA.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Passenger;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
