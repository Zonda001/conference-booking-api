namespace ConferenceBooking.Api.Domain;

public class Booking
{
    public int Id { get; set; }

    public int HallId { get; set; }
    public Hall Hall { get; set; } = null!;

    /// <summary>Включно.</summary>
    public DateTime StartsAt { get; set; }

    /// <summary>Виключно: бронювання 10:00-12:00 і 12:00-14:00 не конфліктують.</summary>
    public DateTime EndsAt { get; set; }

    public List<Service> Services { get; set; } = [];

    /// <summary>Ціни фіксуються на момент бронювання: зміна тарифу залу не переписує історію.</summary>
    public decimal HallCost { get; set; }
    public decimal ServicesCost { get; set; }
    public decimal TotalCost { get; set; }

    public DateTime CreatedAt { get; set; }
}
