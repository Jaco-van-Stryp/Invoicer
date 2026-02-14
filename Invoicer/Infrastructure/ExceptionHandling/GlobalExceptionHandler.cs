using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Invoicer.Infrastructure.ExceptionHandling
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> _logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken
        )
        {
            var (statusCode, title) = exception switch
            {
                ApiException apiEx => (apiEx.StatusCode, apiEx.Message),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
            };

            if (exception is ApiException)
                _logger.LogWarning(
                    exception,
                    "API exception {StatusCode}: {Title}",
                    statusCode,
                    title
                );
            else
                _logger.LogError(exception, "Unhandled exception {StatusCode}", statusCode);

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = statusCode, Title = title },
                cancellationToken
            );

            return true;
        }
    }
}
