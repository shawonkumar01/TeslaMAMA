using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;

namespace TeslaMAMA.Services;

/// <summary>
/// Background service that automatically populates road distances from OSRM at startup.
/// Fetches distances for any zone pairs that don't exist in the database.
/// </summary>
public class RoadDistanceSeeder : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly RoadDistanceStore _store;
    private readonly ILogger<RoadDistanceSeeder> _logger;

    public RoadDistanceSeeder(
        IServiceScopeFactory scopes,
        RoadDistanceStore store,
        ILogger<RoadDistanceSeeder> logger)
    {
        _scopes = scopes;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fareService = scope.ServiceProvider.GetRequiredService<IFareCalculationService>();

            // Only fetch distances for zones that are actually used in fare calculations
            var supportedZones = fareService.GetSupportedZones().ToList();
            var allZoneCoords = ZoneCoordinates.GetCoordinates();

            // Filter to only supported zones
            var zones = supportedZones
                .Select(z => new { Key = z, Value = allZoneCoords[z] })
                .Where(z => z.Value != null)
                .ToList();

            var existing = await db.RoadDistances.ToListAsync(ct);
            var have = existing.Select(r => (r.FromZone, r.ToZone)).ToHashSet();

            // Find all zone pairs that don't have stored distances
            var missing = zones
                .SelectMany(a => zones, (a, b) => (From: a.Key, To: b.Key))
                .Where(p => p.From != p.To && !have.Contains(p))
                .ToList();

            if (missing.Count > 0)
            {
                _logger.LogInformation("Fetching {Count} missing road distances from OSRM...", missing.Count);

                // OSRM expects coordinates in lng,lat order
                var coords = string.Join(";", zones.Select(z => $"{z.Value.Lng},{z.Value.Lat}"));
                var url = $"https://router.project-osrm.org/table/v1/driving/{coords}?annotations=distance";

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                using var doc = JsonDocument.Parse(await http.GetStringAsync(url, ct));
                var matrix = doc.RootElement.GetProperty("distances");

                foreach (var (from, to) in missing)
                {
                    var i = zones.FindIndex(z => z.Key == from);
                    var j = zones.FindIndex(z => z.Key == to);

                    if (matrix[i][j].ValueKind == JsonValueKind.Null)
                    {
                        _logger.LogWarning("OSRM returned null for {From} → {To}", from, to);
                        continue;
                    }

                    db.RoadDistances.Add(new RoadDistance
                    {
                        FromZone = from,
                        ToZone = to,
                        Km = Math.Round(matrix[i][j].GetDouble() / 1000, 1) // metres to km
                    });
                }

                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Stored {Count} road distances in database.", missing.Count);
            }

            // Load all distances into memory store
            _store.Load(await db.RoadDistances.ToListAsync(ct));
            _logger.LogInformation("Loaded {Count} road distances into memory store.", _store.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Road distance seeding failed; fares will use straight-line fallback.");
        }
    }
}
