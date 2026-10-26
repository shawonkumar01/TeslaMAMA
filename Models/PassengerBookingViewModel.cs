
using System.ComponentModel.DataAnnotations;

namespace TeslaMAMA.Models;

public class PassengerBookingViewModel
{
    [Required(ErrorMessage = "Select a pickup zone.")]
    public string PickupZone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a destination zone.")]
    public string DestinationZone { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Choose between 1 and 5 seats.")]
    public int Seats { get; set; } = 1;

    [EnumDataType(
        typeof(RideType),
        ErrorMessage = "Select a valid ride type.")]
    public RideType RideType { get; set; } = RideType.SharedPool;

    [EnumDataType(
        typeof(PaymentMethod),
        ErrorMessage = "Select a valid payment method.")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public IReadOnlyList<string> SupportedZones { get; set; }
        = Array.Empty<string>();

    public decimal? CalculatedFare { get; set; }

    public Dictionary<string, ZonePoint> ZoneCoordinates { get; set; }
        = new Dictionary<string, ZonePoint>();
}
