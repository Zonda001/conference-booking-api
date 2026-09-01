using ConferenceBooking.Api.Application;
using ConferenceBooking.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.Controllers;

[ApiController]
[Route("api/halls")]
public sealed class HallsController(ReservationService reservations) : ControllerBase
{
    /// <summary>Метод 1 з ТЗ: додавання конференц-залу.</summary>
    [HttpPost]
    [ProducesResponseType<HallResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<HallResponse>> Create(
        CreateHallRequest request, CancellationToken ct)
    {
        var hall = await reservations.CreateHallAsync(request, ct);
        return CreatedAtAction(nameof(Create), new { id = hall.Id }, hall.ToResponse());
    }

    /// <summary>Метод 2 з ТЗ: редагування інформації про зал.</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType<HallResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<HallResponse>> Update(
        int id, UpdateHallRequest request, CancellationToken ct)
    {
        var hall = await reservations.UpdateHallAsync(id, request, ct);
        return Ok(hall.ToResponse());
    }

    /// <summary>Метод 3 з ТЗ: видалення залу (м'яке, історія бронювань зберігається).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await reservations.DeleteHallAsync(id, ct);
        return NoContent();
    }

    /// <summary>Метод 4 з ТЗ: пошук вільних залів на проміжок часу.</summary>
    [HttpGet("available")]
    [ProducesResponseType<IReadOnlyList<HallResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HallResponse>>> Available(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int? capacity,
        CancellationToken ct)
    {
        var halls = await reservations.FindAvailableHallsAsync(from, to, capacity, ct);
        return Ok(halls.Select(h => h.ToResponse()).ToList());
    }
}
