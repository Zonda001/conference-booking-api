using ConferenceBooking.Api.Domain;
using ConferenceBooking.Api.Pricing;

namespace ConferenceBooking.Tests;

public class PriceCalculatorTests
{
    private readonly PriceCalculator _calculator = new(TariffSchedule.Default);

    private static DateTime At(int hour, int minute = 0) => new(2026, 9, 1, hour, minute, 0);

    [Fact]
    public void Booking_that_crosses_two_bands_is_split_and_priced_per_segment()
    {
        // Приклад із ТЗ: 10:00-14:00. Перші дві години стандартні, наступні дві - пікові.
        var result = _calculator.Calculate(2000m, At(10), At(14), []);

        Assert.Equal(2, result.Segments.Count);
        Assert.Equal("Стандартні", result.Segments[0].Band);
        Assert.Equal(4000m, result.Segments[0].Cost);   // 2 год * 2000
        Assert.Equal("Пікові", result.Segments[1].Band);
        Assert.Equal(4600m, result.Segments[1].Cost);   // 2 год * 2000 * 1.15
        Assert.Equal(8600m, result.HallCost);
    }

    [Fact]
    public void Peak_band_wins_over_standard_because_it_is_narrower()
    {
        var result = _calculator.Calculate(1000m, At(12), At(13), []);

        Assert.Single(result.Segments);
        Assert.Equal("Пікові", result.Segments[0].Band);
        Assert.Equal(1150m, result.HallCost);
    }

    [Fact]
    public void Evening_discount_applies_to_hall_only()
    {
        var projector = new Service { Name = "Проектор", PricePerBooking = 500m };

        var result = _calculator.Calculate(2000m, At(19), At(21), [projector]);

        Assert.Equal(3200m, result.HallCost);      // 2 год * 2000 * 0.8
        Assert.Equal(500m, result.ServicesCost);   // без знижки
        Assert.Equal(3700m, result.Total);
    }

    [Fact]
    public void Morning_discount_is_applied()
    {
        var result = _calculator.Calculate(1500m, At(7), At(9), []);

        Assert.Equal(2700m, result.HallCost);      // 2 год * 1500 * 0.9
    }

    [Fact]
    public void Fractional_hours_and_offset_start_are_supported()
    {
        // 11:30-12:30: пів години стандартних, пів години пікових.
        var result = _calculator.Calculate(2000m, At(11, 30), At(12, 30), []);

        Assert.Equal(2, result.Segments.Count);
        Assert.Equal(1000m, result.Segments[0].Cost);   // 0.5 * 2000
        Assert.Equal(1150m, result.Segments[1].Cost);   // 0.5 * 2000 * 1.15
        Assert.Equal(2150m, result.HallCost);
    }

    [Fact]
    public void Booking_reaching_into_undefined_night_hours_is_rejected()
    {
        Assert.Throws<OutsideBusinessHoursException>(
            () => _calculator.Calculate(2000m, At(22), At(23).AddHours(2), []));
    }

    [Fact]
    public void End_before_start_is_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => _calculator.Calculate(2000m, At(14), At(10), []));
    }
}
