
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

        // Reuse an existing, valid pending offer instead of creating another.
        var pendingOffer = await _dbContext.DriverOffers
            .Include(o => o.Vehicle)
            .FirstOrDefaultAsync(
                o => o.RideRequestId == rideRequestId &&
                     o.Status == DriverOfferStatus.Pending &&
                     o.ExpiresAt > now,
                cancellationToken);

        if (pendingOffer is not null)
            return pendingOffer;

        // Mark expired offers so they can no longer be accepted.
        var expiredOffers = await _dbContext.DriverOffers
            .Where(o => o.RideRequestId == rideRequestId &&
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

        var alreadyOfferedVehicleIds = await _dbContext.DriverOffers
            .Where(o => o.RideRequestId == rideRequestId)
            .Select(o => o.VehicleId)
            .ToListAsync(cancellationToken);

        var excludedVehicleIds = rejectedVehicleIds
            .Concat(alreadyOfferedVehicleIds)
            .Distinct()
            .ToList();

        var vehicle = await _dbContext.Vehicles
            .Where(v =>
                v.IsOnline &&
                v.CurrentZone == ride.PickupZone &&
                v.Capacity >= ride.Seats &&
                !excludedVehicleIds.Contains(v.Id) &&
                v.Driver.Role == UserRole.Driver)
            .Where(v =>
                !_dbContext.Pools.Any(p =>
                    p.VehicleId == v.Id &&
                    (p.Status == PoolStatus.InProgress)))
            .Where(v =>
                !_dbContext.Pools
                    .Where(p => p.VehicleId == v.Id &&
                                p.Status == PoolStatus.Open)
                    .Any(p =>
                        p.SeatsUsed + ride.Seats > v.Capacity))
            .OrderBy(v => v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (vehicle is null)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        var offer = new DriverOffer
        {
            RideRequestId = ride.Id,
            VehicleId = vehicle.Id,
            Status = DriverOfferStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(30)
        };

        _dbContext.DriverOffers.Add(offer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return offer;
    }
}
