using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Infrastructure.Validation
{
    public class ValidationException(string message)
        : ApiException(message, StatusCodes.Status400BadRequest);
}
