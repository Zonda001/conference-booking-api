using ConferenceBooking.Api.Domain;

namespace ConferenceBooking.Api.Pricing;

/// <summary>
/// Рахує вартість бронювання.
///
/// Головне рішення: інтервал ріжеться на відрізки по межах тарифних смуг і кожен
/// відрізок множиться на свій коефіцієнт. Брати один тариф за часом початку не можна -
/// бронювання 10:00-14:00 з прикладу в ТЗ проходить і через стандартні, і через пікові години.
///
/// Рахунок ведеться у хвилинах, тому дробові години і початок не на рівній годині
/// працюють без окремих випадків.
///
/// Коефіцієнти застосовуються ТІЛЬКИ до оренди залу: у ТЗ сказано "знижка 20% на оренду залу",
/// тому послуги додаються зверху повною вартістю.
/// </summary>
public sealed class PriceCalculator(TariffSchedule schedule)
{
    public PriceBreakdown Calculate(
        decimal basePricePerHour,
        DateTime start,
        DateTime end,
        IEnumerable<Service> services)
    {
        if (end <= start)
            throw new ArgumentException("Кінець бронювання має бути пізніше за початок.", nameof(end));

        var points = BuildBoundaries(start, end);
        var segments = new List<PriceSegment>(points.Count - 1);

        for (var i = 0; i < points.Count - 1; i++)
        {
            var (from, to) = (points[i], points[i + 1]);
            var middle = from.AddTicks((to - from).Ticks / 2);

            var band = schedule.BandAt(TimeOnly.FromDateTime(middle))
                       ?? throw new OutsideBusinessHoursException(from, to);

            var hours = (decimal)(to - from).TotalHours;
            var cost = decimal.Round(basePricePerHour * band.Multiplier * hours, 2);

            segments.Add(new PriceSegment(from, to, band.Name, band.Multiplier, cost));
        }

        var hallCost = segments.Sum(s => s.Cost);
        var servicesCost = services.Sum(s => s.PricePerBooking);

        return new PriceBreakdown(segments, hallCost, servicesCost, hallCost + servicesCost);
    }

    /// <summary>Точки, де може змінитись ціна: краї бронювання плюс усі межі смуг усередині нього.</summary>
    private List<DateTime> BuildBoundaries(DateTime start, DateTime end)
    {
        var points = new SortedSet<DateTime> { start, end };

        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            foreach (var boundary in schedule.Boundaries)
            {
                var moment = day.Add(boundary.ToTimeSpan());
                if (moment > start && moment < end)
                    points.Add(moment);
            }
        }

        return [.. points];
    }
}
