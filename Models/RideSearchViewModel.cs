namespace TeslaMAMA.Models;

public class RideSearchViewModel
{
    public string Pickup { get; set; } = string.Empty;

    public string Dropoff { get; set; } = string.Empty;

    public IReadOnlyList<string> Zones { get; set; } = Array.Empty<string>();
}
