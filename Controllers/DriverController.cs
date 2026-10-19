
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
                CreatedAt = r.CreatedAt,
                PoolSeatsUsed = r.Pool.SeatsUsed,
                VehicleCapacity = r.Pool.Vehicle.Capacity,
                TotalPassengersInPool = r.Pool.RideRequests.Count(ride =>
                    ride.Status != RideStatus.Completed &&
                    ride.Status != RideStatus.Cancelled),
                IsSharedPool = r.RideType == RideType.SharedPool
            })
            .ToListAsync(cancellationToken);

        // Group rides by pool for pool-level actions
        var pools = await _dbContext.Pools
            .AsNoTracking()
            .Include(p => p.RideRequests)
                .ThenInclude(r => r.Passenger)
            .Include(p => p.Vehicle)
            .Where(p => p.VehicleId == vehicle.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        viewModel.Pools = pools.Select(p => new DriverPoolItemViewModel
        {
            PoolId = p.Id,
            PoolStatus = p.Status,
            SeatsUsed = p.SeatsUsed,
            VehicleCapacity = p.Vehicle.Capacity,
            TotalPassengers = p.RideRequests.Count(r =>
                r.Status != RideStatus.Completed &&
                r.Status != RideStatus.Cancelled),
            IsSharedPool = p.RideRequests.Any(r => r.RideType == RideType.SharedPool),
            Rides = p.RideRequests.Select(r => new DriverRideItemViewModel
            {
                RideId = r.Id,
                PassengerName = r.Passenger.FullName,
                PickupZone = r.PickupZone,
                DestinationZone = r.DestinationZone,
                Seats = r.Seats,
                Fare = r.FinalFare,
                Status = r.Status,
                PaymentMethod = r.PaymentMethod,
                PoolId = p.Id,
                PoolStatus = p.Status,
                CreatedAt = r.CreatedAt,
                PoolSeatsUsed = p.SeatsUsed,
                VehicleCapacity = p.Vehicle.Capacity,
                TotalPassengersInPool = p.RideRequests.Count(ride =>
                    ride.Status != RideStatus.Completed &&
                    ride.Status != RideStatus.Cancelled),
                IsSharedPool = r.RideType == RideType.SharedPool
            }).ToList()
        }).ToList();

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
    // Revalidates vehicle eligibility and assigns the ride to a compatible pool.
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

        var pools = await _dbContext.Pools
            .Include(p => p.RideRequests)
            .Where(p =>
                p.VehicleId == vehicle.Id &&
                (p.Status == PoolStatus.Open ||
                 p.Status == PoolStatus.Full ||
                 p.Status == PoolStatus.InProgress))
            .ToListAsync(cancellationToken);

        // Never assign a new ride while this vehicle is on a trip.
        if (pools.Any(p => p.Status == PoolStatus.InProgress))
        {
            TempData["ErrorMessage"] =
                "Your vehicle already has a ride in progress.";

            await transaction.CommitAsync(cancellationToken);
            return RedirectToAction(nameof(Index));
        }

        var activePools = pools
            .Where(p => p.RideRequests.Any(r =>
                r.Status != RideStatus.Completed &&
                r.Status != RideStatus.Cancelled))
            .ToList();

        Pool? pool = null;

        if (ride.RideType == RideType.Private)
        {
            // A Private ride exclusively reserves the vehicle.
            if (activePools.Count > 0)
            {
                offer.Status = DriverOfferStatus.Expired;
                offer.RespondedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["ErrorMessage"] =
                    "This vehicle is already reserved. Please wait for another offer.";

                return RedirectToAction(nameof(Index));
            }

            pool = new Pool
            {
                VehicleId = vehicle.Id,
                Status = PoolStatus.Open,
                SeatsUsed = vehicle.Capacity,
                CreatedAt = now
            };

            _dbContext.Pools.Add(pool);
        }
        else
        {
            // Shared rides may join only a compatible, open Shared Pool.
            pool = activePools.FirstOrDefault(p =>
                p.Status == PoolStatus.Open &&
                p.SeatsUsed + ride.Seats <= vehicle.Capacity &&
                p.RideRequests.All(r =>
                    r.Status == RideStatus.Cancelled ||
                    r.Status == RideStatus.Completed ||
                    (r.RideType == RideType.SharedPool &&
                     string.Equals(
                         r.PickupZone,
                         ride.PickupZone,
                         StringComparison.OrdinalIgnoreCase) &&
                     RouteCompatibilityService.AreRoutesCompatible(
                        ride.PickupZone,
                        r.DestinationZone,
                        ride.DestinationZone))));

            if (activePools.Count > 0 && pool is null)
            {
                offer.Status = DriverOfferStatus.Expired;
                offer.RespondedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["ErrorMessage"] =
                    "This vehicle no longer has a compatible shared pool with enough seats.";

                return RedirectToAction(nameof(Index));
            }

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

            pool.SeatsUsed += ride.Seats;

            if (pool.SeatsUsed >= vehicle.Capacity)
                pool.Status = PoolStatus.Full;
        }

        var previousStatus = ride.Status;

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

        // Invalidate all other pending offers for this ride.
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

        TempData["SuccessMessage"] = ride.RideType == RideType.Private
            ? "Private ride accepted. The entire vehicle is reserved for this passenger."
            : "Shared Pool ride accepted successfully.";

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

    // Marks the driver as arrived at the pickup point for the entire pool.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPoolArrived(
        int poolId,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

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

        var pool = await _dbContext.Pools
            .Include(p => p.RideRequests)
            .FirstOrDefaultAsync(
                p => p.Id == poolId && p.VehicleId == vehicle.Id,
                cancellationToken);

        if (pool is null)
            return NotFound();

        if (pool.Status != PoolStatus.Open && pool.Status != PoolStatus.Full)
        {
            TempData["ErrorMessage"] =
                $"Pool is in {pool.Status} status. Cannot mark arrived.";

            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;

        // Update all matched rides in the pool to DriverArrived
        var matchedRides = pool.RideRequests
            .Where(r => r.Status == RideStatus.Matched)
            .ToList();

        foreach (var ride in matchedRides)
        {
            var previousStatus = ride.Status;
            ride.Status = RideStatus.DriverArrived;

            _dbContext.RideStatusHistory.Add(new RideStatusHistory
            {
                RideRequestId = ride.Id,
                FromStatus = previousStatus,
                ToStatus = RideStatus.DriverArrived,
                ChangedAt = now
            });
        }

        if (matchedRides.Any())
        {
            pool.Status = PoolStatus.InProgress;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["SuccessMessage"] =
            $"Pool #{poolId} marked as arrived. All passengers have been notified.";

        return RedirectToAction(nameof(Index));
    }

    // Starts the trip for the entire pool after all passengers are picked up.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartPoolTrip(
        int poolId,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

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

        var pool = await _dbContext.Pools
            .Include(p => p.RideRequests)
            .FirstOrDefaultAsync(
                p => p.Id == poolId && p.VehicleId == vehicle.Id,
                cancellationToken);

        if (pool is null)
            return NotFound();

        if (pool.Status != PoolStatus.InProgress)
        {
            TempData["ErrorMessage"] =
                $"Pool is in {pool.Status} status. Cannot start trip.";

            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;

        // Update all DriverArrived rides in the pool to Started
        var arrivedRides = pool.RideRequests
            .Where(r => r.Status == RideStatus.DriverArrived)
            .ToList();

        foreach (var ride in arrivedRides)
        {
            var previousStatus = ride.Status;
            ride.Status = RideStatus.Started;

            _dbContext.RideStatusHistory.Add(new RideStatusHistory
            {
                RideRequestId = ride.Id,
                FromStatus = previousStatus,
                ToStatus = RideStatus.Started,
                ChangedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["SuccessMessage"] =
            $"Pool #{poolId} trip started. Heading to destinations.";

        return RedirectToAction(nameof(Index));
    }

    // Drops off a single passenger at their destination (for shared pools).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DropOffPassenger(
        int rideId,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

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

        if (ride.Status != RideStatus.Started)
        {
            TempData["ErrorMessage"] =
                $"Ride is in {ride.Status} status. Cannot drop off passenger.";

            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;
        var previousStatus = ride.Status;

        ride.Status = RideStatus.WaitingForPayment;

        _dbContext.RideStatusHistory.Add(new RideStatusHistory
        {
            RideRequestId = ride.Id,
            FromStatus = previousStatus,
            ToStatus = RideStatus.WaitingForPayment,
            ChangedAt = now
        });

        // Update vehicle location to this passenger's destination
        vehicle.CurrentZone = ride.DestinationZone;

        // Check if all passengers in the pool have been dropped off
        var pool = ride.Pool;
        if (pool != null)
        {
            var hasActiveRides = await _dbContext.RideRequests
                .AnyAsync(
                    r => r.PoolId == pool.Id &&
                         r.Status != RideStatus.Completed &&
                         r.Status != RideStatus.Cancelled &&
                         r.Status != RideStatus.WaitingForPayment,
                    cancellationToken);

            if (!hasActiveRides)
            {
                // All passengers dropped off, complete the pool
                pool.Status = PoolStatus.Completed;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["SuccessMessage"] =
            $"Passenger dropped off at {ride.DestinationZone}. Waiting for payment.";

        return RedirectToAction(nameof(Index));
    }

    // Marks the destination reached for the entire pool (for private pools only).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReachPoolDestination(
        int poolId,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

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

        var pool = await _dbContext.Pools
            .Include(p => p.RideRequests)
            .FirstOrDefaultAsync(
                p => p.Id == poolId && p.VehicleId == vehicle.Id,
                cancellationToken);

        if (pool is null)
            return NotFound();

        if (pool.Status != PoolStatus.InProgress)
        {
            TempData["ErrorMessage"] =
                $"Pool is in {pool.Status} status. Cannot reach destination.";

            return RedirectToAction(nameof(Index));
        }

        // Only allow pool-level destination for private pools
        var isPrivatePool = pool.RideRequests.All(r => r.RideType == RideType.Private);
        if (!isPrivatePool)
        {
            TempData["ErrorMessage"] =
                "Use individual drop-off for shared pools with multiple destinations.";

            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;

        // Update all Started rides in the pool to WaitingForPayment
        var startedRides = pool.RideRequests
            .Where(r => r.Status == RideStatus.Started)
            .ToList();

        foreach (var ride in startedRides)
        {
            var previousStatus = ride.Status;
            ride.Status = RideStatus.WaitingForPayment;

            _dbContext.RideStatusHistory.Add(new RideStatusHistory
            {
                RideRequestId = ride.Id,
                FromStatus = previousStatus,
                ToStatus = RideStatus.WaitingForPayment,
                ChangedAt = now
            });

            // Update vehicle location to this ride's destination
            vehicle.CurrentZone = ride.DestinationZone;
        }

        pool.Status = PoolStatus.Completed;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["SuccessMessage"] =
            $"Pool #{poolId} destination reached. Waiting for passenger payments.";

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

    // Validates ride ownership and updates ride status and vehicle location.
    private async Task<IActionResult> ChangeRideStatusAsync(
        int rideId,
        RideStatus expectedStatus,
        RideStatus newStatus,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null || driver.Role != UserRole.Driver)
            return Forbid();

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
            // Keep the pool reserved until payment is confirmed
            // and all other rides in the pool are completed or cancelled.
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
