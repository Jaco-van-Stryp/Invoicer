using System.ComponentModel.DataAnnotations;
using Invoicer.Domain.Entities;
using MediatR;

namespace Invoicer.Features.Estimate.CreateEstimate
{
    public readonly record struct CreateEstimateProductItem(
        [Required] Guid ProductId,
        [Required] [Range(1, int.MaxValue)] int Quantity
    );

    public readonly record struct CreateEstimateCommand(
        [Required] Guid CompanyId,
        [Required] Guid ClientId,
        [Required] DateTime EstimateDate,
        [Required] DateTime ExpiresOn,
        EstimateStatus Status,
        string? Notes,
        [Required] [MinLength(1)] List<CreateEstimateProductItem> Products
    ) : IRequest<CreateEstimateResponse>;
}
