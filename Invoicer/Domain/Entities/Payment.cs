namespace Invoicer.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; init; }
        public required decimal Amount { get; set; }
        public required DateTime PaidOn { get; set; }
        public string? Notes { get; set; }
        public required Guid InvoiceId { get; init; }
        public required Invoice Invoice { get; init; }
        public required Guid CompanyId { get; init; }
        public required Company Company { get; init; }
    }
}
