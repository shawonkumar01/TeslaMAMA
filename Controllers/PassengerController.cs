
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;
using TeslaMAMA.Services;

namespace TeslaMAMA.Controllers;

[Authorize]
public class PassengerController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFareCalculationService _fareCalculator;
    private readonly IDriverAssignmentService _assignmentService;

    public PassengerController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IFareCalculationService fareCalculator,
        IDriverAssignmentService assignmentService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _fareCalculator = fareCalculator;
        _assignmentService = assignmentService;
    }

    // GET: /Passenger/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var passenger = await _userManager.GetUserAsync(User);

        if (passenger is null)
            return Challenge();

        if (passenger.Role != UserRole.Passenger)
            return Forbid();

        var passengerRides = _dbContext.RideRequests
            .AsNoTracking()
            .Where(r => r.PassengerId == passenger.Id);

        var activeStatuses = new[]
        {
            RideStatus.Requested,
            RideStatus.Matched,
            RideStatus.DriverArrived,
            RideStatus.Started,
            RideStatus.WaitingForPayment
        };

        var viewModel = new PassengerDashboardViewModel
        {
            FullName = string.IsNullOrWhiteSpace(passenger.FullName)
                ? passenger.UserName ?? "Passenger"
                : passenger.FullName,

            TotalRides = await passengerRides.CountAsync(),

            ActiveRides = await passengerRides.CountAsync(
                r => activeStatuses.Contains(r.Status)),

            CompletedRides = await passengerRides.CountAsync(
                r => r.Status == RideStatus.Completed),

            TotalSpent = await passengerRides
                .Where(r => r.Status == RideStatus.Completed)
                .SumAsync(r => (decimal?)r.FinalFare) ?? 0m
        };

        var recentRides = await passengerRides
            .Include(r => r.Pool)
                .ThenInclude(p => p!.Vehicle)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(5)
            .ToListAsync();

        viewModel.RecentRides = recentRides
            .Select(ride => new PassengerRideItemViewModel
            {
                Id = ride.Id,
                PickupZone = ride.PickupZone,
                DestinationZone = ride.DestinationZone,
                Seats = ride.Seats,
                Status = ride.Status,
                PaymentMethod = ride.PaymentMethod,
                FinalFare = ride.FinalFare,
                CreatedAt = ride.CreatedAt,
                PoolId = ride.PoolId,
                PoolStatus = ride.Pool?.Status,
                VehicleName = ride.Pool?.Vehicle?.Name
            })
            .ToList();

        return View(viewModel);
    }

    // GET: /Passenger/Book
    [HttpGet]
    public async Task<IActionResult> Book()
    {
        var passenger = await _userManager.GetUserAsync(User);

        if (passenger is null)
            return Challenge();

        if (passenger.Role != UserRole.Passenger)
            return Forbid();

        var model = new PassengerBookingViewModel();
        PopulateZones(model);

        return View(model);
    }

    // POST: /Passenger/Book
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(
        PassengerBookingViewModel model,
        CancellationToken cancellationToken)
    {
        var passenger = await _userManager.GetUserAsync(User);

        if (passenger is null)
            return Challenge();

        if (passenger.Role != UserRole.Passenger)
            return Forbid();

        PopulateZones(model);

        if (!Enum.IsDefined(model.PaymentMethod))
        {
            ModelState.AddModelError(
                nameof(model.PaymentMethod),
                "Select a valid payment method.");
        }

        if (string.Equals(
                model.PickupZone,
                model.DestinationZone,
                StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(model.PickupZone))
        {
            ModelState.AddModelError(
                nameof(model.DestinationZone),
                "Pickup and destination must be different.");
        }

        if (!ModelState.IsValid)
            return View(model);

        if (!_fareCalculator.TryCalculateFare(
                model.PickupZone,
                model.DestinationZone,
                model.Seats,
                out var totalFare))
        {
            ModelState.AddModelError(
                string.Empty,
                "This route is not currently supported. Please select another route.");

            return View(model);
        }

        var now = DateTime.UtcNow;

        var ride = new RideRequest
        {
            PassengerId = passenger.Id,
            PickupZone = model.PickupZone,
            DestinationZone = model.DestinationZone,
            Seats = model.Seats,
            PaymentMethod = model.PaymentMethod,
            FinalFare = totalFare,
            Status = RideStatus.Requested,
            PoolId = null,
            CreatedAt = now
        };

        ride.StatusHistory.Add(new RideStatusHistory
        {
            FromStatus = null,
            ToStatus = RideStatus.Requested,
            ChangedAt = now
        });

        // Save the passenger's booking first.
        // Driver assignment runs only after this transaction commits.
        await using (var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken))
        {
            _dbContext.RideRequests.Add(ride);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Try to offer the ride to an eligible driver.
        // If no driver is available, keep the booking as Requested.
        try
        {
            var offer = await _assignmentService.OfferRideAsync(
                ride.Id,
                cancellationToken);

            TempData["BookingSuccess"] = offer is not null
                ? $"Ride request #{ride.Id} submitted. A driver offer has been created. Estimated fare: ৳{totalFare:N2}."
                : $"Ride request #{ride.Id} submitted. No eligible driver is available right now. Estimated fare: ৳{totalFare:N2}.";
        }
        catch (Exception)
        {
            // The booking is already committed, so do not tell the
            // passenger that their ride booking failed.
            TempData["BookingSuccess"] =
                $"Ride request #{ride.Id} submitted. Driver assignment could not be completed right now. Estimated fare: ৳{totalFare:N2}.";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    private void PopulateZones(PassengerBookingViewModel model)
    {
        model.SupportedZones = _fareCalculator.GetSupportedZones();
    }
  
// POST: /Passenger/ConfirmPayment
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmPayment(
    int rideId,
    CancellationToken cancellationToken)
    {
        var passenger = await _userManager.GetUserAsync(User);

        if (passenger is null)
            return Challenge();

        if (passenger.Role != UserRole.Passenger)
            return Forbid();

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var ride = await _dbContext.RideRequests
            .Include(r => r.Pool)
            .FirstOrDefaultAsync(
                r => r.Id == rideId &&
                     r.PassengerId == passenger.Id,
                cancellationToken);

        if (ride is null)
            return NotFound();

        if (ride.Status != RideStatus.WaitingForPayment)
        {
            TempData["Error"] =
                "Payment confirmation is available only after the destination is reached.";

            return RedirectToAction(nameof(Dashboard));
        }

        var now = DateTime.UtcNow;
        var previousStatus = ride.Status;

        ride.Status = RideStatus.Completed;

        _dbContext.RideStatusHistory.Add(new RideStatusHistory
        {
            RideRequestId = ride.Id,
            FromStatus = previousStatus,
            ToStatus = RideStatus.Completed,
            ChangedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["BookingSuccess"] =
            $"Payment confirmed for ride #{ride.Id}. Your ride is complete.";

        return RedirectToAction(nameof(Dashboard));
    }
}