using ConferenceBooking.Api.Contracts;
using ConferenceBooking.Api.Data;
using ConferenceBooking.Api.Pricing;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Api.Application;

/// <summary>
/// Звіти для бізнесу. ТЗ не називає конкретних, тому вибрані ті, що відповідають
/// на реальні питання власника: що приносить гроші, що простоює, чи працюють знижки.
/// </summary>
public sealed class ReportService(AppDbContext db, PriceCalculator calculator, TariffSchedule schedule)
{
    /// <summary>Робочих годин на добу за сіткою тарифів (06:00-23:00).</summary>
    private const double WorkingHoursPerDay = 17;

    public async Task<RevenueSummary> RevenueAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var bookings = await InRange(from, to).ToListAsync(ct);

        var hall = bookings.Sum(b => b.HallCost);
        var services = bookings.Sum(b => b.ServicesCost);
        var total = hall + services;

        return new RevenueSummary(
            from, to,
            bookings.Count,
            hall,
            services,
            total,
            bookings.Count == 0 ? 0 : decimal.Round(total / bookings.Count, 2),
            bookings.Count == 0 ? 0 : Math.Round(bookings.Average(b => (b.EndsAt - b.StartsAt).TotalHours), 2));
    }

    public async Task<IReadOnlyList<HallUtilizationRow>> UtilizationAsync(
        DateTime from, DateTime to, CancellationToken ct)
    {
        var days = Math.Max((to.Date - from.Date).TotalDays + 1, 1);
        var availableHours = days * WorkingHoursPerDay;

        var halls = await db.Halls.Where(h => !h.IsDeleted).ToListAsync(ct);
        var bookings = await InRange(from, to).ToListAsync(ct);

        return
        [
            .. halls
                .Select(h =>
                {
                    var own = bookings.Where(b => b.HallId == h.Id).ToList();
                    var booked = own.Sum(b => (b.EndsAt - b.StartsAt).TotalHours);

                    return new HallUtilizationRow(
                        h.Id, h.Name, h.Capacity,
                        own.Count,
                        Math.Round(booked, 2),
                        availableHours,
                        Math.Round(booked / availableHours * 100, 2),
                        own.Sum(b => b.TotalCost));
                })
                .OrderByDescending(r => r.UtilizationPercent)
        ];
    }

    /// <summary>
    /// Дохід по тарифних смугах. Розклад не зберігається в базі, а перераховується
    /// тим самим калькулятором - одне джерело правди для ціни.
    /// </summary>
    public async Task<IReadOnlyList<BandRevenueRow>> RevenueByBandAsync(
        DateTime from, DateTime to, CancellationToken ct)
    {
        var bookings = await InRange(from, to).Include(b => b.Hall).ToListAsync(ct);

        var segments = bookings
            .SelectMany(b => calculator
                .Calculate(b.Hall.BasePricePerHour, b.StartsAt, b.EndsAt, [])
                .Segments)
            .ToList();

        var totalRevenue = segments.Sum(s => s.Cost);

        return
        [
            .. segments
                .GroupBy(s => new { s.Band, s.Multiplier })
                .Select(g => new BandRevenueRow(
                    g.Key.Band,
                    g.Key.Multiplier,
                    Math.Round(g.Sum(s => s.Hours), 2),
                    g.Sum(s => s.Cost),
                    totalRevenue == 0 ? 0 : Math.Round((double)(g.Sum(s => s.Cost) / totalRevenue) * 100, 2)))
                .OrderByDescending(r => r.Revenue)
        ];
    }

    public async Task<IReadOnlyList<ServiceDemandRow>> ServiceDemandAsync(
        DateTime from, DateTime to, CancellationToken ct)
    {
        var bookings = await InRange(from, to).Include(b => b.Services).ToListAsync(ct);
        var allServices = await db.Services.ToListAsync(ct);

        return
        [
            .. allServices
                .Select(s =>
                {
                    var ordered = bookings.Count(b => b.Services.Any(x => x.Id == s.Id));

                    return new ServiceDemandRow(
                        s.Id, s.Name,
                        ordered,
                        bookings.Count == 0 ? 0 : Math.Round((double)ordered / bookings.Count * 100, 2),
                        ordered * s.PricePerBooking);
                })
                .OrderByDescending(r => r.Revenue)
        ];
    }

    public async Task<IReadOnlyList<SeatPriceRow>> SeatPricesAsync(CancellationToken ct)
    {
        var halls = await db.Halls.Where(h => !h.IsDeleted).ToListAsync(ct);

        return
        [
            .. halls
                .Select(h => new SeatPriceRow(
                    h.Id, h.Name, h.Capacity, h.BasePricePerHour,
                    h.Capacity == 0 ? 0 : decimal.Round(h.BasePricePerHour / h.Capacity, 2)))
                .OrderBy(r => r.Capacity)
        ];
    }

    public IReadOnlyList<TariffBand> Tariffs() => schedule.Bands;

    /// <summary>Бронювання, що хоч частиною потрапляють у період звіту.</summary>
    private IQueryable<Domain.Booking> InRange(DateTime from, DateTime to) =>
        db.Bookings.Where(b => b.StartsAt < to && from < b.EndsAt);
}
