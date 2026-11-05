using Microsoft.AspNetCore.Mvc;
using TeslaMAMA.Models;
using TeslaMAMA.Services;

namespace TeslaMAMA.Controllers;

public class RidesController : Controller
{
    private readonly IFareCalculationService _fareCalculator;

    public RidesController(IFareCalculationService fareCalculator)
    {
        _fareCalculator = fareCalculator;
    }

    // GET: /Rides/Book — hero search form; hands off to the passenger booking flow
    [HttpGet]
    public IActionResult Book(RideSearchViewModel model)
    {
        return RedirectToAction(
            nameof(PassengerController.Book),
            "Passenger",
            new
            {
                pickupZone = model.Pickup,
                destinationZone = model.Dropoff
            });
    }
}
