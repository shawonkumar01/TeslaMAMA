
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Data;
using TeslaMAMA.Models;
using TeslaMAMA.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// PostgreSQL database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default")));

// ASP.NET Core Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<
    TeslaMAMA.Services.IFareCalculationService,
    TeslaMAMA.Services.FixedFareCalculationService>();
builder.Services.AddScoped<
    IDriverAssignmentService,
    TeslaMAMA.Services.DriverAssignmentService>();
builder.Services.AddHostedService<RideOfferTimeoutService>();

// Road distance caching
builder.Services.AddSingleton<RoadDistanceStore>();
builder.Services.AddHostedService<RoadDistanceSeeder>();

var app = builder.Build();
//await DbSeeder.SeedAsync(app.Services);
// Ensure required Identity roles exist without creating demo users or vehicles.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Passenger", "Driver" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role {role}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
