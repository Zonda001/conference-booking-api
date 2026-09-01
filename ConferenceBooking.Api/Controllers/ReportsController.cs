using ConferenceBooking.Api.Application;
using ConferenceBooking.Api.Contracts;
using ConferenceBooking.Api.Pricing;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.Controllers;

/// <summary>
/// Звіти з пункту «Звіти та аналітика» додаткових вимог ТЗ.
/// </summary>
[ApiController]
[Route("api/reports")]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    /// <summary>Гроші за період: скільки принесли зали, скільки послуги, середній чек.</summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueSummary>> Revenue(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        Ok(await reports.RevenueAsync(from, to, ct));

    /// <summary>Завантаженість залів: що працює, а що простоює.</summary>
    [HttpGet("utilization")]
    public async Task<ActionResult<IReadOnlyList<HallUtilizationRow>>> Utilization(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        Ok(await reports.UtilizationAsync(from, to, ct));

    /// <summary>Дохід у розрізі тарифних смуг: чи справді знижки заповнюють ранок і вечір.</summary>
    [HttpGet("revenue-by-band")]
    public async Task<ActionResult<IReadOnlyList<BandRevenueRow>>> RevenueByBand(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        Ok(await reports.RevenueByBandAsync(from, to, ct));

    /// <summary>Попит на послуги і частка бронювань, у які їх додають.</summary>
    [HttpGet("service-demand")]
    public async Task<ActionResult<IReadOnlyList<ServiceDemandRow>>> ServiceDemand(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        Ok(await reports.ServiceDemandAsync(from, to, ct));

    /// <summary>Вартість особи-години: показує зал, що вибивається з цінової сітки.</summary>
    [HttpGet("seat-prices")]
    public async Task<ActionResult<IReadOnlyList<SeatPriceRow>>> SeatPrices(CancellationToken ct) =>
        Ok(await reports.SeatPricesAsync(ct));

    /// <summary>Чинна сітка тарифів, щоб клієнт не гадав, звідки беруться коефіцієнти.</summary>
    [HttpGet("tariffs")]
    public ActionResult<IReadOnlyList<TariffBand>> Tariffs() => Ok(reports.Tariffs());
}
