using ConferenceBooking.Api.Pricing;

namespace ConferenceBooking.Api.Contracts;

public sealed record CreateHallRequest(
    string Name,
    int Capacity,
    decimal BasePricePerHour,
    IReadOnlyList<int>? ServiceIds);

/// <summary>Часткове оновлення: передані поля змінюються, пропущені лишаються як були.</summary>
public sealed record UpdateHallRequest(
    string? Name,
    int? Capacity,
    decimal? BasePricePerHour,
    IReadOnlyList<int>? ServiceIds);

public sealed record ServiceResponse(int Id, string Name, decimal PricePerBooking);

public sealed record HallResponse(
    int Id,
    string Name,
    int Capacity,
    decimal BasePricePerHour,
    IReadOnlyList<ServiceResponse> AvailableServices);

/// <summary>ТЗ просить саме тривалість, а не час кінця.</summary>
public sealed record CreateBookingRequest(
    int HallId,
    DateTime StartsAt,
    double DurationHours,
    IReadOnlyList<int>? ServiceIds);

public sealed record PriceSegmentResponse(
    DateTime From, DateTime To, string Band, decimal Multiplier, decimal Cost);

public sealed record BookingResponse(
    int Id,
    int HallId,
    string HallName,
    DateTime StartsAt,
    DateTime EndsAt,
    IReadOnlyList<ServiceResponse> Services,
    IReadOnlyList<PriceSegmentResponse> PriceBreakdown,
    decimal HallCost,
    decimal ServicesCost,
    decimal TotalCost);
