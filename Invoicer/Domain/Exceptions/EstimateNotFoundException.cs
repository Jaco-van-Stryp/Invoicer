using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class EstimateNotFoundException() : ApiException("Estimate not found", 404);
}
