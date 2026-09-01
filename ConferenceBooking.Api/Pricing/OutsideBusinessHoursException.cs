namespace ConferenceBooking.Api.Pricing;

/// <summary>Частина бронювання потрапила в час, на який тариф не визначено (23:00-06:00).</summary>
public sealed class OutsideBusinessHoursException(DateTime from, DateTime to)
    : Exception($"Немає тарифу на проміжок {from:yyyy-MM-dd HH:mm} - {to:HH:mm}. Зали працюють з 06:00 до 23:00.")
{
    public DateTime From { get; } = from;
    public DateTime To { get; } = to;
}
