
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;
using TeslaMAMA.Services;

namespace TeslaMAMA.Controllers;

[Authorize]
public class VehicleController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFareCalculationService _fareCalculator;

    public VehicleController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IFareCalculationService fareCalculator)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _fareCalculator = fareCalculator;
    }

    [HttpGet]
    public async Task<IActionResult> Register(
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null)
            return Challenge();

        if (driver.Role != UserRole.Driver)
            return Forbid();

        var existingVehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .AnyAsync(v => v.DriverId == driver.Id, cancellationToken);

        if (existingVehicle)
            return RedirectToAction("Index", "Driver");

        var model = new RegisterVehicleViewModel();
        PopulateZones(model);

        return View(model);
    }

    // GET: /Vehicle/Edit
    [HttpGet]
    public async Task<IActionResult> Edit(
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
            return RedirectToAction(nameof(Register));

        var model = new RegisterVehicleViewModel
        {
            Name = vehicle.Name,
            Capacity = vehicle.Capacity,
            CurrentZone = vehicle.CurrentZone,
            SupportedZones = _fareCalculator.GetSupportedZones().ToList()
        };

        return View(model);
    }

    // POST: /Vehicle/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        RegisterVehicleViewModel model,
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
            return RedirectToAction(nameof(Register));

        model.SupportedZones = _fareCalculator.GetSupportedZones().ToList();

        var selectedZone = model.SupportedZones.FirstOrDefault(
            zone => string.Equals(
                zone,
                model.CurrentZone?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (selectedZone is null)
        {
            ModelState.AddModelError(
                nameof(model.CurrentZone),
                "Select a valid operating location.");
        }

        if (vehicle.IsOnline)
        {
            ModelState.AddModelError(
                string.Empty,
                "Go offline before editing your vehicle.");
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
            ModelState.AddModelError(
                string.Empty,
                "You cannot edit your vehicle while you have an active ride.");
        }

        if (!ModelState.IsValid)
            return View(model);

        vehicle.Name = model.Name.Trim();
        vehicle.Capacity = model.Capacity;
        vehicle.CurrentZone = selectedZone!;

        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Vehicle details updated successfully.";

        return RedirectToAction("Index", "Driver");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterVehicleViewModel model,
        CancellationToken cancellationToken)
    {
        var driver = await _userManager.GetUserAsync(User);

        if (driver is null)
            return Challenge();

        if (driver.Role != UserRole.Driver)
            return Forbid();

        PopulateZones(model);

        var supportedZones = _fareCalculator.GetSupportedZones();

        if (!supportedZones.Contains(
                model.CurrentZone,
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.CurrentZone),
                "Select a supported current location.");
        }

        var alreadyRegistered = await _dbContext.Vehicles
            .AnyAsync(v => v.DriverId == driver.Id, cancellationToken);

        if (alreadyRegistered)
            return RedirectToAction("Index", "Driver");

        if (!ModelState.IsValid)
            return View(model);

        var vehicle = new Vehicle
        {
            Name = model.Name.Trim(),
            Capacity = model.Capacity,
            CurrentZone = model.CurrentZone,
            DriverId = driver.Id,
            IsOnline = false
        };

        _dbContext.Vehicles.Add(vehicle);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The vehicle could not be registered. Please try again.");

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Vehicle registered successfully. You can now manage your availability.";

        return RedirectToAction("Index", "Driver");
    }

    private void PopulateZones(RegisterVehicleViewModel model)
    {
        model.SupportedZones = _fareCalculator
            .GetSupportedZones()
            .ToList();
    }
}
