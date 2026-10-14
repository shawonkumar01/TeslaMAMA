
namespace TeslaMAMA.Models;

public class PoolDecline
{
    public int Id { get; set; }

    public int RideRequestId { get; set; }
    public RideRequest RideRequest { get; set; } = null!;

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
