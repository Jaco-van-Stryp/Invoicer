using Invoicer.Domain.Entities;

namespace Invoicer.Features.Estimate.GetAllEstimates
{
    public record EstimateProductItem(
        Guid ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice
    );

    public record GetAllEstimatesResponse(
        Guid Id,
        string EstimateNumber,
        DateTime EstimateDate,
        DateTime ExpiresOn,
        EstimateStatus Status,
        decimal TotalAmount,
        string? Notes,
        Guid ClientId,
        string ClientName,
        List<EstimateProductItem> Products
    );
}
