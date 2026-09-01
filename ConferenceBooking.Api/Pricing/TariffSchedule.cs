namespace ConferenceBooking.Api.Pricing;

/// <summary>
/// Сітка тарифів із ТЗ. Ніч 23:00-06:00 у ТЗ не описана - вважаємо неробочим часом
/// і відхиляємо такі бронювання (рішення задокументоване в README).
/// </summary>
public sealed class TariffSchedule
{
    public static readonly TariffSchedule Default = new(
    [
        new TariffBand("Ранкові",    new TimeOnly(6, 0),  new TimeOnly(9, 0),  0.90m, Priority: 1),
        new TariffBand("Стандартні", new TimeOnly(9, 0),  new TimeOnly(18, 0), 1.00m, Priority: 1),
        new TariffBand("Вечірні",    new TimeOnly(18, 0), new TimeOnly(23, 0), 0.80m, Priority: 1),
        new TariffBand("Пікові",     new TimeOnly(12, 0), new TimeOnly(14, 0), 1.15m, Priority: 2),
    ]);

    private readonly IReadOnlyList<TariffBand> _bands;

    public TariffSchedule(IReadOnlyList<TariffBand> bands) => _bands = bands;

    public IReadOnlyList<TariffBand> Bands => _bands;

    /// <summary>Смуга, що діє в конкретний момент, або null у неробочий час.</summary>
    public TariffBand? BandAt(TimeOnly time) =>
        _bands
            .Where(b => time >= b.Start && time < b.End)
            .OrderByDescending(b => b.Priority)
            .FirstOrDefault();

    /// <summary>Усі межі смуг у межах доби - точки, де може змінитись ціна.</summary>
    public IEnumerable<TimeOnly> Boundaries =>
        _bands.SelectMany(b => new[] { b.Start, b.End }).Distinct();
}
