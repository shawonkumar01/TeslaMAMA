namespace TeslaMAMA.Services;

public static class DistanceMatrixService
{
    // Simplified distance matrix for demo purposes
    // In production, this would use a real routing API like Google Maps or OpenRouteService
    private static readonly Dictionary<string, Dictionary<string, double>> DistanceMatrix = new()
    {
        ["Banani"] = new()
        {
            ["Banani"] = 0,
            ["Mohakhali"] = 2.5,
            ["Gulshan 1"] = 3.0,
            ["Gulshan 2"] = 3.5,
            ["Tejgaon"] = 5.0,
            ["Farmgate"] = 6.0,
            ["Mirpur"] = 8.0,
            ["Notun Bazar"] = 4.0,
            ["Badda"] = 5.5,
            ["Rampura"] = 6.5,
            ["Hatirjheel"] = 7.0,
            ["Mohanagar"] = 8.5
        },
        ["Mohakhali"] = new()
        {
            ["Banani"] = 2.5,
            ["Mohakhali"] = 0,
            ["Gulshan 1"] = 2.0,
            ["Gulshan 2"] = 2.5,
            ["Tejgaon"] = 3.5,
            ["Farmgate"] = 4.5,
            ["Mirpur"] = 7.0,
            ["Notun Bazar"] = 3.0,
            ["Badda"] = 4.0,
            ["Rampura"] = 5.0,
            ["Hatirjheel"] = 5.5,
            ["Mohanagar"] = 7.0
        },
        ["Gulshan 1"] = new()
        {
            ["Banani"] = 3.0,
            ["Mohakhali"] = 2.0,
            ["Gulshan 1"] = 0,
            ["Gulshan 2"] = 1.5,
            ["Tejgaon"] = 4.0,
            ["Farmgate"] = 5.0,
            ["Mirpur"] = 8.0,
            ["Notun Bazar"] = 3.5,
            ["Badda"] = 4.5,
            ["Rampura"] = 5.5,
            ["Hatirjheel"] = 6.0,
            ["Mohanagar"] = 7.5
        },
        ["Gulshan 2"] = new()
        {
            ["Banani"] = 3.5,
            ["Mohakhali"] = 2.5,
            ["Gulshan 1"] = 1.5,
            ["Gulshan 2"] = 0,
            ["Tejgaon"] = 4.5,
            ["Farmgate"] = 5.5,
            ["Mirpur"] = 8.5,
            ["Notun Bazar"] = 3.0,
            ["Badda"] = 3.5,
            ["Rampura"] = 4.5,
            ["Hatirjheel"] = 5.0,
            ["Mohanagar"] = 6.5
        },
        ["Tejgaon"] = new()
        {
            ["Banani"] = 5.0,
            ["Mohakhali"] = 3.5,
            ["Gulshan 1"] = 4.0,
            ["Gulshan 2"] = 4.5,
            ["Tejgaon"] = 0,
            ["Farmgate"] = 2.0,
            ["Mirpur"] = 4.0,
            ["Notun Bazar"] = 5.0,
            ["Badda"] = 6.0,
            ["Rampura"] = 7.0,
            ["Hatirjheel"] = 4.0,
            ["Mohanagar"] = 5.5
        },
        ["Farmgate"] = new()
        {
            ["Banani"] = 6.0,
            ["Mohakhali"] = 4.5,
            ["Gulshan 1"] = 5.0,
            ["Gulshan 2"] = 5.5,
            ["Tejgaon"] = 2.0,
            ["Farmgate"] = 0,
            ["Mirpur"] = 3.0,
            ["Notun Bazar"] = 6.0,
            ["Badda"] = 7.0,
            ["Rampura"] = 8.0,
            ["Hatirjheel"] = 3.0,
            ["Mohanagar"] = 4.5
        },
        ["Mirpur"] = new()
        {
            ["Banani"] = 8.0,
            ["Mohakhali"] = 7.0,
            ["Gulshan 1"] = 8.0,
            ["Gulshan 2"] = 8.5,
            ["Tejgaon"] = 4.0,
            ["Farmgate"] = 3.0,
            ["Mirpur"] = 0,
            ["Notun Bazar"] = 9.0,
            ["Badda"] = 10.0,
            ["Rampura"] = 11.0,
            ["Hatirjheel"] = 6.0,
            ["Mohanagar"] = 7.5
        },
        ["Notun Bazar"] = new()
        {
            ["Banani"] = 4.0,
            ["Mohakhali"] = 3.0,
            ["Gulshan 1"] = 3.5,
            ["Gulshan 2"] = 3.0,
            ["Tejgaon"] = 5.0,
            ["Farmgate"] = 6.0,
            ["Mirpur"] = 9.0,
            ["Notun Bazar"] = 0,
            ["Badda"] = 2.0,
            ["Rampura"] = 3.0,
            ["Hatirjheel"] = 4.0,
            ["Mohanagar"] = 5.5
        },
        ["Badda"] = new()
        {
            ["Banani"] = 5.5,
            ["Mohakhali"] = 4.0,
            ["Gulshan 1"] = 4.5,
            ["Gulshan 2"] = 3.5,
            ["Tejgaon"] = 6.0,
            ["Farmgate"] = 7.0,
            ["Mirpur"] = 10.0,
            ["Notun Bazar"] = 2.0,
            ["Badda"] = 0,
            ["Rampura"] = 2.0,
            ["Hatirjheel"] = 5.0,
            ["Mohanagar"] = 6.5
        },
        ["Rampura"] = new()
        {
            ["Banani"] = 6.5,
            ["Mohakhali"] = 5.0,
            ["Gulshan 1"] = 5.5,
            ["Gulshan 2"] = 4.5,
            ["Tejgaon"] = 7.0,
            ["Farmgate"] = 8.0,
            ["Mirpur"] = 11.0,
            ["Notun Bazar"] = 3.0,
            ["Badda"] = 2.0,
            ["Rampura"] = 0,
            ["Hatirjheel"] = 3.0,
            ["Mohanagar"] = 4.5
        },
        ["Hatirjheel"] = new()
        {
            ["Banani"] = 7.0,
            ["Mohakhali"] = 5.5,
            ["Gulshan 1"] = 6.0,
            ["Gulshan 2"] = 5.0,
            ["Tejgaon"] = 4.0,
            ["Farmgate"] = 3.0,
            ["Mirpur"] = 6.0,
            ["Notun Bazar"] = 4.0,
            ["Badda"] = 5.0,
            ["Rampura"] = 3.0,
            ["Hatirjheel"] = 0,
            ["Mohanagar"] = 2.0
        },
        ["Mohanagar"] = new()
        {
            ["Banani"] = 8.5,
            ["Mohakhali"] = 7.0,
            ["Gulshan 1"] = 7.5,
            ["Gulshan 2"] = 6.5,
            ["Tejgaon"] = 5.5,
            ["Farmgate"] = 4.5,
            ["Mirpur"] = 7.5,
            ["Notun Bazar"] = 5.5,
            ["Badda"] = 6.5,
            ["Rampura"] = 4.5,
            ["Hatirjheel"] = 2.0,
            ["Mohanagar"] = 0
        }
    };

    public static double GetDistance(string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return double.MaxValue;

        from = from.Trim();
        to = to.Trim();

        if (!DistanceMatrix.ContainsKey(from) || !DistanceMatrix[from].ContainsKey(to))
            return double.MaxValue;

        return DistanceMatrix[from][to];
    }

    public static double CalculateRouteDistance(string[] stops)
    {
        if (stops == null || stops.Length < 2)
            return 0;

        double totalDistance = 0;
        for (int i = 0; i < stops.Length - 1; i++)
        {
            totalDistance += GetDistance(stops[i], stops[i + 1]);
        }
        return totalDistance;
    }

    public static double CalculateDetourPercentage(
        string originalDestination,
        string[] additionalStops)
    {
        if (additionalStops == null || additionalStops.Length == 0)
            return 0;

        // Direct distance from pickup to original destination
        var directDistance = GetDistance(additionalStops[0], originalDestination);
        if (directDistance == 0 || directDistance == double.MaxValue)
            return 0;

        // Distance with additional stops
        var routeDistance = CalculateRouteDistance(additionalStops);

        // Calculate detour percentage
        var detour = routeDistance - directDistance;
        return (detour / directDistance) * 100;
    }
}
