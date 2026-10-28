using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeslaMAMA.Models;

namespace TeslaMAMA.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Pool> Pools => Set<Pool>();
    public DbSet<RideRequest> RideRequests => Set<RideRequest>();
    public DbSet<RideStatusHistory> RideStatusHistory => Set<RideStatusHistory>();
    public DbSet<PoolDecline> PoolDeclines => Set<PoolDecline>();
    public DbSet<DriverOffer> DriverOffers => Set<DriverOffer>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RoadDistance> RoadDistances => Set<RoadDistance>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Vehicle>(entity =>
        {
            entity.Property(v => v.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(v => v.CurrentZone)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(v => v.DriverId)
                .IsUnique();

            entity.HasOne(v => v.Driver)
                .WithOne()
                .HasForeignKey<Vehicle>(v => v.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Pool>(entity =>
        {
            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(p => p.Status);

            entity.HasOne(p => p.Vehicle)
                .WithMany(v => v.Pools)
                .HasForeignKey(p => p.VehicleId);
        });

        builder.Entity<RideRequest>(entity =>
        {
            entity.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(r => r.PaymentMethod)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(r => r.PickupZone)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(r => r.DestinationZone)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(r => r.FinalFare)
                .HasPrecision(10, 2);

            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.PoolId);

            entity.HasOne(r => r.Passenger)
                .WithMany()
                .HasForeignKey(r => r.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Pool)
                .WithMany(p => p.RideRequests)
                .HasForeignKey(r => r.PoolId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_RideRequest_Seats",
                    "\"Seats\" BETWEEN 1 AND 5");

                t.HasCheckConstraint(
                    "CK_RideRequest_FinalFare",
                    "\"FinalFare\" >= 0");
            });
        });

        builder.Entity<RideStatusHistory>(entity =>
        {
            entity.Property(h => h.FromStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(h => h.ToStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(h => h.RideRequest)
                .WithMany(r => r.StatusHistory)
                .HasForeignKey(h => h.RideRequestId);
        });

        builder.Entity<PoolDecline>(entity =>
        {
            entity.HasIndex(d => new
            {
                d.RideRequestId,
                d.VehicleId
            })
                .IsUnique();

            entity.HasOne(d => d.RideRequest)
                .WithMany(r => r.Declines)
                .HasForeignKey(d => d.RideRequestId);

            entity.HasOne(d => d.Vehicle)
                .WithMany(v => v.Declines)
                .HasForeignKey(d => d.VehicleId);
        });

        builder.Entity<DriverOffer>(entity =>
        {
            entity.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(o => new
            {
                o.RideRequestId,
                o.VehicleId
            })
                .IsUnique();

            entity.HasIndex(o => new
            {
                o.VehicleId,
                o.Status
            });

            entity.HasOne(o => o.RideRequest)
                .WithMany()
                .HasForeignKey(o => o.RideRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Vehicle)
                .WithMany()
                .HasForeignKey(o => o.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Method)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(p => p.TransactionId)
                .HasMaxLength(100);

            entity.Property(p => p.Notes)
                .HasMaxLength(500);

            entity.HasIndex(p => p.RideRequestId);
            entity.HasIndex(p => p.Status);

            entity.HasOne(p => p.RideRequest)
                .WithMany()
                .HasForeignKey(p => p.RideRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Payment_Amount",
                    "\"Amount\" >= 0");
            });
        });

        builder.Entity<RoadDistance>(entity =>
        {
            entity.Property(r => r.FromZone)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(r => r.ToZone)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(r => new { r.FromZone, r.ToZone })
                .IsUnique();
        });
    }
}