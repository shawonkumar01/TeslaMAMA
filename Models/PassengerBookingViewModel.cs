
using System.ComponentModel.DataAnnotations;

namespace TeslaMAMA.Models;

public class PassengerBookingViewModel
{
    [Required(ErrorMessage = "Select a pickup zone.")]
    public string PickupZone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a destination zone.")]
    public string DestinationZone { get; set; } = string.Empty;

    [Range(1, 3, ErrorMessage = "Choose between 1 and 3 seats.")]
    public int Seats { get; set; } = 1;

    [EnumDataType(
        typeof(PaymentMethod),
        ErrorMessage = "Select a valid payment method.")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public IReadOnlyList<string> SupportedZones { get; set; }
        = Array.Empty<string>();

    public decimal? CalculatedFare { get; set; }
}
