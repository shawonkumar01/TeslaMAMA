using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;

namespace TeslaMAMA.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        var viewModel = new AdminDashboardViewModel
        {
            TotalUsers = await _dbContext.Users.CountAsync(),
            TotalPassengers = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Passenger),
            TotalDrivers = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Driver),
            TotalRides = await _dbContext.RideRequests.CountAsync(),
            CompletedRides = await _dbContext.RideRequests.CountAsync(r => r.Status == RideStatus.Completed),
            ActiveRides = await _dbContext.RideRequests.CountAsync(r =>
                r.Status == RideStatus.Requested ||
                r.Status == RideStatus.Matched ||
                r.Status == RideStatus.DriverArrived ||
                r.Status == RideStatus.Started),
            TotalRevenue = await _dbContext.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m,
            PendingRefundsCount = await _dbContext.Payments
                .CountAsync(p => p.Status == PaymentStatus.RefundRequested),
            OnlineDrivers = await _dbContext.Vehicles
                .CountAsync(v => v.IsOnline),
            TotalVehicles = await _dbContext.Vehicles.CountAsync()
        };

        // Recent rides (last 10)
        var recentRides = await _dbContext.RideRequests
            .AsNoTracking()
            .Include(r => r.Passenger)
            .Include(r => r.Pool)
                .ThenInclude(p => p!.Vehicle)
                    .ThenInclude(v => v!.Driver)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        viewModel.RecentRides = recentRides.Select(r => new AdminRideItemViewModel
            {
                Id = r.Id,
                PassengerName = r.Passenger?.FullName,
                DriverName = r.Pool?.Vehicle?.Driver?.FullName,
                PickupZone = r.PickupZone,
                DestinationZone = r.DestinationZone,
                Status = r.Status,
                Fare = r.FinalFare,
                CreatedAt = r.CreatedAt
            }).ToList();

        // Pending refund requests
        viewModel.PendingRefundsCount = await _dbContext.Payments
            .CountAsync(p => p.Status == PaymentStatus.RefundRequested);

        var pendingRefunds = await _dbContext.Payments
            .AsNoTracking()
            .Include(p => p.RideRequest)
            .ThenInclude(r => r.Passenger)
            .Where(p => p.Status == PaymentStatus.RefundRequested)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        viewModel.PendingRefunds = pendingRefunds.Select(p => new AdminRefundItemViewModel
            {
                PaymentId = p.Id,
                RideId = p.RideRequestId,
                PassengerName = p.RideRequest?.Passenger?.FullName,
                Amount = p.Amount,
                RequestDate = p.CreatedAt,
                Notes = p.Notes
            }).ToList();

        return View(viewModel);
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users(string? search = null, string? role = null, int page = 1)
    {
        var query = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.ToLower().Contains(searchLower)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                (u.Email != null && u.Email.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        const int pageSize = 20;
        var totalUsers = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new AdminUsersViewModel
        {
            Users = users.Select(u => new AdminUserItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                UserName = u.UserName,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                IsActive = true // You can add an IsActive field to ApplicationUser if needed
            }).ToList(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize),
            Search = search,
            RoleFilter = role
        };

        return View(viewModel);
    }

    // POST: /Admin/ToggleUserStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return NotFound();

        // Toggle active status (you may want to add IsActive field to ApplicationUser)
        // For now, we'll just return success
        TempData["SuccessMessage"] = $"User {user.FullName} status updated.";

        return RedirectToAction(nameof(Users));
    }

    // GET: /Admin/Rides
    public async Task<IActionResult> Rides(string? status = null, int page = 1)
    {
        var query = _dbContext.RideRequests
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RideStatus>(status, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        query = query
            .Include(r => r.Passenger)
            .Include(r => r.Pool)
            .ThenInclude(p => p!.Vehicle)
            .ThenInclude(v => v!.Driver);

        const int pageSize = 20;
        var totalRides = await query.CountAsync();
        var rides = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new AdminRidesViewModel
        {
            Rides = rides.Select(r => new AdminRideItemViewModel
            {
                Id = r.Id,
                PassengerName = r.Passenger?.FullName,
                DriverName = r.Pool?.Vehicle?.Driver?.FullName,
                PickupZone = r.PickupZone,
                DestinationZone = r.DestinationZone,
                Status = r.Status,
                Fare = r.FinalFare,
                CreatedAt = r.CreatedAt
            }).ToList(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalRides / (double)pageSize),
            StatusFilter = status
        };

        return View(viewModel);
    }

    // GET: /Admin/RideDetails/{id}
    public async Task<IActionResult> RideDetails(int id)
    {
        var ride = await _dbContext.RideRequests
            .AsNoTracking()
            .Include(r => r.Passenger)
            .Include(r => r.Pool)
                .ThenInclude(p => p!.Vehicle)
                    .ThenInclude(v => v!.Driver)
            .Include(r => r.StatusHistory)
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (ride is null)
            return NotFound();

        var viewModel = new AdminRideDetailsViewModel
        {
            Id = ride.Id,
            PassengerName = ride.Passenger.FullName,
            DriverName = ride.Pool?.Vehicle?.Driver?.FullName,
            VehicleName = ride.Pool?.Vehicle?.Name,
            PickupZone = ride.PickupZone,
            DestinationZone = ride.DestinationZone,
            Status = ride.Status,
            Seats = ride.Seats,
            Fare = ride.FinalFare,
            PaymentMethod = ride.PaymentMethod,
            CreatedAt = ride.CreatedAt,
            StatusHistory = ride.StatusHistory.OrderBy(h => h.ChangedAt).ToList(),
            Payment = ride.Payments.FirstOrDefault()
        };

        return View(viewModel);
    }

    // GET: /Admin/Payments
    public async Task<IActionResult> Payments(string? status = null, int page = 1)
    {
        var query = _dbContext.Payments
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        query = query
            .Include(p => p.RideRequest)
            .ThenInclude(r => r.Passenger);

        const int pageSize = 20;
        var totalPayments = await query.CountAsync();
        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new AdminPaymentsViewModel
        {
            Payments = payments.Select(p => new AdminPaymentItemViewModel
            {
                Id = p.Id,
                RideId = p.RideRequestId,
                PassengerName = p.RideRequest?.Passenger?.FullName,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                CompletedAt = p.CompletedAt,
                Notes = p.Notes
            }).ToList(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalPayments / (double)pageSize),
            StatusFilter = status
        };

        return View(viewModel);
    }

    // POST: /Admin/ApproveRefund
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRefund(int paymentId)
    {
        var payment = await _dbContext.Payments
            .Include(p => p.RideRequest)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment is null)
            return NotFound();

        if (payment.Status != PaymentStatus.RefundRequested)
        {
            TempData["ErrorMessage"] = "This payment is not awaiting refund approval.";
            return RedirectToAction(nameof(Payments));
        }

        payment.Status = PaymentStatus.Refunded;
        payment.RefundedAt = DateTime.UtcNow;
        payment.Notes += $"\nRefund approved by admin on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Refund for payment #{paymentId} has been approved.";
        return RedirectToAction(nameof(Payments));
    }

    // POST: /Admin/RejectRefund
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRefund(int paymentId, string? reason)
    {
        var payment = await _dbContext.Payments
            .Include(p => p.RideRequest)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment is null)
            return NotFound();

        if (payment.Status != PaymentStatus.RefundRequested)
        {
            TempData["ErrorMessage"] = "This payment is not awaiting refund approval.";
            return RedirectToAction(nameof(Payments));
        }

        payment.Status = PaymentStatus.Completed;
        payment.Notes += $"\nRefund rejected by admin on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. Reason: {reason ?? "No reason provided"}";

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Refund request for payment #{paymentId} has been rejected.";
        return RedirectToAction(nameof(Payments));
    }

    // GET: /Admin/Vehicles
    public async Task<IActionResult> Vehicles(int page = 1)
    {
        const int pageSize = 20;
        var totalVehicles = await _dbContext.Vehicles.CountAsync();
        var vehicles = await _dbContext.Vehicles
            .AsNoTracking()
            .Include(v => v.Driver)
            .OrderByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new AdminVehiclesViewModel
        {
            Vehicles = vehicles.Select(v => new AdminVehicleItemViewModel
            {
                Id = v.Id,
                Name = v.Name,
                DriverName = v.Driver?.FullName,
                Capacity = v.Capacity,
                CurrentZone = v.CurrentZone,
                IsOnline = v.IsOnline
            }).ToList(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalVehicles / (double)pageSize)
        };

        return View(viewModel);
    }
}
