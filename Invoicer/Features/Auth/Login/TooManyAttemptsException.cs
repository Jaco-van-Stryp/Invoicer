using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Features.Auth.Login
{
    public class TooManyAttemptsException()
        : ApiException("Too many attempts. Please try again later.", StatusCodes.Status429TooManyRequests);
}
