using Invoicer.Domain.Enums;

namespace Invoicer.Features.Estimate.GetAllEstimates
{
    public record struct EstimateProductItem(
        Guid ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        bool IsTaxed
    );

    public record struct GetAllEstimatesResponse(
        Guid Id,
        string EstimateNumber,
        DateTime EstimateDate,
        DateTime ExpiresOn,
        EstimateStatus Status,
        decimal TotalAmount,
        decimal Subtotal,
        decimal TaxAmount,
        decimal TaxRate,
        string? TaxName,
        string? Notes,
        Guid ClientId,
        string ClientName,
        List<EstimateProductItem> Products
    );
}
