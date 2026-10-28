using TeslaMAMA.Models;

namespace TeslaMAMA.Services;

/// <summary>
/// In-memory cache of road distances from the database.
/// Populated at startup by RoadDistanceSeeder to avoid database queries on every fare calculation.
/// </summary>
public class RoadDistanceStore
{
    private volatile Dictionary<(string, string), double> _km = new();

    /// <summary>
    /// Loads road distances from the database into memory.
    /// </summary>
    public void Load(IEnumerable<RoadDistance> rows) =>
        _km = rows.ToDictionary(
            r => (r.FromZone.ToLowerInvariant(), r.ToZone.ToLowerInvariant()),
            r => r.Km);

    /// <summary>
    /// Attempts to get the road distance between two zones.
    /// </summary>
    public bool TryGet(string from, string to, out double km) =>
        _km.TryGetValue((from.ToLowerInvariant(), to.ToLowerInvariant()), out km);

    /// <summary>
    /// Gets the count of stored distances (for logging).
    /// </summary>
    public int Count => _km.Count;
}
