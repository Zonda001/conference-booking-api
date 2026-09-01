namespace ConferenceBooking.Api.Domain;

/// <summary>
/// Додаткова послуга. Назва властивості фіксує рішення: ціна разова за бронювання,
/// а не погодинна. У ТЗ одиниця не вказана.
/// </summary>
public class Service
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PricePerBooking { get; set; }

    public List<Hall> Halls { get; set; } = [];
}
