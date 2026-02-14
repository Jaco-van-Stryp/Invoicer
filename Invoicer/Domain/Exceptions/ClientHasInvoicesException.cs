using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class ClientHasInvoicesException()
        : ApiException(
            "Cannot delete a client that has existing invoices",
            StatusCodes.Status409Conflict
        );
}
