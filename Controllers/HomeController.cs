using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TeslaMAMA.Models;
using TeslaMAMA.Services;

namespace TeslaMAMA.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFareCalculationService _fareCalculator;

        public HomeController(IFareCalculationService fareCalculator)
        {
            _fareCalculator = fareCalculator;
        }

        public IActionResult Index()
        {
            var model = new RideSearchViewModel
            {
                Zones = _fareCalculator.GetSupportedZones()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
