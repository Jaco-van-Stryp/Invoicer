using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Payment.GetAllPayments
{
    public readonly record struct GetAllPaymentsQuery(
        [Required] Guid CompanyId
    ) : IRequest<List<GetAllPaymentsResponse>>;
}
