namespace ConferenceBooking.Api.Pricing;

/// <summary>
/// Тарифна смуга. Смуги перетинаються (пікові 12:00-14:00 лежать усередині
/// стандартних 09:00-18:00), тому потрібен пріоритет: вужча смуга виграє.
/// </summary>
/// <param name="Name">Назва смуги для розкладу ціни.</param>
/// <param name="Start">Початок смуги, включно.</param>
/// <param name="End">Кінець смуги, виключно.</param>
/// <param name="Multiplier">Коефіцієнт до базової вартості залу.</param>
/// <param name="Priority">Більший пріоритет перекриває менший на спільному проміжку.</param>
public sealed record TariffBand(
    string Name,
    TimeOnly Start,
    TimeOnly End,
    decimal Multiplier,
    int Priority);
