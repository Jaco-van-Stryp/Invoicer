using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Estimate.GetAllEstimates
{
    public readonly record struct GetAllEstimatesQuery([Required] Guid CompanyId)
        : IRequest<List<GetAllEstimatesResponse>>;
}
