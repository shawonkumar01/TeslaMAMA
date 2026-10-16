
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;

namespace TeslaMAMA.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _dbContext;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "An account with this email already exists.");

            return View(model);
        }

        var role = string.Equals(
            model.AccountType,
            "Driver",
            StringComparison.OrdinalIgnoreCase)
                ? UserRole.Driver
                : UserRole.Passenger;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            Role = role,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            role.ToString());

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["SuccessMessage"] =
            "Your account has been created successfully!";

        if (role == UserRole.Driver)
        {
            return RedirectToAction(
                "Register",
                "Vehicle");
        }

        return RedirectToAction(
            "Dashboard",
            "Passenger");
    }

    [HttpGet]
    public IActionResult RegisterSuccess()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role == UserRole.Driver)
            {
                var hasVehicle = await _dbContext.Vehicles
                    .AnyAsync(v => v.DriverId == user.Id);

                return hasVehicle
                    ? RedirectToAction("Index", "Driver")
                    : RedirectToAction("Register", "Vehicle");
            }

            if (user.Role == UserRole.Passenger)
            {
                return RedirectToAction("Dashboard", "Passenger");
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Your account is temporarily locked. Please try again later.");
        }
        else
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }
}