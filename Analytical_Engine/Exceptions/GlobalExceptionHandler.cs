using System.Security.Claims;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.Entities;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Analytical_Engine.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private const int MaxOperationLength = 100;
    private const int MaxErrorTypeLength = 200;
    private const int MaxMessageLength = 4000;
    private const int MaxStackTraceLength = 8000;

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        IServiceScopeFactory scopeFactory,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is RequestValidationException validationEx)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            var validationProblem = new ValidationProblemDetails(validationEx.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = httpContext.Request.Path
            };

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = validationProblem
            });
        }

        var (statusCode, title, detail) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            ConflictException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "Conflict", "A resource with the same unique key already exists."),
            UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            _ => (0, string.Empty, string.Empty)
        };

        if (statusCode == 0)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");

            // Only unexpected (500) errors are persisted; expected 4xx business
            // errors would just flood the table.
            await PersistErrorAsync(httpContext, exception);

            return false; // default handler writes the 500 ProblemDetails
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            }
        });
    }

    /// <summary>
    /// Saves the error using a fresh DI scope (the request's DbContext may be in a
    /// failed state). Must never throw: logging failures must not mask the original error.
    /// </summary>
    private async Task PersistErrorAsync(HttpContext httpContext, Exception exception)
    {
        try
        {
            Guid? userId = Guid.TryParse(
                httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var parsedUserId)
                ? parsedUserId
                : null;

            var entry = new ErrorLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Operation = Truncate(
                    $"{httpContext.Request.Method} {httpContext.Request.Path}",
                    MaxOperationLength),
                ErrorType = Truncate(exception.GetType().Name, MaxErrorTypeLength),
                Message = Truncate(exception.Message, MaxMessageLength),
                StackTrace = exception.StackTrace is null
                    ? null
                    : Truncate(exception.StackTrace, MaxStackTraceLength),
                OccurredAt = DateTime.UtcNow
            };

            await using var scope = _scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await unitOfWork.ErrorLogs.AddAsync(entry, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception loggingException)
        {
            _logger.LogError(loggingException, "Failed to persist error log entry.");
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
