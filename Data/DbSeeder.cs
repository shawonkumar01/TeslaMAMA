
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Models;

namespace TeslaMAMA.Data;

public static class DbSeeder
{
    private const string DemoPassword = "TeslaDemo@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        var jashim = await GetOrCreateUser(
            userManager, "Jashim", "jashim@example.com", UserRole.Driver);

        var kamal = await GetOrCreateUser(
            userManager, "Kamal", "kamal@example.com", UserRole.Driver);

        var nusrat = await GetOrCreateUser(
            userManager, "Nusrat", "nusrat@example.com", UserRole.Passenger);

        var rafiq = await GetOrCreateUser(
            userManager, "Rafiq", "rafiq@example.com", UserRole.Passenger);

        var shirin = await GetOrCreateUser(
            userManager, "Shirin", "shirin@example.com", UserRole.Passenger);

        var bullet = await GetOrCreateVehicle(db, jashim, "Bullet");
        var rocket = await GetOrCreateVehicle(db, kamal, "Rocket");

        var pool = await db.Pools
            .Where(p => p.VehicleId == bullet.Id &&
                        p.Status == PoolStatus.Open)
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();

        if (pool == null)
        {
            pool = new Pool
            {
                VehicleId = bullet.Id,
                Status = PoolStatus.Open,
                SeatsUsed = 0
            };

            db.Pools.Add(pool);
            await db.SaveChangesAsync();
        }

        // Sample fares are stored in the decimal currency format
        // expected by RideRequest.FinalFare.
        await GetOrCreateRide(
            db, nusrat, "Banani", "Mohakhali",
            pool, RideStatus.Matched, 52.80m);

        await GetOrCreateRide(
            db, rafiq, "Banani", "Gulshan 1",
            pool, RideStatus.Matched, 52.16m);

        await GetOrCreateRide(
            db, shirin, "Banani", "Mohakhali",
            null, RideStatus.Requested, 66.00m);

        pool.SeatsUsed = await db.RideRequests
            .Where(r => r.PoolId == pool.Id &&
                        r.Status != RideStatus.Cancelled)
            .SumAsync(r => (int?)r.Seats) ?? 0;

        await db.SaveChangesAsync();

        Console.WriteLine("TeslaMAMA demo data seeded successfully.");
        Console.WriteLine("Demo password: " + DemoPassword);
    }

    private static async Task<ApplicationUser> GetOrCreateUser(
        UserManager<ApplicationUser> manager,
        string fullName,
        string email,
        UserRole role)
    {
        var user = await manager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                Role = role,
                EmailConfirmed = true
            };

            var result = await manager.CreateAsync(user, DemoPassword);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Creating {email} failed: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        else if (user.Role != role || user.FullName != fullName)
        {
            user.Role = role;
            user.FullName = fullName;

            var result = await manager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (!await manager.IsInRoleAsync(user, role.ToString()))
        {
            var result = await manager.AddToRoleAsync(user, role.ToString());

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    private static async Task<Vehicle> GetOrCreateVehicle(
        AppDbContext db,
        ApplicationUser driver,
        string name)
    {
        var vehicle = await db.Vehicles
            .FirstOrDefaultAsync(v => v.DriverId == driver.Id);

        if (vehicle != null)
            return vehicle;

        vehicle = new Vehicle
        {
            Name = name,
            Capacity = 3,
            CurrentZone = "Banani",
            IsOnline = true,
            DriverId = driver.Id
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return vehicle;
    }

    private static async Task GetOrCreateRide(
        AppDbContext db,
        ApplicationUser passenger,
        string pickup,
        string destination,
        Pool? pool,
        RideStatus status,
        decimal fare)
    {
        var exists = await db.RideRequests.AnyAsync(r =>
            r.PassengerId == passenger.Id &&
            r.PickupZone == pickup &&
            r.DestinationZone == destination);

        if (exists)
            return;

        var ride = new RideRequest
        {
            PassengerId = passenger.Id,
            PickupZone = pickup,
            DestinationZone = destination,
            Seats = 1,
            PoolId = pool?.Id,
            Status = status,
            PaymentMethod = PaymentMethod.Cash,
            FinalFare = fare
        };

        ride.StatusHistory.Add(new RideStatusHistory
        {
            FromStatus = null,
            ToStatus = RideStatus.Requested
        });

        if (status != RideStatus.Requested)
        {
            ride.StatusHistory.Add(new RideStatusHistory
            {
                FromStatus = RideStatus.Requested,
                ToStatus = status
            });
        }

        db.RideRequests.Add(ride);
        await db.SaveChangesAsync();
    }
}
