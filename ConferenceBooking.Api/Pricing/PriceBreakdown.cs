namespace ConferenceBooking.Api.Pricing;

/// <summary>Відрізок бронювання, що цілком лежить в одній тарифній смузі.</summary>
public sealed record PriceSegment(
    DateTime From,
    DateTime To,
    string Band,
    decimal Multiplier,
    decimal Cost)
{
    public double Hours => (To - From).TotalHours;
}

/// <summary>
/// Розклад ціни. Повертається клієнту разом із підтвердженням, щоб було видно,
/// звідки взялась сума, а не лише підсумок.
/// </summary>
public sealed record PriceBreakdown(
    IReadOnlyList<PriceSegment> Segments,
    decimal HallCost,
    decimal ServicesCost,
    decimal Total);
