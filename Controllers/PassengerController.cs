
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
    private readonly RoadDistanceStore _roadDistanceStore;

    public PassengerController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IFareCalculationService fareCalculator,
        IDriverAssignmentService assignmentService,
        RoadDistanceStore roadDistanceStore)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _fareCalculator = fareCalculator;
        _assignmentService = assignmentService;
        _roadDistanceStore = roadDistanceStore;
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
            .Where(ride => ride.PassengerId == passenger.Id);

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
                ride => activeStatuses.Contains(ride.Status)),

            CompletedRides = await passengerRides.CountAsync(
                ride => ride.Status == RideStatus.Completed),

            TotalSpent = await passengerRides
                .Where(ride => ride.Status == RideStatus.Completed)
                .SumAsync(ride => (decimal?)ride.FinalFare) ?? 0m
        };

        var recentRides = await passengerRides
            .Include(ride => ride.Pool)
                .ThenInclude(pool => pool!.Vehicle)
            .OrderByDescending(ride => ride.CreatedAt)
            .ThenByDescending(ride => ride.Id)
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
                VehicleName = ride.Pool?.Vehicle?.Name,
                PoolSeatsUsed = ride.Pool?.SeatsUsed,
                VehicleCapacity = ride.Pool?.Vehicle?.Capacity,
                OtherPassengersInPool = ride.Pool != null
                    ? ride.Pool.RideRequests.Count(r =>
                        r.Id != ride.Id &&
                        r.Status != RideStatus.Completed &&
                        r.Status != RideStatus.Cancelled)
                    : null,
                IsSharedPool = ride.RideType == RideType.SharedPool
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

        var model = new PassengerBookingViewModel
        {
            ZoneCoordinates = ZoneCoordinates.GetCoordinates()
        };
        PopulateZones(model);

        return View(model);
    }

    // GET: /Passenger/CheckDriverAvailability?zone=Notun Bazar
    [HttpGet]
    public async Task<IActionResult> CheckDriverAvailability(string zone)
    {
        if (string.IsNullOrWhiteSpace(zone))
            return Json(new { hasDrivers = false });

        var hasDrivers = await _dbContext.Vehicles
            .AsNoTracking()
            .AnyAsync(v =>
                v.CurrentZone == zone &&
                v.IsOnline &&
                v.Driver != null);

        return Json(new { hasDrivers });
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

        if (!Enum.IsDefined(model.RideType))
        {
            ModelState.AddModelError(
                nameof(model.RideType),
                "Select a valid ride type.");
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
        {
            model.ZoneCoordinates = ZoneCoordinates.GetCoordinates();
            return View(model);
        }

        if (!_fareCalculator.TryCalculateFare(
                model.PickupZone,
                model.DestinationZone,
                model.Seats,
                model.RideType,
                out var totalFare))
        {
            ModelState.AddModelError(
                string.Empty,
                "This route is not currently supported. Please select another route.");

            model.ZoneCoordinates = ZoneCoordinates.GetCoordinates();
            return View(model);
        }

        // Check if there are any online drivers in the pickup zone
        var hasOnlineDrivers = await _dbContext.Vehicles
            .AsNoTracking()
            .AnyAsync(v =>
                v.CurrentZone == model.PickupZone &&
                v.IsOnline &&
                v.Driver != null,
                cancellationToken);

        if (!hasOnlineDrivers)
        {
            ModelState.AddModelError(
                string.Empty,
                $"No drivers are currently online in {model.PickupZone}. Please try a different pickup zone or try again later.");

            model.ZoneCoordinates = ZoneCoordinates.GetCoordinates();
            return View(model);
        }

        // Check if passenger already has an active ride
        var activeStatuses = new[]
        {
            RideStatus.Requested,
            RideStatus.Matched,
            RideStatus.DriverArrived,
            RideStatus.Started
        };

        var hasActiveRide = await _dbContext.RideRequests
            .AsNoTracking()
            .AnyAsync(r =>
                r.PassengerId == passenger.Id &&
                activeStatuses.Contains(r.Status),
                cancellationToken);

        if (hasActiveRide)
        {
            ModelState.AddModelError(
                string.Empty,
                "You already have an active ride. Please cancel or complete your current ride before booking a new one.");

            model.ZoneCoordinates = ZoneCoordinates.GetCoordinates();
            return View(model);
        }

        var now = DateTime.UtcNow;

        var ride = new RideRequest
        {
            PassengerId = passenger.Id,
            PickupZone = model.PickupZone,
            DestinationZone = model.DestinationZone,
            Seats = model.Seats,
            RideType = model.RideType,
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

        // Save the booking before attempting driver assignment.
        await using (var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken))
        {
            _dbContext.RideRequests.Add(ride);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Driver assignment will be updated separately to enforce
        // the different rules for SharedPool and Private rides.
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
            // The booking is already saved; do not report it as failed.
            TempData["BookingSuccess"] =
                $"Ride request #{ride.Id} submitted. Driver assignment could not be completed right now. Estimated fare: ৳{totalFare:N2}.";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    // GET: /Passenger/FareEstimate
    // Returns the same server-calculated fare used during booking.
    [HttpGet]
    public async Task<IActionResult> FareEstimate(
        string pickupZone,
        string destinationZone,
        int seats,
        RideType rideType)
    {
        var passenger = await _userManager.GetUserAsync(User);

        if (passenger is null)
            return Challenge();

        if (passenger.Role != UserRole.Passenger)
            return Forbid();

        if (!Enum.IsDefined(rideType))
        {
            return BadRequest(new
            {
                message = "Select a valid ride type."
            });
        }

        if (!_fareCalculator.TryCalculateFare(
                pickupZone,
                destinationZone,
                seats,
                rideType,
                out var totalFare))
        {
            return BadRequest(new
            {
                message = "This route is not currently supported."
            });
        }

        var perSeatFare = decimal.Round(
            totalFare / seats,
            2,
            MidpointRounding.AwayFromZero);

        // Get the road distance for display consistency
        var distanceKm = GetDistanceKm(pickupZone, destinationZone);

        return Json(new
        {
            totalFare,
            perSeatFare,
            seats,
            rideType = rideType.ToString(),
            discountPercent = rideType == RideType.SharedPool
                ? FixedFareCalculationService.PoolDiscountPercent
                : 0m,
            distanceKm
        });
    }

    private void PopulateZones(PassengerBookingViewModel model)
    {
        model.SupportedZones = _fareCalculator.GetSupportedZones();
    }

    private double GetDistanceKm(string from, string to)
    {
        // Try precomputed road distance from database (loaded into memory store)
        if (_roadDistanceStore.TryGet(from, to, out var km))
        {
            return km;
        }

        // Fallback: straight-line distance scaled up to approximate roads
        var fromCoord = ZoneCoordinates.GetCoordinate(from);
        var toCoord = ZoneCoordinates.GetCoordinate(to);

        if (fromCoord == null || toCoord == null)
        {
            return 0;
        }

        var straightLineKm = ZoneCoordinates.CalculateDistance(
            fromCoord.Lat,
            fromCoord.Lng,
            toCoord.Lat,
            toCoord.Lng);

        // Roads are typically 1.2-1.5x longer than straight line
        return straightLineKm * 1.3;
    }

    // POST: /Passenger/CancelRide
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRide(
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
            .Include(request => request.Pool)
                .ThenInclude(pool => pool!.Vehicle)
            .FirstOrDefaultAsync(
                request =>
                    request.Id == rideId &&
                    request.PassengerId == passenger.Id,
                cancellationToken);

        if (ride is null)
            return NotFound();

        var cancellableStatuses = new[]
        {
            RideStatus.Requested,
            RideStatus.Matched,
            RideStatus.DriverArrived
        };

        if (!cancellableStatuses.Contains(ride.Status))
        {
            TempData["Error"] =
                "This ride cannot be cancelled at this stage.";

            return RedirectToAction(nameof(Dashboard));
        }

        var now = DateTime.UtcNow;
        var previousStatus = ride.Status;

        ride.Status = RideStatus.Cancelled;

        _dbContext.RideStatusHistory.Add(new RideStatusHistory
        {
            RideRequestId = ride.Id,
            FromStatus = previousStatus,
            ToStatus = RideStatus.Cancelled,
            ChangedAt = now
        });

        // Cancel all pending driver offers for this ride
        var pendingOffers = await _dbContext.DriverOffers
            .Where(o =>
                o.RideRequestId == ride.Id &&
                o.Status == DriverOfferStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var offer in pendingOffers)
        {
            offer.Status = DriverOfferStatus.Expired;
            offer.RespondedAt = now;
        }

        // If ride is in a pool, handle pool status
        if (ride.Pool is not null)
        {
            var pool = ride.Pool;

            // Remove seats used from the pool
            pool.SeatsUsed -= ride.Seats;

            if (pool.SeatsUsed < 0)
                pool.SeatsUsed = 0;

            // Check if there are other active rides in this pool
            var hasOtherActiveRides = await _dbContext.RideRequests
                .AnyAsync(
                    request =>
                        request.PoolId == pool.Id &&
                        request.Id != ride.Id &&
                        request.Status != RideStatus.Completed &&
                        request.Status != RideStatus.Cancelled,
                    cancellationToken);

            if (!hasOtherActiveRides)
            {
                // No other active rides, cancel the pool
                pool.Status = PoolStatus.Cancelled;
            }
            else if (pool.Status == PoolStatus.Full && pool.SeatsUsed < pool.Vehicle.Capacity)
            {
                // Pool was full but now has space
                pool.Status = PoolStatus.Open;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["BookingSuccess"] =
            $"Ride #{ride.Id} has been cancelled successfully.";

        return RedirectToAction(nameof(Dashboard));
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
            .Include(request => request.Pool)
            .FirstOrDefaultAsync(
                request =>
                    request.Id == rideId &&
                    request.PassengerId == passenger.Id,
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

        // Create payment record with Pending status first
        var payment = new Payment
        {
            RideRequestId = ride.Id,
            Amount = ride.FinalFare,
            Method = ride.PaymentMethod,
            Status = PaymentStatus.Pending,
            TransactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{ride.Id}",
            CreatedAt = now
        };

        _dbContext.Payments.Add(payment);

        try
        {
            // Simulate payment processing - in production, integrate with actual payment gateway
            await Task.Delay(100, cancellationToken);

            // Mark payment as completed
            payment.Status = PaymentStatus.Completed;
            payment.CompletedAt = now;

            ride.Status = RideStatus.Completed;

            _dbContext.RideStatusHistory.Add(new RideStatusHistory
            {
                RideRequestId = ride.Id,
                FromStatus = previousStatus,
                ToStatus = RideStatus.Completed,
                ChangedAt = now
            });

            // Complete the pool only when no other ride remains active.
            if (ride.Pool is not null)
            {
                var hasOtherActiveRides = await _dbContext.RideRequests
                    .AnyAsync(
                        request =>
                            request.PoolId == ride.PoolId &&
                            request.Id != ride.Id &&
                            request.Status != RideStatus.Completed &&
                            request.Status != RideStatus.Cancelled,
                        cancellationToken);

                if (!hasOtherActiveRides)
                {
                    ride.Pool.Status = PoolStatus.Completed;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            TempData["BookingSuccess"] =
                $"Payment confirmed for ride #{ride.Id}. Your ride is complete.";

            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception)
        {
            // Mark payment as failed
            payment.Status = PaymentStatus.Failed;
            payment.Notes = "Payment processing failed";

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            TempData["Error"] =
                $"Payment processing failed for ride #{ride.Id}. Please try again.";

            return RedirectToAction(nameof(Dashboard));
        }
    }

    // POST: /Passenger/RefundPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefundPayment(
        int paymentId,
        string refundReason,
        CancellationToken cancellationToken)
    {
        var passenger = await _userManager.GetUserAsync(User);

        if (passenger is null)
            return Challenge();

        if (passenger.Role != UserRole.Passenger)
            return Forbid();

        if (string.IsNullOrWhiteSpace(refundReason))
        {
            TempData["Error"] = "Please provide a reason for the refund.";
            return RedirectToAction(nameof(PaymentHistory));
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var payment = await _dbContext.Payments
            .Include(p => p.RideRequest)
            .FirstOrDefaultAsync(
                p => p.Id == paymentId &&
                     p.RideRequest.PassengerId == passenger.Id,
                cancellationToken);

        if (payment is null)
            return NotFound();

        if (payment.Status != PaymentStatus.Completed)
        {
            TempData["Error"] =
                "Only completed payments can be refunded.";

            return RedirectToAction(nameof(PaymentHistory));
        }

        // Set status to RefundRequested - admin will approve/reject later
        payment.Status = PaymentStatus.RefundRequested;
        payment.Notes = $"Refund requested on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. Reason: {refundReason}";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["BookingSuccess"] =
            $"Refund request for payment #{payment.Id} has been submitted. Admin will review it shortly.";

        return RedirectToAction(nameof(PaymentHistory));
    }

    // GET: /Passenger/PaymentHistory
    [HttpGet]
    public async Task<IActionResult> PaymentHistory(CancellationToken cancellationToken)
    {
        var passenger = await _userManager.GetUserAsync(User);

        if (passenger is null)
            return Challenge();

        if (passenger.Role != UserRole.Passenger)
            return Forbid();

        var payments = await _dbContext.Payments
            .AsNoTracking()
            .Include(p => p.RideRequest)
            .Where(p => p.RideRequest.PassengerId == passenger.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentRecord
            {
                Id = p.Id,
                RideId = p.RideRequestId,
                PickupZone = p.RideRequest.PickupZone,
                DestinationZone = p.RideRequest.DestinationZone,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                TransactionId = p.TransactionId,
                CreatedAt = p.CreatedAt,
                CompletedAt = p.CompletedAt,
                RefundedAt = p.RefundedAt
            })
            .ToListAsync(cancellationToken);

        var viewModel = new PaymentHistoryViewModel
        {
            FullName = string.IsNullOrWhiteSpace(passenger.FullName)
                ? passenger.UserName ?? "Passenger"
                : passenger.FullName,
            Payments = payments,
            TotalSpent = payments.Where(p => p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Refunded).Sum(p => p.Amount)
        };

        return View(viewModel);
    }

}
