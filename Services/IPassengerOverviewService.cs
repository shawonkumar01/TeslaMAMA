using TeslaMAMA.Models;

namespace TeslaMAMA.Services;

public interface IPassengerOverviewService
{
    Task<PassengerOverviewViewModel> GetOverviewAsync(string passengerId, string range);
}
