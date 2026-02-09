using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Features.Auth.Login
{
    public class UnauthorizedException()
        : ApiException("Invalid email or password.", StatusCodes.Status401Unauthorized);
}
