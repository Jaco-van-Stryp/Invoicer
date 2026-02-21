using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Estimate.DeleteEstimate
{
    public readonly record struct DeleteEstimateCommand(Guid EstimateId)
        : IRequest<Unit>;
}
