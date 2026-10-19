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

    // Maximum detour percentage allowed for pooling
    // Real-world ride-sharing apps typically allow 20-50% detour
    // For this demo, we use 100% to encourage pooling for testing
    private const double MaxDetourPercentage = 100.0;

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

    public static bool AreRoutesCompatible(
        string pickupZone,
        string existingDestination,
        string newDestination,
        double maxDetourPercentage = MaxDetourPercentage)
    {
        if (string.IsNullOrWhiteSpace(pickupZone) ||
            string.IsNullOrWhiteSpace(existingDestination) ||
            string.IsNullOrWhiteSpace(newDestination))
        {
            return false;
        }

        pickupZone = pickupZone.Trim();
        existingDestination = existingDestination.Trim();
        newDestination = newDestination.Trim();

        // Check if destinations are in compatible zones
        if (!AreDestinationsCompatible(existingDestination, newDestination))
        {
            return false;
        }

        // Calculate detour percentage
        var directDistance = DistanceMatrixService.GetDistance(pickupZone, existingDestination);
        if (directDistance == 0 || directDistance == double.MaxValue)
        {
            return false;
        }

        // Calculate route with new destination
        var routeWithNewDestination = DistanceMatrixService.CalculateRouteDistance(
            [pickupZone, newDestination, existingDestination]);

        // Alternative route: pickup -> existing -> new
        var alternativeRoute = DistanceMatrixService.CalculateRouteDistance(
            [pickupZone, existingDestination, newDestination]);

        // Use the shorter of the two routes
        var optimalRouteDistance = Math.Min(routeWithNewDestination, alternativeRoute);

        // Calculate detour percentage
        var detour = optimalRouteDistance - directDistance;
        var detourPercentage = (detour / directDistance) * 100;

        return detourPercentage <= maxDetourPercentage;
    }

    public static (string[] optimalStops, double totalDistance) CalculateOptimalRoute(
        string pickupZone,
        List<string> destinations)
    {
        if (destinations == null || destinations.Count == 0)
        {
            return ([pickupZone], 0);
        }

        if (destinations.Count == 1)
        {
            var distance = DistanceMatrixService.GetDistance(pickupZone, destinations[0]);
            return ([pickupZone, destinations[0]], distance);
        }

        // Simple greedy algorithm for route optimization
        // In production, use TSP algorithm for better optimization
        var remainingDestinations = destinations.ToList();
        var currentLocation = pickupZone;
        var optimalStops = new List<string> { pickupZone };
        var totalDistance = 0.0;

        while (remainingDestinations.Count > 0)
        {
            var nearestDestination = remainingDestinations
                .OrderBy(d => DistanceMatrixService.GetDistance(currentLocation, d))
                .First();

            var distance = DistanceMatrixService.GetDistance(currentLocation, nearestDestination);
            totalDistance += distance;
            optimalStops.Add(nearestDestination);
            currentLocation = nearestDestination;
            remainingDestinations.Remove(nearestDestination);
        }

        return (optimalStops.ToArray(), totalDistance);
    }

    public static double CalculateDetourPercentage(
        string pickupZone,
        string primaryDestination,
        string additionalDestination)
    {
        var directDistance = DistanceMatrixService.GetDistance(pickupZone, primaryDestination);
        if (directDistance == 0 || directDistance == double.MaxValue)
        {
            return 0;
        }

        // Calculate route with additional stop
        var routeWithStop = DistanceMatrixService.CalculateRouteDistance(
            [pickupZone, additionalDestination, primaryDestination]);

        var alternativeRoute = DistanceMatrixService.CalculateRouteDistance(
            [pickupZone, primaryDestination, additionalDestination]);

        var optimalRouteDistance = Math.Min(routeWithStop, alternativeRoute);

        var detour = optimalRouteDistance - directDistance;
        return (detour / directDistance) * 100;
    }
}