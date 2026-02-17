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
        public required Client Client { get; set; }
        public required Guid ClientId { get; set; }
        public required Company Company { get; init; }
        public required Guid CompanyId { get; init; }
    }

    // TODO - later, create a payments received table to track payments against invoices
    // TODO - Invoices should have a status which shows if the clicent saw the invoice, and reminders, overdue trackers, etc. 
    // TODO - Status to show if it's paid or not, etc. figure it out later
}
