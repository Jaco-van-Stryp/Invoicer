using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Estimate.DeleteEstimate
{
    public readonly record struct DeleteEstimateCommand([Required] Guid EstimateId)
        : IRequest<Unit>;
}
