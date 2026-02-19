namespace Invoicer.Domain.Entities
{
    public enum EstimateStatus
    {
        Draft = 0,
        Sent = 1,
        Accepted = 2,
        Declined = 3
    }

    public class Estimate
    {
        public Guid Id { get; init; }
        public required string EstimateNumber { get; set; }
        public required DateTime EstimateDate { get; set; }
        public required DateTime ExpiresOn { get; set; }
        public required EstimateStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public required Guid ClientId { get; init; }
        public required Client Client { get; init; }
        public required Guid CompanyId { get; init; }
        public required Company Company { get; init; }
        public required ICollection<ProductEstimate> ProductEstimates { get; init; } = [];
    }
}
