using System.ComponentModel.DataAnnotations;
using Invoicer.Domain.Entities;
using MediatR;

namespace Invoicer.Features.Estimate.CreateEstimate
{
    public readonly record struct CreateEstimateProductItem(
         Guid ProductId,
         int Quantity
    );

    public readonly record struct CreateEstimateCommand(
         Guid CompanyId,
         Guid ClientId,
         DateTime EstimateDate,
         DateTime ExpiresOn,
         EstimateStatus Status,
         string? Notes,
         List<CreateEstimateProductItem> Products
    ) : IRequest<CreateEstimateResponse>;
}
