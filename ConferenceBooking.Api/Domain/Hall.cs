namespace ConferenceBooking.Api.Domain;

/// <summary>
/// Конференц-зал. Базова вартість години задається клієнтом при створенні
/// і змінюється через редагування - вона НЕ виводиться з місткості.
/// </summary>
public class Hall
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePricePerHour { get; set; }

    /// <summary>Зал з бронюваннями не видаляється фізично, а помічається видаленим.</summary>
    public bool IsDeleted { get; set; }

    public List<Service> AvailableServices { get; set; } = [];
    public List<Booking> Bookings { get; set; } = [];
}
