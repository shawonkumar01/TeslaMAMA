
namespace TeslaMAMA.Models;

public class RideRequest
{
    public int Id { get; set; }

    public string PassengerId { get; set; } = string.Empty;
    public ApplicationUser Passenger { get; set; } = null!;

    public int? PoolId { get; set; }
    public Pool? Pool { get; set; }

    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;

    public int Seats { get; set; } = 1;

    public RideType RideType { get; set; } = RideType.SharedPool;

    public RideStatus Status { get; set; } = RideStatus.Requested;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public decimal FinalFare { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int OfferAttempts { get; set; } = 0;

    public ICollection<RideStatusHistory> StatusHistory { get; set; }
        = new List<RideStatusHistory>();

    public ICollection<PoolDecline> Declines { get; set; }
        = new List<PoolDecline>();

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}
