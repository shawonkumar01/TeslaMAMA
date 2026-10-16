using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;

namespace TeslaMAMA.Services;

public class RideOfferTimeoutService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RideOfferTimeoutService> _logger;

    public RideOfferTimeoutService(
        IServiceScopeFactory scopeFactory,
        ILogger<RideOfferTimeoutService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRidesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing expired ride offers.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingRidesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var assignmentService = scope.ServiceProvider
            .GetRequiredService<IDriverAssignmentService>();

        var now = DateTime.UtcNow;

        var expiredOffers = await dbContext.DriverOffers
            .Where(o =>
                o.Status == DriverOfferStatus.Pending &&
                o.ExpiresAt <= now &&
                o.RideRequest.Status == RideStatus.Requested)
            .ToListAsync(cancellationToken);

        foreach (var offer in expiredOffers)
        {
            offer.Status = DriverOfferStatus.Expired;
            offer.RespondedAt = now;
        }

        if (expiredOffers.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        var rideIds = await dbContext.RideRequests
            .Where(r =>
                r.Status == RideStatus.Requested &&
                !dbContext.DriverOffers.Any(o =>
                    o.RideRequestId == r.Id &&
                    o.Status == DriverOfferStatus.Pending &&
                    o.ExpiresAt > now))
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var rideId in rideIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await assignmentService.OfferRideAsync(
                    rideId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to assign a driver to ride {RideId}.",
                    rideId);
            }
        }
    }
}