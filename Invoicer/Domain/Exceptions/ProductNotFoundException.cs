using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class ProductNotFoundException()
        : ApiException(
            "The product you're trying to manage does not exist",
            StatusCodes.Status404NotFound
        );
}
