using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class ClientNotFoundException()
        : ApiException(
            "The client you're trying to manage does not exist",
            StatusCodes.Status404NotFound
        );
}
