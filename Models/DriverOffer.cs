
namespace TeslaMAMA.Models;

public class DriverOffer
{
    public int Id { get; set; }

    public int RideRequestId { get; set; }
    public RideRequest RideRequest { get; set; } = null!;

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public DriverOfferStatus Status { get; set; }
        = DriverOfferStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
