using Invoicer.Domain.Enums;

namespace Invoicer.Features.Estimate.UpdateEstimate
{
    public readonly record struct UpdateEstimateProductItem(Guid ProductId, int Quantity);

    public readonly record struct UpdateEstimateCommand(
        Guid CompanyId,
        Guid EstimateId,
        Guid? ClientId,
        DateTime? EstimateDate,
        DateTime? ExpiresOn,
        EstimateStatus? Status,
        string? Notes,
        List<UpdateEstimateProductItem>? Products
    ) : MediatR.IRequest;
}
