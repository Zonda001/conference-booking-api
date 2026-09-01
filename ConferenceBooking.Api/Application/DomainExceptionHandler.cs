using ConferenceBooking.Api.Pricing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Api.Application;

/// <summary>
/// Єдина точка перетворення винятків у відповіді.
/// Клієнт отримує причину, але ніколи не отримує стек - це вимога безпеки з ТЗ.
/// </summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException           => (StatusCodes.Status404NotFound, "Не знайдено"),
            ConflictException           => (StatusCodes.Status409Conflict, "Конфлікт"),
            ValidationException         => (StatusCodes.Status400BadRequest, "Некоректні дані"),
            OutsideBusinessHoursException => (StatusCodes.Status400BadRequest, "Неробочий час"),
            ArgumentException           => (StatusCodes.Status400BadRequest, "Некоректні дані"),
            _                           => (StatusCodes.Status500InternalServerError, "Внутрішня помилка")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            // Несподівані помилки лишаються в логах, а назовні йде загальний текст.
            logger.LogError(exception, "Необроблений виняток");
        }

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "Спробуйте пізніше."
                    : exception.Message
            }
        });
    }
}
