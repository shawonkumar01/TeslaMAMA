using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;

namespace TeslaMAMA.Services;

public class PassengerOverviewService : IPassengerOverviewService
{
    private readonly AppDbContext _dbContext;

    public PassengerOverviewService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PassengerOverviewViewModel> GetOverviewAsync(string passengerId, string range)
    {
        // Validate and set range
        var validRanges = new[] { "30d", "6m", "12m", "all" };
        if (!validRanges.Contains(range))
        {
            range = "6m";
        }

        // Calculate date range
        DateTime? startDate = range switch
        {
            "30d" => DateTime.UtcNow.AddDays(-30),
            "6m" => DateTime.UtcNow.AddMonths(-6),
            "12m" => DateTime.UtcNow.AddMonths(-12),
            "all" => null,
            _ => DateTime.UtcNow.AddMonths(-6)
        };

        // Get timezone info
        TimeZoneInfo? timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");
        }
        catch
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
        }

        // Query rides with projection
        var ridesQuery = _dbContext.RideRequests
            .AsNoTracking()
            .Where(r => r.PassengerId == passengerId);

        if (startDate.HasValue)
        {
            ridesQuery = ridesQuery.Where(r => r.CreatedAt >= startDate.Value);
        }

        var rides = await ridesQuery
            .Select(r => new
            {
                r.Id,
                r.Status,
                r.FinalFare,
                r.CreatedAt,
                r.RideType,
                r.PaymentMethod,
                r.PickupZone,
                r.DestinationZone,
                r.Seats
            })
            .ToListAsync();

        // Calculate totals
        var totalRides = rides.Count;
        var completedRides = rides.Count(r => r.Status == RideStatus.Completed);
        var cancelledRides = rides.Count(r => r.Status == RideStatus.Cancelled);
        var noDriverRides = rides.Count(r => r.Status == RideStatus.NoDriverAvailable);
        var activeStatuses = new[] { RideStatus.Requested, RideStatus.Matched, RideStatus.DriverArrived, RideStatus.Started, RideStatus.WaitingForPayment };
        var activeRides = rides.Count(r => activeStatuses.Contains(r.Status));

        var totalSpent = rides.Where(r => r.Status == RideStatus.Completed).Sum(r => r.FinalFare);
        var averageFare = completedRides > 0 ? totalSpent / completedRides : 0m;
        var cancellationRate = totalRides > 0 ? (decimal)(cancelledRides + noDriverRides) / totalRides * 100 : 0m;
        var sharedPoolPercent = totalRides > 0 ? (decimal)rides.Count(r => r.RideType == RideType.SharedPool) / totalRides * 100 : 0m;
        var totalSeatsBooked = rides.Sum(r => r.Seats);

        // Get active ride for banner
        ActiveRideSummary? activeRide = null;
        var activeRideData = rides.FirstOrDefault(r => activeStatuses.Contains(r.Status));
        if (activeRideData != null)
        {
            activeRide = new ActiveRideSummary
            {
                Id = activeRideData.Id,
                PickupZone = activeRideData.PickupZone,
                DestinationZone = activeRideData.DestinationZone,
                Status = activeRideData.Status
            };
        }

        // Convert to local time for grouping
        var localRides = rides.Select(r => new
        {
            r.Status,
            r.FinalFare,
            r.CreatedAt,
            r.RideType,
            r.PaymentMethod,
            r.PickupZone,
            r.DestinationZone,
            LocalCreatedAt = timeZone is null ? r.CreatedAt : TimeZoneInfo.ConvertTimeFromUtc(r.CreatedAt, timeZone)
        }).ToList();

        // Monthly spending
        var monthlySpending = new List<MonthlySpendingItem>();
        if (range == "30d")
        {
            // Group by week for 30d range
            var weeks = localRides
                .Where(r => r.Status == RideStatus.Completed)
                .GroupBy(r => 
                {
                    var daysSinceEpoch = (r.LocalCreatedAt.Date - DateTime.MinValue.Date).Days;
                    var weekNumber = daysSinceEpoch / 7;
                    var weekStart = DateTime.MinValue.Date.AddDays(weekNumber * 7);
                    return weekStart.ToString("MMM d");
                })
                .Select(g => new MonthlySpendingItem
                {
                    Label = g.Key,
                    Amount = g.Sum(r => r.FinalFare),
                    Rides = g.Count()
                })
                .OrderBy(x => x.Label)
                .ToList();
            monthlySpending = weeks;
        }
        else
        {
            // Group by month
            var months = localRides
                .Where(r => r.Status == RideStatus.Completed)
                .GroupBy(r => new { r.LocalCreatedAt.Year, r.LocalCreatedAt.Month })
                .Select(g => new MonthlySpendingItem
                {
                    Label = g.First().LocalCreatedAt.ToString("MMM yyyy"),
                    Amount = g.Sum(r => r.FinalFare),
                    Rides = g.Count()
                })
                .OrderBy(x => x.Label)
                .ToList();

            // Fill in missing months
            if (startDate.HasValue)
            {
                var allMonths = new List<MonthlySpendingItem>();
                var current = startDate.Value;
                while (current <= DateTime.UtcNow)
                {
                    var monthLabel = current.ToString("MMM yyyy");
                    var existing = months.FirstOrDefault(m => m.Label == monthLabel);
                    allMonths.Add(existing ?? new MonthlySpendingItem { Label = monthLabel, Amount = 0, Rides = 0 });
                    current = current.AddMonths(1);
                }
                monthlySpending = allMonths;
            }
            else
            {
                monthlySpending = months;
            }
        }

        // Status counts
        var statusCounts = localRides
            .GroupBy(r => r.Status.ToString())
            .Select(g => new StatusCountItem
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Ride type counts
        var rideTypeCounts = new List<RideTypeCountItem>
        {
            new RideTypeCountItem { Label = "Shared Pool", Count = localRides.Count(r => r.RideType == RideType.SharedPool) },
            new RideTypeCountItem { Label = "Private", Count = localRides.Count(r => r.RideType == RideType.Private) }
        };

        // Top routes (exclude cancelled and no driver)
        var topRoutes = localRides
            .Where(r => r.Status != RideStatus.Cancelled && r.Status != RideStatus.NoDriverAvailable)
            .GroupBy(r => $"{r.PickupZone} → {r.DestinationZone}")
            .Select(g => new TopRouteItem
            {
                Label = g.Key,
                Count = g.Count(),
                TotalSpent = g.Sum(r => r.FinalFare)
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        // Payment method counts
        var paymentMethodCounts = localRides
            .GroupBy(r => r.PaymentMethod.ToString())
            .Select(g => new PaymentMethodCountItem
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Weekday counts
        var weekdayCounts = new List<WeekdayCountItem>();
        var weekdays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        foreach (var day in weekdays)
        {
            var count = localRides.Count(r => r.LocalCreatedAt.DayOfWeek.ToString() == day);
            weekdayCounts.Add(new WeekdayCountItem { Label = day.Substring(0, 3), Count = count });
        }

        // Recent rides (last 3 completed or cancelled rides)
        var recentRides = rides
            .Where(r => r.Status == RideStatus.Completed || r.Status == RideStatus.Cancelled || r.Status == RideStatus.NoDriverAvailable)
            .OrderByDescending(r => r.CreatedAt)
            .Take(3)
            .Select(r => new RecentRideItem
            {
                Id = r.Id,
                PickupZone = r.PickupZone,
                DestinationZone = r.DestinationZone,
                Status = r.Status == RideStatus.Completed ? "completed" : r.Status == RideStatus.Cancelled ? "cancelled" : "nodriveravailable",
                FinalFare = r.FinalFare,
                CreatedAt = r.CreatedAt,
                IsSharedPool = r.RideType == RideType.SharedPool
            })
            .ToList();

        // Set range label
        var rangeLabel = range switch
        {
            "30d" => "Last 30 days",
            "6m" => "Last 6 months",
            "12m" => "Last 12 months",
            "all" => "All time",
            _ => "Last 6 months"
        };

        return new PassengerOverviewViewModel
        {
            Range = range,
            RangeLabel = rangeLabel,
            TotalRides = totalRides,
            CompletedRides = completedRides,
            CancelledRides = cancelledRides,
            NoDriverRides = noDriverRides,
            ActiveRides = activeRides,
            TotalSpent = Math.Round(totalSpent, 2),
            AverageFare = Math.Round(averageFare, 2),
            CancellationRate = Math.Round(cancellationRate, 1),
            SharedPoolPercent = Math.Round(sharedPoolPercent, 1),
            TotalSeatsBooked = totalSeatsBooked,
            MonthlySpending = monthlySpending,
            StatusCounts = statusCounts,
            RideTypeCounts = rideTypeCounts,
            TopRoutes = topRoutes,
            PaymentMethodCounts = paymentMethodCounts,
            WeekdayCounts = weekdayCounts,
            RecentRides = recentRides,
            ActiveRide = activeRide
        };
    }
}
