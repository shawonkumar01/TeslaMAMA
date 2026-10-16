using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;
using TeslaMAMA.Services;

namespace TeslaMAMA.Controllers;

[Authorize]
public class DriverController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFareCalculationService _fareCalculator;
    private readonly IDriverAssignmentService _assignmentService;

    public DriverController(
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

    // GET: /Driver
    // Displays vehicle details, location options, offers, and assigned rides.
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.DriverId == driver.Id,
                cancellationToken);

        var viewModel = new DriverDashboardViewModel
        {
            DriverName = driver.FullName,
            SupportedZones = _fareCalculator
                .GetSupportedZones()
                .ToList()
        };

        if (vehicle is null)
            return View(viewModel);

        viewModel.VehicleName = vehicle.Name;
        viewModel.CurrentZone = vehicle.CurrentZone;
        viewModel.IsOnline = vehicle.IsOnline;

        var now = DateTime.UtcNow;

        // Only show valid offers while the vehicle is online.
        if (vehicle.IsOnline)
        {
            viewModel.PendingOffers = await _dbContext.DriverOffers
                .AsNoTracking()
                .Where(o =>
                    o.VehicleId == vehicle.Id &&
                    o.Status == DriverOfferStatus.Pending &&
                    o.ExpiresAt > now &&
                    o.RideRequest.Status == RideStatus.Requested)
                .OrderBy(o => o.ExpiresAt)
                .Select(o => new DriverOfferItemViewModel
                {
                    OfferId = o.Id,
                    RideId = o.RideRequestId,
                    PickupZone = o.RideRequest.PickupZone,
                    DestinationZone = o.RideRequest.DestinationZone,
                    Seats = o.RideRequest.Seats,
                    Fare = o.RideRequest.FinalFare,
                    PaymentMethod = o.RideRequest.PaymentMethod,
                    ExpiresAt = o.ExpiresAt
                })
                .ToListAsync(cancellationToken);
        }

        // Assigned rides are determined by this vehicle's pools.
        var ridesQuery = _dbContext.RideRequests
            .AsNoTracking()
            .Where(r =>
                r.PoolId != null &&
                r.Pool != null &&
                r.Pool.VehicleId == vehicle.Id);

        viewModel.TotalAssignedRides =
            await ridesQuery.CountAsync(cancellationToken);

        viewModel.ActiveRides = await ridesQuery.CountAsync(
            r => r.Status != RideStatus.Completed &&
                 r.Status != RideStatus.Cancelled,
            cancellationToken);

        viewModel.CompletedRides = await ridesQuery.CountAsync(
            r => r.Status == RideStatus.Completed,
            cancellationToken);

        viewModel.Rides = await ridesQuery
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new DriverRideItemViewModel
            {
                RideId = r.Id,
                PassengerName = r.Passenger.FullName,
                PickupZone = r.PickupZone,
                DestinationZone = r.DestinationZone,
                Seats = r.Seats,
                Fare = r.FinalFare,
                Status = r.Status,
                PaymentMethod = r.PaymentMethod,
                PoolId = r.PoolId!.Value,
                PoolStatus = r.Pool!.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return View(viewModel);
    }

    // POST: /Driver/SetAvailability
    // Requires the driver to select a valid zone before going online.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAvailability(
        bool isOnline,
        string? currentZone,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null)
            return Challenge();

        if (driver.Role != UserRole.Driver)
            return Forbid();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(
                v => v.DriverId == driver.Id,
                cancellationToken);

        if (vehicle is null)
        {
            TempData["ErrorMessage"] =
                "Register a vehicle before changing your availability.";

            return RedirectToAction(nameof(Index));
        }

        var hasActiveRide = await _dbContext.RideRequests
            .AnyAsync(
                r => r.Pool != null &&
                     r.Pool.VehicleId == vehicle.Id &&
                     (r.Status == RideStatus.Matched ||
                      r.Status == RideStatus.DriverArrived ||
                      r.Status == RideStatus.Started ||
                      r.Status == RideStatus.WaitingForPayment),
                cancellationToken);

        if (hasActiveRide)
        {
            TempData["ErrorMessage"] =
                "You cannot change your availability while you have an active ride.";

            return RedirectToAction(nameof(Index));
        }

        if (isOnline)
        {
            if (string.IsNullOrWhiteSpace(currentZone))
            {
                TempData["ErrorMessage"] =
                    "Select your current operating location before going online.";

                return RedirectToAction(nameof(Index));
            }

            var supportedZones = _fareCalculator
                .GetSupportedZones()
                .ToList();

            var selectedZone = supportedZones.FirstOrDefault(
                zone => string.Equals(
                    zone,
                    currentZone.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (selectedZone is null)
            {
                TempData["ErrorMessage"] =
                    "Please select a valid operating location.";

                return RedirectToAction(nameof(Index));
            }

            // Update the vehicle's actual operating zone before going online.
            vehicle.CurrentZone = selectedZone;
        }

        vehicle.IsOnline = isOnline;

        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = isOnline
            ? $"You are now online in {vehicle.CurrentZone}."
            : "You are now offline.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Driver/AcceptOffer
    // Accepts a valid offer and assigns the ride to a compatible pool.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptOffer(
        int offerId,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var offer = await _dbContext.DriverOffers
            .Include(o => o.RideRequest)
            .Include(o => o.Vehicle)
            .FirstOrDefaultAsync(
                o => o.Id == offerId &&
                     o.Vehicle.DriverId == driver.Id,
                cancellationToken);

        if (offer is null)
            return NotFound();

        var now = DateTime.UtcNow;
        var ride = offer.RideRequest;
        var vehicle = offer.Vehicle;

        if (offer.Status != DriverOfferStatus.Pending ||
            offer.ExpiresAt <= now ||
            ride.Status != RideStatus.Requested)
        {
            if (offer.Status == DriverOfferStatus.Pending)
            {
                offer.Status = DriverOfferStatus.Expired;
                offer.RespondedAt = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            TempData["ErrorMessage"] =
                "This offer is no longer available.";

            return RedirectToAction(nameof(Index));
        }

        if (!vehicle.IsOnline ||
            vehicle.Capacity < ride.Seats ||
            !string.Equals(
                vehicle.CurrentZone,
                ride.PickupZone,
                StringComparison.OrdinalIgnoreCase))
        {
            offer.Status = DriverOfferStatus.Expired;
            offer.RespondedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            TempData["ErrorMessage"] =
                "Your vehicle is offline, in another zone, or lacks capacity.";

            return RedirectToAction(nameof(Index));
        }

        var hasRideInProgress = await _dbContext.Pools
            .AnyAsync(
                p => p.VehicleId == vehicle.Id &&
                     p.Status == PoolStatus.InProgress,
                cancellationToken);

        if (hasRideInProgress)
        {
            TempData["ErrorMessage"] =
                "Your vehicle already has a ride in progress.";

            return RedirectToAction(nameof(Index));
        }

        var openPools = await _dbContext.Pools
            .Include(p => p.RideRequests)
            .Where(p =>
                p.VehicleId == vehicle.Id &&
                p.Status == PoolStatus.Open)
            .ToListAsync(cancellationToken);

        var pool = openPools.FirstOrDefault(p =>
            p.SeatsUsed + ride.Seats <= vehicle.Capacity &&
            p.RideRequests.All(r =>
                r.Status == RideStatus.Cancelled ||
                (r.PickupZone == ride.PickupZone &&
                 r.DestinationZone == ride.DestinationZone)));

        if (pool is null)
        {
            pool = new Pool
            {
                VehicleId = vehicle.Id,
                Status = PoolStatus.Open,
                SeatsUsed = 0,
                CreatedAt = now
            };

            _dbContext.Pools.Add(pool);
        }

        var previousStatus = ride.Status;

        pool.SeatsUsed += ride.Seats;

        if (pool.SeatsUsed >= vehicle.Capacity)
            pool.Status = PoolStatus.Full;

        ride.Pool = pool;
        ride.Status = RideStatus.Matched;

        offer.Status = DriverOfferStatus.Accepted;
        offer.RespondedAt = now;

        _dbContext.RideStatusHistory.Add(new RideStatusHistory
        {
            RideRequestId = ride.Id,
            FromStatus = previousStatus,
            ToStatus = RideStatus.Matched,
            ChangedAt = now
        });

        // Invalidate any other pending offers for this ride.
        var otherOffers = await _dbContext.DriverOffers
            .Where(o =>
                o.RideRequestId == ride.Id &&
                o.Id != offer.Id &&
                o.Status == DriverOfferStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var otherOffer in otherOffers)
        {
            otherOffer.Status = DriverOfferStatus.Expired;
            otherOffer.RespondedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["SuccessMessage"] = "Ride accepted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Driver/RejectOffer
    // Records rejection without cancelling the passenger's ride.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOffer(
        int offerId,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

        int rideId;
        var now = DateTime.UtcNow;

        await using (var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken))
        {
            var offer = await _dbContext.DriverOffers
                .Include(o => o.RideRequest)
                .Include(o => o.Vehicle)
                .FirstOrDefaultAsync(
                    o => o.Id == offerId &&
                         o.Vehicle.DriverId == driver.Id,
                    cancellationToken);

            if (offer is null)
                return NotFound();

            if (offer.Status != DriverOfferStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "This offer has already been processed.";

                return RedirectToAction(nameof(Index));
            }

            if (offer.ExpiresAt <= now ||
                offer.RideRequest.Status != RideStatus.Requested)
            {
                offer.Status = DriverOfferStatus.Expired;
                offer.RespondedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["ErrorMessage"] = "This offer has expired.";
                return RedirectToAction(nameof(Index));
            }

            rideId = offer.RideRequestId;

            offer.Status = DriverOfferStatus.Rejected;
            offer.RespondedAt = now;

            var declineExists = await _dbContext.PoolDeclines
                .AnyAsync(
                    d => d.RideRequestId == rideId &&
                         d.VehicleId == offer.VehicleId,
                    cancellationToken);

            if (!declineExists)
            {
                _dbContext.PoolDeclines.Add(new PoolDecline
                {
                    RideRequestId = rideId,
                    VehicleId = offer.VehicleId,
                    CreatedAt = now
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        var nextOffer = await _assignmentService.OfferRideAsync(
            rideId,
            cancellationToken);

        TempData["SuccessMessage"] = nextOffer is null
            ? "Offer rejected. No other eligible driver is currently available."
            : "Offer rejected. The ride has been offered to another driver.";

        return RedirectToAction(nameof(Index));
    }

    // Marks the driver as arrived at the pickup point.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> MarkArrived(
        int rideId,
        CancellationToken cancellationToken)
    {
        return ChangeRideStatusAsync(
            rideId,
            RideStatus.Matched,
            RideStatus.DriverArrived,
            cancellationToken);
    }

    // Starts the trip after the driver has arrived.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> StartTrip(
        int rideId,
        CancellationToken cancellationToken)
    {
        return ChangeRideStatusAsync(
            rideId,
            RideStatus.DriverArrived,
            RideStatus.Started,
            cancellationToken);
    }

    // Marks the destination reached and moves the ride to payment.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ReachDestination(
        int rideId,
        CancellationToken cancellationToken)
    {
        return ChangeRideStatusAsync(
            rideId,
            RideStatus.Started,
            RideStatus.WaitingForPayment,
            cancellationToken);
    }

    // Validates ride ownership and updates ride, pool, and vehicle location.
    private async Task<IActionResult> ChangeRideStatusAsync(
        int rideId,
        RideStatus expectedStatus,
        RideStatus newStatus,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

        // Load the tracked vehicle so its location can be updated and saved.
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(
                v => v.DriverId == driver.Id,
                cancellationToken);

        if (vehicle is null)
        {
            TempData["ErrorMessage"] =
                "No vehicle is assigned to your account.";

            return RedirectToAction(nameof(Index));
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var ride = await _dbContext.RideRequests
            .Include(r => r.Pool)
            .FirstOrDefaultAsync(
                r => r.Id == rideId &&
                     r.Pool != null &&
                     r.Pool.VehicleId == vehicle.Id,
                cancellationToken);

        if (ride is null)
            return NotFound();

        if (ride.Status != expectedStatus)
        {
            TempData["ErrorMessage"] =
                $"This action is not allowed for a ride in status {ride.Status}.";

            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;
        var previousStatus = ride.Status;

        ride.Status = newStatus;

        _dbContext.RideStatusHistory.Add(new RideStatusHistory
        {
            RideRequestId = ride.Id,
            FromStatus = previousStatus,
            ToStatus = newStatus,
            ChangedAt = now
        });

        if (newStatus == RideStatus.Started && ride.Pool is not null)
        {
            ride.Pool.Status = PoolStatus.InProgress;
        }

        if (newStatus == RideStatus.WaitingForPayment)
        {
            if (ride.Pool is not null)
                ride.Pool.Status = PoolStatus.Completed;

            // The driver has physically reached the destination.
            // Update the tracked vehicle so the next online location is accurate.
            vehicle.CurrentZone = ride.DestinationZone;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["SuccessMessage"] = newStatus switch
        {
            RideStatus.DriverArrived => "You are marked as arrived.",
            RideStatus.Started => "Trip started successfully.",
            RideStatus.WaitingForPayment =>
                $"Destination reached. Your location is now {vehicle.CurrentZone}. Waiting for passenger payment.",
            _ => "Ride status updated."
        };

        return RedirectToAction(nameof(Index));
    }
}