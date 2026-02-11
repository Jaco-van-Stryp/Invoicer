using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class InvalidCredentialsException()
        : ApiException("Invalid email or password.", StatusCodes.Status401Unauthorized);
}
