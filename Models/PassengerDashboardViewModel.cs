
using TeslaMAMA.Models;

namespace TeslaMAMA.Models;

public class PassengerDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;

    public int TotalRides { get; set; }

    public int ActiveRides { get; set; }

    public int CompletedRides { get; set; }

    public decimal TotalSpent { get; set; }

    public List<PassengerRideItemViewModel> RecentRides { get; set; } = new();
}

public class PassengerRideItemViewModel
{
    public int Id { get; set; }

    public string PickupZone { get; set; } = string.Empty;

    public string DestinationZone { get; set; } = string.Empty;

    public int Seats { get; set; }

    public RideStatus Status { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public decimal FinalFare { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? PoolId { get; set; }

    public PoolStatus? PoolStatus { get; set; }

    public string? VehicleName { get; set; }
}
