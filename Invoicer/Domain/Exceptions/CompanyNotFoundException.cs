using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class CompanyNotFoundException()
        : ApiException(
            "The company you're trying to manage does not exist",
            StatusCodes.Status404NotFound
        );
}
