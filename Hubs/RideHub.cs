using Microsoft.AspNetCore.SignalR;

namespace TeslaMAMA.Hubs;

public class RideHub : Hub
{
    public async Task JoinDriverGroup(string driverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"driver_{driverId}");
    }

    public async Task LeaveDriverGroup(string driverId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"driver_{driverId}");
    }

    public async Task JoinPassengerGroup(string passengerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"passenger_{passengerId}");
    }

    public async Task LeavePassengerGroup(string passengerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"passenger_{passengerId}");
    }
}
