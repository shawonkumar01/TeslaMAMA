namespace TeslaMAMA.Models;

public class PassengerOverviewViewModel
{
    public string Range { get; set; } = "6m";
    public string RangeLabel { get; set; } = "Last 6 months";

    public int TotalRides { get; set; }
    public int CompletedRides { get; set; }
    public int CancelledRides { get; set; }
    public int NoDriverRides { get; set; }
    public int ActiveRides { get; set; }

    public decimal TotalSpent { get; set; }
    public decimal AverageFare { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal SharedPoolPercent { get; set; }
    public int TotalSeatsBooked { get; set; }

    public List<MonthlySpendingItem> MonthlySpending { get; set; } = new();
    public List<StatusCountItem> StatusCounts { get; set; } = new();
    public List<RideTypeCountItem> RideTypeCounts { get; set; } = new();
    public List<TopRouteItem> TopRoutes { get; set; } = new();
    public List<PaymentMethodCountItem> PaymentMethodCounts { get; set; } = new();
    public List<WeekdayCountItem> WeekdayCounts { get; set; } = new();

    public ActiveRideSummary? ActiveRide { get; set; }
}

public class MonthlySpendingItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Rides { get; set; }
}

public class StatusCountItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RideTypeCountItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopRouteItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalSpent { get; set; }
}

public class PaymentMethodCountItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class WeekdayCountItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ActiveRideSummary
{
    public int Id { get; set; }
    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
    public RideStatus Status { get; set; }
}
