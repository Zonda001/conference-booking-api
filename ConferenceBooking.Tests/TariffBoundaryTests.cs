using ConferenceBooking.Api.Domain;
using ConferenceBooking.Api.Pricing;

namespace ConferenceBooking.Tests;

/// <summary>
/// Випадки на межах смуг: перехід у вечірні, три смуги в одному бронюванні,
/// пів години всередині пікових.
/// </summary>
public class TariffBoundaryTests
{
    private readonly PriceCalculator _calculator = new(TariffSchedule.Default);

    private static DateTime At(int hour, int minute = 0) => new(2026, 9, 1, hour, minute, 0);

    [Fact]
    public void Crossing_into_evening_splits_into_two_segments()
    {
        var result = _calculator.Calculate(3500m, At(17), At(19), []);

        Assert.Equal(2, result.Segments.Count);
        Assert.Equal(3500m, result.Segments[0].Cost);   // 17-18 стандартні
        Assert.Equal(2800m, result.Segments[1].Cost);   // 18-19 вечірні, x0.8
        Assert.Equal(6300m, result.HallCost);
    }

    [Fact]
    public void Booking_across_morning_standard_and_peak_splits_into_three()
    {
        var result = _calculator.Calculate(1500m, At(8), At(13), []);

        Assert.Equal(3, result.Segments.Count);
        Assert.Equal(1350m, result.Segments[0].Cost);   // 08-09 ранкові, x0.9
        Assert.Equal(4500m, result.Segments[1].Cost);   // 09-12 стандартні
        Assert.Equal(1725m, result.Segments[2].Cost);   // 12-13 пікові, x1.15
        Assert.Equal(7575m, result.HallCost);
    }

    [Fact]
    public void Half_hour_inside_peak_with_service()
    {
        var projector = new Service { Name = "Проектор", PricePerBooking = 500m };

        var result = _calculator.Calculate(2000m, At(12), At(12, 30), [projector]);

        Assert.Single(result.Segments);
        Assert.Equal(1150m, result.HallCost);   // 0.5 год * 2000 * 1.15
        Assert.Equal(1650m, result.Total);
    }

    /// <summary>
    /// Експеримент: що буде, якщо зрівняти пріоритет пікових зі стандартними.
    /// Показує, чому Priority взагалі існує.
    /// </summary>
    [Fact]
    public void Equal_priority_makes_result_depend_on_list_order()
    {
        var broken = new TariffSchedule(
        [
            new TariffBand("Ранкові",    new TimeOnly(6, 0),  new TimeOnly(9, 0),  0.90m, Priority: 1),
            new TariffBand("Стандартні", new TimeOnly(9, 0),  new TimeOnly(18, 0), 1.00m, Priority: 1),
            new TariffBand("Вечірні",    new TimeOnly(18, 0), new TimeOnly(23, 0), 0.80m, Priority: 1),
            new TariffBand("Пікові",     new TimeOnly(12, 0), new TimeOnly(14, 0), 1.15m, Priority: 1),
        ]);

        var reordered = new TariffSchedule(
        [
            new TariffBand("Пікові",     new TimeOnly(12, 0), new TimeOnly(14, 0), 1.15m, Priority: 1),
            new TariffBand("Ранкові",    new TimeOnly(6, 0),  new TimeOnly(9, 0),  0.90m, Priority: 1),
            new TariffBand("Стандартні", new TimeOnly(9, 0),  new TimeOnly(18, 0), 1.00m, Priority: 1),
            new TariffBand("Вечірні",    new TimeOnly(18, 0), new TimeOnly(23, 0), 0.80m, Priority: 1),
        ]);

        var a = new PriceCalculator(broken).Calculate(2000m, At(12), At(13), []);
        var b = new PriceCalculator(reordered).Calculate(2000m, At(12), At(13), []);

        // Той самий тариф, той самий час - різна ціна, бо вирішує порядок у списку.
        Assert.Equal("Стандартні", a.Segments[0].Band);
        Assert.Equal(2000m, a.HallCost);

        Assert.Equal("Пікові", b.Segments[0].Band);
        Assert.Equal(2300m, b.HallCost);
    }
}
