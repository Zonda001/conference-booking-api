using ConferenceBooking.Api.Application;
using ConferenceBooking.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(ReservationService reservations) : ControllerBase
{
    /// <summary>
    /// Метод 5 з ТЗ: бронювання залу. У відповідь іде не лише сума, а й розклад
    /// по тарифних смугах - клієнт бачить, за що платить.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<BookingResponse>> Create(
        CreateBookingRequest request, CancellationToken ct)
    {
        var booking = await reservations.CreateBookingAsync(request, ct);
        var price = reservations.Quote(booking.Hall, booking.StartsAt, booking.EndsAt, booking.Services);

        return CreatedAtAction(nameof(Create), new { id = booking.Id }, booking.ToResponse(price));
    }
}
