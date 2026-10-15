
namespace TeslaMAMA.Services;

public interface IFareCalculationService
{
    IReadOnlyList<string> GetSupportedZones();

    bool TryCalculateFare(
        string pickupZone,
        string destinationZone,
        int seats,
        out decimal totalFare);
}

public sealed class FixedFareCalculationService : IFareCalculationService
{
    private static readonly string[] Zones =
    {
        "Banani",
        "Mohakhali",
        "Gulshan 1"
    };

    private static readonly Dictionary<string, decimal> RouteFares =
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
        out decimal totalFare)
    {
        totalFare = 0m;

        if (seats is < 1 or > 3)
            return false;

        var pickup = Zones.FirstOrDefault(
            z => string.Equals(
                z, pickupZone, StringComparison.OrdinalIgnoreCase));

        var destination = Zones.FirstOrDefault(
            z => string.Equals(
                z, destinationZone, StringComparison.OrdinalIgnoreCase));

        if (pickup is null ||
            destination is null ||
            string.Equals(pickup, destination, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var routeKey = string.Compare(
            pickup, destination, StringComparison.OrdinalIgnoreCase) < 0
            ? $"{pickup}|{destination}"
            : $"{destination}|{pickup}";

        if (!RouteFares.TryGetValue(routeKey, out var farePerSeat))
            return false;

        totalFare = decimal.Round(farePerSeat * seats, 2);
        return true;
    }
}
