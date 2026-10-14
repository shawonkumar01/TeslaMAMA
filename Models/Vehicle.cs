
namespace TeslaMAMA.Models;

public class Vehicle
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; } = 3;
    public string CurrentZone { get; set; } = string.Empty;
    public bool IsOnline { get; set; }

    public string DriverId { get; set; } = string.Empty;
    public ApplicationUser Driver { get; set; } = null!;

    public ICollection<Pool> Pools { get; set; } = new List<Pool>();
    public ICollection<PoolDecline> Declines { get; set; } = new List<PoolDecline>();
}
