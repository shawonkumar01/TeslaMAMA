
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;

namespace TeslaMAMA.Controllers;

[Authorize]
public class PassengerController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public PassengerController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        var passenger = await _userManager.GetUserAsync(User);

        if (passenger is null)
        {
            return Challenge();
        }

        if (passenger.Role != UserRole.Passenger)
        {
            return Forbid();
        }

        var passengerRides = _dbContext.RideRequests
            .AsNoTracking()
            .Where(r => r.PassengerId == passenger.Id);

        var activeStatuses = new[]
        {
            RideStatus.Requested,
            RideStatus.Matched,
            RideStatus.DriverArrived,
            RideStatus.Started
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
}
