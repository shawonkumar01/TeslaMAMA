namespace TeslaMAMA.Services;

public static class RouteCompatibilityService
{
    private static readonly string[][] DestinationGroups =
    [
        ["Banani", "Mohakhali", "Gulshan 1", "Gulshan 2"],
        ["Gulshan 1", "Gulshan 2", "Banani", "Notun Bazar", "Badda"],
        ["Mohakhali", "Banani", "Tejgaon", "Farmgate"],
        ["Mirpur", "Tejgaon", "Farmgate"],
        ["Tejgaon", "Farmgate", "Mohakhali", "Mirpur"],
        ["Badda", "Notun Bazar", "Gulshan 2", "Rampura"],
        ["Hatirjheel", "Rampura", "Mohanagar", "Farmgate"],
        ["Mohanagar", "Rampura", "Hatirjheel", "Badda"]
    ];

    public static bool AreDestinationsCompatible(
        string? firstDestination,
        string? secondDestination)
    {
        if (string.IsNullOrWhiteSpace(firstDestination) ||
            string.IsNullOrWhiteSpace(secondDestination))
        {
            return false;
        }

        if (string.Equals(
            firstDestination.Trim(),
            secondDestination.Trim(),
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return DestinationGroups.Any(group =>
            group.Contains(
                firstDestination.Trim(),
                StringComparer.OrdinalIgnoreCase) &&
            group.Contains(
                secondDestination.Trim(),
                StringComparer.OrdinalIgnoreCase));
    }
}