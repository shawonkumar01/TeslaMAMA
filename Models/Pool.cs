
namespace TeslaMAMA.Models;

public class Pool
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public PoolStatus Status { get; set; } = PoolStatus.Open;
    public int SeatsUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RideRequest> RideRequests { get; set; }
        = new List<RideRequest>();
}
