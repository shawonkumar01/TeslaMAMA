
namespace TeslaMAMA.Models;

public class DriverDashboardViewModel
{
    public string DriverName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string CurrentZone { get; set; } = string.Empty;
    public List<string> SupportedZones { get; set; } = new();
    public bool IsOnline { get; set; }

    public int TotalAssignedRides { get; set; }
    public int ActiveRides { get; set; }
    public int CompletedRides { get; set; }

    public List<DriverOfferItemViewModel> PendingOffers { get; set; } = new();
    public List<DriverRideItemViewModel> Rides { get; set; } = new();
}

public class DriverOfferItemViewModel
{
    public int OfferId { get; set; }
    public int RideId { get; set; }

    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;

    public int Seats { get; set; }
    public decimal Fare { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public DateTime ExpiresAt { get; set; }
}

public class DriverRideItemViewModel
{
    public int RideId { get; set; }
    public string PassengerName { get; set; } = string.Empty;

    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;

    public int Seats { get; set; }
    public decimal Fare { get; set; }

    public RideStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public int PoolId { get; set; }
    public PoolStatus PoolStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool CanMarkArrived =>
        Status == RideStatus.Matched;

    public bool CanStartTrip =>
        Status == RideStatus.DriverArrived;

    public bool CanReachDestination =>
        Status == RideStatus.Started;
}