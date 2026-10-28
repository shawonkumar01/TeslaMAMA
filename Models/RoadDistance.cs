namespace TeslaMAMA.Models;

/// <summary>
/// Stores precomputed road distances from OSRM for zone-to-zone pairs.
/// Populated automatically at startup by RoadDistanceSeeder.
/// </summary>
public class RoadDistance
{
    public int Id { get; set; }
    public string FromZone { get; set; } = string.Empty;
    public string ToZone { get; set; } = string.Empty;
    public double Km { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
