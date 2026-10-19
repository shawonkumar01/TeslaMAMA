using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;

namespace TeslaMAMA.Services;

public class DriverAssignmentService : IDriverAssignmentService
{
    private readonly AppDbContext _dbContext;

    public DriverAssignmentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DriverOffer?> OfferRideAsync(
        int rideRequestId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var ride = await _dbContext.RideRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == rideRequestId,
                cancellationToken);

        if (ride is null || ride.Status != RideStatus.Requested)
            return null;

        var existingOffer = await _dbContext.DriverOffers
            .Include(o => o.Vehicle)
            .FirstOrDefaultAsync(
                o => o.RideRequestId == rideRequestId &&
                     o.Status == DriverOfferStatus.Pending &&
                     o.ExpiresAt > now,
                cancellationToken);

        if (existingOffer is not null)
            return existingOffer;

        var expiredOffers = await _dbContext.DriverOffers
            .Where(o =>
                o.RideRequestId == rideRequestId &&
                o.Status == DriverOfferStatus.Pending &&
                o.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var expiredOffer in expiredOffers)
        {
            expiredOffer.Status = DriverOfferStatus.Expired;
            expiredOffer.RespondedAt = now;
        }

        var rejectedVehicleIds = await _dbContext.PoolDeclines
            .Where(d => d.RideRequestId == rideRequestId)
            .Select(d => d.VehicleId)
            .ToListAsync(cancellationToken);

        var previouslyOfferedVehicleIds = await _dbContext.DriverOffers
            .Where(o => o.RideRequestId == rideRequestId)
            .Select(o => o.VehicleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var excludedVehicleIds = rejectedVehicleIds
            .Concat(previouslyOfferedVehicleIds)
            .Distinct()
            .ToList();

        var vehicles = await _dbContext.Vehicles
            .Include(v => v.Driver)
            .Where(v =>
                v.IsOnline &&
                v.CurrentZone == ride.PickupZone &&
                v.Capacity >= ride.Seats &&
                !excludedVehicleIds.Contains(v.Id) &&
                v.Driver.Role == UserRole.Driver)
            .OrderBy(v => v.Id)
            .ToListAsync(cancellationToken);

        foreach (var vehicle in vehicles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pools = await _dbContext.Pools
                .Include(p => p.RideRequests)
                .Where(p =>
                    p.VehicleId == vehicle.Id &&
                    (p.Status == PoolStatus.Open ||
                     p.Status == PoolStatus.Full ||
                     p.Status == PoolStatus.InProgress))
                .ToListAsync(cancellationToken);

            if (pools.Any(p => p.Status == PoolStatus.InProgress))
                continue;

            var activePools = pools
                .Where(p => p.RideRequests.Any(r =>
                    r.Status != RideStatus.Completed &&
                    r.Status != RideStatus.Cancelled))
                .ToList();

            // A pending offer reserves potential capacity until it expires,
            // is rejected, or is accepted.
            var pendingOffers = await _dbContext.DriverOffers
                .Include(o => o.RideRequest)
                .Where(o =>
                    o.VehicleId == vehicle.Id &&
                    o.Status == DriverOfferStatus.Pending &&
                    o.ExpiresAt > now &&
                    o.RideRequest.Status == RideStatus.Requested)
                .ToListAsync(cancellationToken);

            if (pendingOffers.Any(o =>
                o.RideRequest.RideType == RideType.Private))
            {
                continue;
            }

            if (ride.RideType == RideType.Private)
            {
                // A private ride must reserve an otherwise unused vehicle.
                if (activePools.Count > 0 || pendingOffers.Count > 0)
                    continue;
            }
            else
            {
                // A shared ride cannot join an active private ride.
                if (activePools.Any(p => p.RideRequests.Any(r =>
                    r.Status != RideStatus.Completed &&
                    r.Status != RideStatus.Cancelled &&
                    r.RideType == RideType.Private)))
                {
                    continue;
                }

                // If an active pool exists, it must be open and compatible.
                if (activePools.Count > 0)
                {
                    var compatiblePool = activePools.FirstOrDefault(p =>
                        p.Status == PoolStatus.Open &&
                        p.SeatsUsed + ride.Seats <= vehicle.Capacity &&
                        p.RideRequests
                            .Where(r =>
                                r.Status != RideStatus.Completed &&
                                r.Status != RideStatus.Cancelled)
                            .All(r =>
                                r.RideType == RideType.SharedPool &&
                                RouteCompatibilityService
                                    .AreRoutesCompatible(
                                        ride.PickupZone,
                                        r.DestinationZone,
                                        ride.DestinationZone)));

                    if (compatiblePool is null)
                        continue;

                    var otherPendingSeats = pendingOffers
                        .Where(o => o.RideRequestId != ride.Id)
                        .Sum(o => o.RideRequest.Seats);

                    if (compatiblePool.SeatsUsed +
                        otherPendingSeats +
                        ride.Seats > vehicle.Capacity)
                    {
                        continue;
                    }

                    // Pending offers for this vehicle must also be compatible
                    // with the active pool and the new request.
                    var pendingIsCompatible = pendingOffers
                        .Where(o => o.RideRequestId != ride.Id)
                        .All(o =>
                            o.RideRequest.RideType == RideType.SharedPool &&
                            RouteCompatibilityService
                                .AreRoutesCompatible(
                                    ride.PickupZone,
                                    o.RideRequest.DestinationZone,
                                    ride.DestinationZone) &&
                            compatiblePool.RideRequests
                                .Where(r =>
                                    r.Status != RideStatus.Completed &&
                                    r.Status != RideStatus.Cancelled)
                                .All(r =>
                                    RouteCompatibilityService
                                        .AreRoutesCompatible(
                                            ride.PickupZone,
                                            o.RideRequest.DestinationZone,
                                            r.DestinationZone)));

                    if (!pendingIsCompatible)
                        continue;
                }
                else
                {
                    // No pool has been created yet. Compatible pending offers
                    // can still be directed to this same vehicle.
                    if (pendingOffers.Any(o =>
                        o.RideRequest.RideType != RideType.SharedPool ||
                        !string.Equals(
                            o.RideRequest.PickupZone,
                            ride.PickupZone,
                            StringComparison.OrdinalIgnoreCase) ||
                        !RouteCompatibilityService.AreRoutesCompatible(
                            ride.PickupZone,
                            o.RideRequest.DestinationZone,
                            ride.DestinationZone)))
                    {
                        continue;
                    }

                    var pendingSeats = pendingOffers
                        .Where(o => o.RideRequestId != ride.Id)
                        .Sum(o => o.RideRequest.Seats);

                    if (pendingSeats + ride.Seats > vehicle.Capacity)
                        continue;
                }
            }

            var offer = new DriverOffer
            {
                RideRequestId = ride.Id,
                VehicleId = vehicle.Id,
                Status = DriverOfferStatus.Pending,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(90)
            };

            _dbContext.DriverOffers.Add(offer);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return offer;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }
}