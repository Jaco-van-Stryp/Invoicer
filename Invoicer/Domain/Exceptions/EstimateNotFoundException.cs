using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class EstimateNotFoundException()
        : ApiException(
            "The estimate you're trying to manage does not exist",
            StatusCodes.Status404NotFound
        );
}
