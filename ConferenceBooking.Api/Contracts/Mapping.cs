using ConferenceBooking.Api.Domain;
using ConferenceBooking.Api.Pricing;

namespace ConferenceBooking.Api.Contracts;

public static class Mapping
{
    public static ServiceResponse ToResponse(this Service service) =>
        new(service.Id, service.Name, service.PricePerBooking);

    public static HallResponse ToResponse(this Hall hall) =>
        new(hall.Id, hall.Name, hall.Capacity, hall.BasePricePerHour,
            [.. hall.AvailableServices.Select(s => s.ToResponse())]);

    public static BookingResponse ToResponse(this Booking booking, PriceBreakdown price) =>
        new(booking.Id,
            booking.HallId,
            booking.Hall.Name,
            booking.StartsAt,
            booking.EndsAt,
            [.. booking.Services.Select(s => s.ToResponse())],
            [.. price.Segments.Select(s =>
                new PriceSegmentResponse(s.From, s.To, s.Band, s.Multiplier, s.Cost))],
            booking.HallCost,
            booking.ServicesCost,
            booking.TotalCost);
}
