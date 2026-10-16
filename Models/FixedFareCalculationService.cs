
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

    // These distances are illustrative prototype values, not verified
    // live road distances. Replace them with validated route distances.
    private static readonly Dictionary<string, decimal> RouteDistancesKm =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Badda|Banani"] = 4m,
            ["Banani|Gulshan 1"] = 3m,
            ["Banani|Gulshan 2"] = 4m,
            ["Banani|Hatirjheel"] = 6m,
            ["Banani|Mirpur"] = 10m,
            ["Banani|Mohanagar"] = 8m,
            ["Banani|Mohakhali"] = 3m,
            ["Banani|Notun Bazar"] = 6m,
            ["Banani|Rampura"] = 8m,
            ["Banani|Tejgaon"] = 5m,

            ["Farmgate|Banani"] = 6m,
            ["Farmgate|Badda"] = 8m,
            ["Farmgate|Gulshan 1"] = 6m,
            ["Farmgate|Gulshan 2"] = 7m,
            ["Farmgate|Hatirjheel"] = 4m,
            ["Farmgate|Mirpur"] = 8m,
            ["Farmgate|Mohanagar"] = 5m,
            ["Farmgate|Mohakhali"] = 4m,
            ["Farmgate|Notun Bazar"] = 10m,
            ["Farmgate|Rampura"] = 7m,
            ["Farmgate|Tejgaon"] = 3m,

            ["Gulshan 1|Gulshan 2"] = 3m,
            ["Gulshan 1|Hatirjheel"] = 5m,
            ["Gulshan 1|Mirpur"] = 10m,
            ["Gulshan 1|Mohanagar"] = 7m,
            ["Gulshan 1|Mohakhali"] = 3m,
            ["Gulshan 1|Notun Bazar"] = 5m,
            ["Gulshan 1|Rampura"] = 6m,
            ["Gulshan 1|Tejgaon"] = 5m,

            ["Gulshan 2|Hatirjheel"] = 6m,
            ["Gulshan 2|Mirpur"] = 11m,
            ["Gulshan 2|Mohanagar"] = 8m,
            ["Gulshan 2|Mohakhali"] = 4m,
            ["Gulshan 2|Notun Bazar"] = 4m,
            ["Gulshan 2|Rampura"] = 7m,
            ["Gulshan 2|Tejgaon"] = 6m,

            ["Hatirjheel|Mirpur"] = 10m,
            ["Hatirjheel|Mohanagar"] = 5m,
            ["Hatirjheel|Mohakhali"] = 6m,
            ["Hatirjheel|Notun Bazar"] = 8m,
            ["Hatirjheel|Rampura"] = 4m,
            ["Hatirjheel|Tejgaon"] = 5m,

            ["Mirpur|Mohanagar"] = 12m,
            ["Mirpur|Mohakhali"] = 8m,
            ["Mirpur|Notun Bazar"] = 12m,
            ["Mirpur|Rampura"] = 13m,
            ["Mirpur|Tejgaon"] = 7m,

            ["Mohanagar|Mohakhali"] = 7m,
            ["Mohanagar|Notun Bazar"] = 9m,
            ["Mohanagar|Rampura"] = 4m,
            ["Mohanagar|Tejgaon"] = 6m,

            ["Mohakhali|Notun Bazar"] = 6m,
            ["Mohakhali|Rampura"] = 7m,
            ["Mohakhali|Tejgaon"] = 3m,

            ["Notun Bazar|Rampura"] = 6m,
            ["Notun Bazar|Tejgaon"] = 8m,

            ["Rampura|Tejgaon"] = 7m
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
        else if (RouteDistancesKm.TryGetValue(
                     routeKey,
                     out var distanceKm))
        {
            farePerSeat = BaseFare + (PricePerKm * distanceKm);
        }
        else
        {
            return false;
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
}
