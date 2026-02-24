namespace Invoicer.Domain.Entities
{
    public class ProductEstimate
    {
        public Guid Id { get; init; }
        public required Guid EstimateId { get; init; }
        public required Estimate Estimate { get; init; }
        public required Guid ProductId { get; init; }
        public required Product Product { get; init; }
        public required int Quantity { get; set; }
        public required decimal UnitPrice { get; set; }
        public required Guid CompanyId { get; init; }
        public required Company Company { get; init; }
    }
}
