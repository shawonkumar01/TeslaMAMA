
using TeslaMAMA.Models;

namespace TeslaMAMA.Services;

public interface IDriverAssignmentService
{
    Task<DriverOffer?> OfferRideAsync(
        int rideRequestId,
        CancellationToken cancellationToken = default);
}
