
using TeslaMAMA.Models;

namespace TeslaMAMA.Services;

public interface IFareCalculationService
{
    IReadOnlyList<string> GetSupportedZones();

    bool TryCalculateFare(
        string pickupZone,
        string destinationZone,
        int seats,
        TeslaMAMA.Models.RideType rideType,
        out decimal totalFare);
}

public sealed class FixedFareCalculationService : IFareCalculationService
{
    private readonly RoadDistanceStore _roadDistances;

    public FixedFareCalculationService(RoadDistanceStore roadDistances)
    {
        _roadDistances = roadDistances;
    }

    private static readonly string[] Zones =
    {
        "Banani",
        "Gulshan 1",
        "Gulshan 2",
        "Mohakhali",
        "Mirpur",
        "Tejgaon",
        "Farmgate",
        "Badda",
        "Notun Bazar",
        "Hatirjheel",
        "Mohanagar",
        "Rampura"
    };

    public const decimal BaseFare = 30m;
    public const decimal PricePerKm = 20m;
    public const decimal PoolDiscountPercent = 15m;

    private static readonly Dictionary<string, decimal> FixedRouteFares =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Banani|Mohakhali"] = 52.80m,
            ["Banani|Gulshan 1"] = 52.16m
        };

    public IReadOnlyList<string> GetSupportedZones() => Zones;

    public bool TryCalculateFare(
        string pickupZone,
        string destinationZone,
        int seats,
        TeslaMAMA.Models.RideType rideType,
        out decimal totalFare)
    {
        totalFare = 0m;

        if (seats is < 1 or > 5 ||
            !Enum.IsDefined(rideType))
        {
            return false;
        }

        var pickup = Zones.FirstOrDefault(
            zone => string.Equals(
                zone,
                pickupZone,
                StringComparison.OrdinalIgnoreCase));

        var destination = Zones.FirstOrDefault(
            zone => string.Equals(
                zone,
                destinationZone,
                StringComparison.OrdinalIgnoreCase));

        if (pickup is null ||
            destination is null ||
            string.Equals(
                pickup,
                destination,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var routeKey = GetRouteKey(pickup, destination);
        decimal farePerSeat;

        if (FixedRouteFares.TryGetValue(routeKey, out var fixedFare))
        {
            farePerSeat = fixedFare;
        }
        else
        {
            // Use precomputed road distance from OSRM
            var distanceKm = GetDistanceKm(pickup, destination);
            if (distanceKm <= 0)
            {
                return false;
            }
            farePerSeat = BaseFare + (PricePerKm * (decimal)distanceKm);
        }

        if (rideType == TeslaMAMA.Models.RideType.SharedPool)
        {
            farePerSeat *= 1m - (PoolDiscountPercent / 100m);
        }

        totalFare = decimal.Round(
            farePerSeat * seats,
            2,
            MidpointRounding.AwayFromZero);

        return true;
    }

    private static string GetRouteKey(
        string pickup,
        string destination)
    {
        return string.Compare(
            pickup,
            destination,
            StringComparison.OrdinalIgnoreCase) < 0
            ? $"{pickup}|{destination}"
            : $"{destination}|{pickup}";
    }

    private double GetDistanceKm(string from, string to)
    {
        // Try precomputed road distance from database (loaded into memory store)
        if (_roadDistances.TryGet(from, to, out var km))
        {
            return km;
        }

        // Fallback: straight-line distance scaled up to approximate roads
        var fromCoord = ZoneCoordinates.GetCoordinate(from);
        var toCoord = ZoneCoordinates.GetCoordinate(to);

        if (fromCoord == null || toCoord == null)
        {
            return 0;
        }

        var straightLineKm = ZoneCoordinates.CalculateDistance(
            fromCoord.Lat,
            fromCoord.Lng,
            toCoord.Lat,
            toCoord.Lng);

        // Roads are typically 1.2-1.5x longer than straight line
        return straightLineKm * 1.3;
    }
}
