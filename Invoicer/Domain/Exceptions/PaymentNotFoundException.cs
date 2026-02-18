using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class PaymentNotFoundException()
        : ApiException(
            "The payment you're trying to manage does not exist",
            StatusCodes.Status404NotFound
        );
}
