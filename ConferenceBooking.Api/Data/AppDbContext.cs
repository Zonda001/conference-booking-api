using ConferenceBooking.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Hall>(hall =>
        {
            hall.Property(h => h.Name).IsRequired().HasMaxLength(200);
            hall.Property(h => h.BasePricePerHour).HasPrecision(18, 2);
            hall.HasIndex(h => h.IsDeleted);
        });

        model.Entity<Service>(service =>
        {
            service.Property(s => s.Name).IsRequired().HasMaxLength(200);
            service.Property(s => s.PricePerBooking).HasPrecision(18, 2);
        });

        model.Entity<Booking>(booking =>
        {
            booking.Property(b => b.HallCost).HasPrecision(18, 2);
            booking.Property(b => b.ServicesCost).HasPrecision(18, 2);
            booking.Property(b => b.TotalCost).HasPrecision(18, 2);

            // Пошук перетинів іде по залу і часу - без цього індексу кожна перевірка
            // конфлікту сканує всю таблицю.
            booking.HasIndex(b => new { b.HallId, b.StartsAt, b.EndsAt });

            booking.HasOne(b => b.Hall)
                   .WithMany(h => h.Bookings)
                   .HasForeignKey(b => b.HallId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        SeedFromSpec(model);
    }

    /// <summary>Початкові дані з ТЗ.</summary>
    private static void SeedFromSpec(ModelBuilder model)
    {
        model.Entity<Hall>().HasData(
            new { Id = 1, Name = "Зал A", Capacity = 50,  BasePricePerHour = 2000m, IsDeleted = false },
            new { Id = 2, Name = "Зал B", Capacity = 100, BasePricePerHour = 3500m, IsDeleted = false },
            new { Id = 3, Name = "Зал C", Capacity = 30,  BasePricePerHour = 1500m, IsDeleted = false });

        model.Entity<Service>().HasData(
            new { Id = 1, Name = "Проектор", PricePerBooking = 500m },
            new { Id = 2, Name = "Wi-Fi",    PricePerBooking = 300m },
            new { Id = 3, Name = "Звук",     PricePerBooking = 700m });

        // Усі три послуги доступні в усіх залах.
        model.Entity("HallService").HasData(
            from hallId in new[] { 1, 2, 3 }
            from serviceId in new[] { 1, 2, 3 }
            select new { AvailableServicesId = serviceId, HallsId = hallId });
    }
}
