using ConferenceBooking.Api.Contracts;
using ConferenceBooking.Api.Data;
using ConferenceBooking.Api.Domain;
using ConferenceBooking.Api.Pricing;
using Microsoft.EntityFrameworkCore;

namespace ConferenceBooking.Api.Application;

/// <summary>
/// Уся бізнес-логіка залів і бронювань. Контролери лишаються тонкими:
/// приймають HTTP, віддають HTTP, рішень не ухвалюють.
/// </summary>
public sealed class ReservationService(AppDbContext db, PriceCalculator calculator)
{
    /// <summary>Робочий день із сітки тарифів: 06:00-23:00, тобто максимум 17 годин поспіль.</summary>
    private const double MaxDurationHours = 17;

    public async Task<Hall> CreateHallAsync(CreateHallRequest request, CancellationToken ct)
    {
        ValidateHallFields(request.Name, request.Capacity, request.BasePricePerHour);

        var hall = new Hall
        {
            Name = request.Name.Trim(),
            Capacity = request.Capacity,
            BasePricePerHour = request.BasePricePerHour,
            AvailableServices = await LoadServicesAsync(request.ServiceIds, ct)
        };

        db.Halls.Add(hall);
        await db.SaveChangesAsync(ct);
        return hall;
    }

    public async Task<Hall> UpdateHallAsync(int id, UpdateHallRequest request, CancellationToken ct)
    {
        var hall = await db.Halls
            .Include(h => h.AvailableServices)
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, ct)
            ?? throw new NotFoundException($"Зал {id} не знайдено.");

        ValidateHallFields(
            request.Name ?? hall.Name,
            request.Capacity ?? hall.Capacity,
            request.BasePricePerHour ?? hall.BasePricePerHour);

        if (request.Name is not null) hall.Name = request.Name.Trim();
        if (request.Capacity is not null) hall.Capacity = request.Capacity.Value;
        if (request.BasePricePerHour is not null) hall.BasePricePerHour = request.BasePricePerHour.Value;

        // Зміна тарифу діє тільки на майбутні бронювання: у вже створених ціна зафіксована.
        if (request.ServiceIds is not null)
            hall.AvailableServices = await LoadServicesAsync(request.ServiceIds, ct);

        await db.SaveChangesAsync(ct);
        return hall;
    }

    /// <summary>
    /// М'яке видалення. Фізичне знищення залу зламало б історію бронювань і звіти,
    /// тому зал лише зникає з видачі.
    /// </summary>
    public async Task DeleteHallAsync(int id, CancellationToken ct)
    {
        var hall = await db.Halls.FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, ct)
            ?? throw new NotFoundException($"Зал {id} не знайдено.");

        hall.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Hall>> FindAvailableHallsAsync(
        DateTime from, DateTime to, int? minCapacity, CancellationToken ct)
    {
        if (to <= from)
            throw new ValidationException("Кінець проміжку має бути пізніше за початок.");

        return await db.Halls
            .Include(h => h.AvailableServices)
            .Where(h => !h.IsDeleted)
            .Where(h => minCapacity == null || h.Capacity >= minCapacity)
            // Перетин: бронювання починається до кінця проміжку і закінчується після його початку.
            .Where(h => !h.Bookings.Any(b => b.StartsAt < to && from < b.EndsAt))
            .OrderBy(h => h.Capacity)
            .ToListAsync(ct);
    }

    public async Task<Booking> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct)
    {
        if (request.DurationHours <= 0)
            throw new ValidationException("Тривалість має бути більшою за нуль.");
        if (request.DurationHours > MaxDurationHours)
            throw new ValidationException($"Максимальна тривалість - {MaxDurationHours} годин.");

        var start = request.StartsAt;
        var end = start.AddHours(request.DurationHours);

        // Транзакція потрібна саме тут: між перевіркою зайнятості і вставкою не має
        // проліземти інше бронювання. SQLite серіалізує записувачів, тому повторна
        // перевірка всередині транзакції закриває гонку. На PostgreSQL те саме
        // надійніше робити обмеженням EXCLUDE по діапазону (див. README).
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var hall = await db.Halls
            .Include(h => h.AvailableServices)
            .FirstOrDefaultAsync(h => h.Id == request.HallId && !h.IsDeleted, ct)
            ?? throw new NotFoundException($"Зал {request.HallId} не знайдено.");

        var overlaps = await db.Bookings
            .AnyAsync(b => b.HallId == hall.Id && b.StartsAt < end && start < b.EndsAt, ct);

        if (overlaps)
            throw new ConflictException($"Зал \"{hall.Name}\" уже зайнятий у цей час.");

        var services = SelectRequestedServices(hall, request.ServiceIds);

        // Кидає OutsideBusinessHoursException, якщо бронювання зачепило ніч.
        var price = calculator.Calculate(hall.BasePricePerHour, start, end, services);

        var booking = new Booking
        {
            HallId = hall.Id,
            StartsAt = start,
            EndsAt = end,
            Services = services,
            HallCost = price.HallCost,
            ServicesCost = price.ServicesCost,
            TotalCost = price.Total,
            CreatedAt = DateTime.UtcNow
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        booking.Hall = hall;
        return booking;
    }

    /// <summary>Розклад ціни без створення бронювання - зручно клієнту і використовується у відповіді.</summary>
    public PriceBreakdown Quote(Hall hall, DateTime start, DateTime end, List<Service> services) =>
        calculator.Calculate(hall.BasePricePerHour, start, end, services);

    private static void ValidateHallFields(string name, int capacity, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Назва залу обов'язкова.");
        if (capacity <= 0)
            throw new ValidationException("Місткість має бути більшою за нуль.");
        if (price < 0)
            throw new ValidationException("Вартість не може бути від'ємною.");
    }

    private async Task<List<Service>> LoadServicesAsync(IReadOnlyList<int>? ids, CancellationToken ct)
    {
        if (ids is null || ids.Count == 0) return [];

        var services = await db.Services.Where(s => ids.Contains(s.Id)).ToListAsync(ct);

        var missing = ids.Except(services.Select(s => s.Id)).ToList();
        if (missing.Count > 0)
            throw new ValidationException($"Послуги не знайдено: {string.Join(", ", missing)}.");

        return services;
    }

    /// <summary>У бронюванні можна замовити лише послуги, доступні саме в цьому залі.</summary>
    private static List<Service> SelectRequestedServices(Hall hall, IReadOnlyList<int>? ids)
    {
        if (ids is null || ids.Count == 0) return [];

        var selected = hall.AvailableServices.Where(s => ids.Contains(s.Id)).ToList();

        var unavailable = ids.Except(selected.Select(s => s.Id)).ToList();
        if (unavailable.Count > 0)
            throw new ValidationException(
                $"У залі \"{hall.Name}\" недоступні послуги: {string.Join(", ", unavailable)}.");

        return selected;
    }
}
