
namespace TeslaMAMA.Models;

public class RideStatusHistory
{
    public int Id { get; set; }

    public int RideRequestId { get; set; }
    public RideRequest RideRequest { get; set; } = null!;

    public RideStatus? FromStatus { get; set; }
    public RideStatus ToStatus { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
