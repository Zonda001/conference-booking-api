namespace ConferenceBooking.Api.Application;

/// <summary>Запитаного об'єкта не існує -> 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Стан бази не дозволяє операцію (зал зайнятий, зал видалений) -> 409.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Вхідні дані не пройшли перевірку -> 400.</summary>
public sealed class ValidationException(string message) : Exception(message);
