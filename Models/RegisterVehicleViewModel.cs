
using System.ComponentModel.DataAnnotations;

namespace TeslaMAMA.Models;

public class RegisterVehicleViewModel
{
    [Required(ErrorMessage = "Enter your vehicle name.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Vehicle name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, 8, ErrorMessage = "Capacity must be between 1 and 8 seats.")]
    [Display(Name = "Passenger capacity")]
    public int Capacity { get; set; } = 3;

    [Required(ErrorMessage = "Select your current location.")]
    [Display(Name = "Current location")]
    public string CurrentZone { get; set; } = string.Empty;

    public List<string> SupportedZones { get; set; } = new();
}
