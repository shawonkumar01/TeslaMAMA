namespace TeslaMAMA.Models;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalPassengers { get; set; }
    public int TotalDrivers { get; set; }
    public int TotalRides { get; set; }
    public int CompletedRides { get; set; }
    public int ActiveRides { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingRefundsCount { get; set; }
    public int OnlineDrivers { get; set; }
    public int TotalVehicles { get; set; }

    public List<AdminRideItemViewModel> RecentRides { get; set; } = new();
    public List<AdminRefundItemViewModel> PendingRefunds { get; set; } = new();
}

public class AdminRideItemViewModel
{
    public int Id { get; set; }
    public string? PassengerName { get; set; }
    public string? DriverName { get; set; }
    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
    public RideStatus Status { get; set; }
    public decimal Fare { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminRefundItemViewModel
{
    public int PaymentId { get; set; }
    public int RideId { get; set; }
    public string? PassengerName { get; set; }
    public decimal Amount { get; set; }
    public DateTime RequestDate { get; set; }
    public string? Notes { get; set; }
}

public class AdminUsersViewModel
{
    public List<AdminUserItemViewModel> Users { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string? RoleFilter { get; set; }
}

public class AdminUserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class AdminRidesViewModel
{
    public List<AdminRideItemViewModel> Rides { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? StatusFilter { get; set; }
}

public class AdminRideDetailsViewModel
{
    public int Id { get; set; }
    public string? PassengerName { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleName { get; set; }
    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
    public RideStatus Status { get; set; }
    public int Seats { get; set; }
    public decimal Fare { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RideStatusHistory> StatusHistory { get; set; } = new();
    public Payment? Payment { get; set; }
}

public class AdminPaymentsViewModel
{
    public List<AdminPaymentItemViewModel> Payments { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? StatusFilter { get; set; }
}

public class AdminPaymentItemViewModel
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public string? PassengerName { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public class AdminVehiclesViewModel
{
    public List<AdminVehicleItemViewModel> Vehicles { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class AdminVehicleItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public int Capacity { get; set; }
    public string CurrentZone { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}
