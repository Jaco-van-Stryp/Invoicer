using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class InvoiceNotFoundException()
        : ApiException(
            "The invoice you're trying to manage does not exist",
            StatusCodes.Status404NotFound
        );
}
