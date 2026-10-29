namespace TeslaMAMA.Models;

public class RideDetailsViewModel
{
    public int Id { get; set; }
    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
    public RideStatus Status { get; set; }
    public int Seats { get; set; }
    public bool IsSharedPool { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal FinalFare { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleName { get; set; }
    public int? PoolId { get; set; }
    public int? OtherPassengersCount { get; set; }
    public int? PoolSeatsUsed { get; set; }
    public int? VehicleCapacity { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? DropoffTime { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? PaymentTransactionId { get; set; }
    public Dictionary<string, ZonePoint> ZoneCoordinates { get; set; } = new();
}
