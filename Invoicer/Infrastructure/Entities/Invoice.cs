namespace Invoicer.Infrastructure.Entities
{
    public class Invoice
    {
        public required Guid Id { get; init; }
        public required string InvoiceNumber { get; init; }
        public required DateTime InvoiceDate { get; init; } = DateTime.UtcNow;
        public required DateTime InvoiceDue { get; init; } = DateTime.UtcNow.AddDays(30);
        public required ICollection<ProductInvoice> Products { get; init; } =
            new List<ProductInvoice>();
        public required Client Client { get; init; }
        public required Guid ClientId { get; init; }
        public required Company Company { get; init; }
        public required Guid CompanyId { get; init; }
    }
}
