namespace Invoicer.Domain.Entities
{
    public class Invoice
    {
        public Guid Id { get; init; }
        public required string InvoiceNumber { get; set; }
        public required DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public required DateTime InvoiceDue { get; set; } = DateTime.UtcNow.AddDays(30);
        public required ICollection<ProductInvoice> Products { get; init; } =
            new List<ProductInvoice>();
        public required Client Client { get; init; }
        public required Guid ClientId { get; init; }
        public required Company Company { get; init; }
        public required Guid CompanyId { get; init; }
    }

    // TODO - later, create a payments received table to track payments against invoices
}
