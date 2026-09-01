namespace ConferenceBooking.Api.Contracts;

/// <summary>Скільки зал заробив і наскільки щільно був зайнятий.</summary>
public sealed record HallUtilizationRow(
    int HallId,
    string HallName,
    int Capacity,
    int BookingsCount,
    double BookedHours,
    double AvailableHours,
    double UtilizationPercent,
    decimal Revenue);

/// <summary>Дохід у розрізі тарифних смуг: видно, чи знижки реально приводять людей.</summary>
public sealed record BandRevenueRow(
    string Band,
    decimal Multiplier,
    double Hours,
    decimal Revenue,
    double SharePercent);

/// <summary>Які послуги замовляють, а які лежать мертвим вантажем.</summary>
public sealed record ServiceDemandRow(
    int ServiceId,
    string ServiceName,
    int TimesOrdered,
    double AttachRatePercent,
    decimal Revenue);

/// <summary>
/// Вартість однієї особи-години по залах. Початкові дані з ТЗ побудовані рівно
/// за такою сіткою (1500/30 = 50, 2000/50 = 40, 3500/100 = 35), тому метрика
/// одразу показує зал, який вибивається з цінової політики.
/// </summary>
public sealed record SeatPriceRow(
    int HallId,
    string HallName,
    int Capacity,
    decimal BasePricePerHour,
    decimal PricePerSeatHour);

/// <summary>Підсумок грошей за період.</summary>
public sealed record RevenueSummary(
    DateTime From,
    DateTime To,
    int BookingsCount,
    decimal HallRevenue,
    decimal ServicesRevenue,
    decimal TotalRevenue,
    decimal AverageBookingValue,
    double AverageDurationHours);
