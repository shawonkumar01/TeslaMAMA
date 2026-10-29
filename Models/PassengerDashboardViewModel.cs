using TeslaMAMA.Models;

namespace TeslaMAMA.Models;

public class PassengerDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;

    public int TotalRides { get; set; }

    public int ActiveRidesCount { get; set; }

    public int CompletedRides { get; set; }

    public decimal TotalSpent { get; set; }

    public List<ActiveRideViewModel> ActiveRides { get; set; } = new();

    public List<PastRideViewModel> PastRides { get; set; } = new();

    public bool HasMorePastRides { get; set; }

    public Dictionary<string, ZonePoint> ZoneCoordinates { get; set; } = new();
}

public class ActiveRideViewModel
{
    public int Id { get; set; }
    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
    public int Seats { get; set; }
    public bool IsSharedPool { get; set; }
    public RideStatus Status { get; set; }
    public decimal FinalFare { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleName { get; set; }
    public string? VehicleCurrentZone { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public int? PoolId { get; set; }
    public int? OtherPassengersCount { get; set; }
    public int? PoolSeatsUsed { get; set; }
    public int? VehicleCapacity { get; set; }
}

public class PastRideViewModel
{
    public int Id { get; set; }
    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
    public int Seats { get; set; }
    public bool IsSharedPool { get; set; }
    public RideStatus Status { get; set; }
    public decimal FinalFare { get; set; }
    public DateTime CreatedAt { get; set; }
}
