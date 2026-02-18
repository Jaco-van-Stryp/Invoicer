namespace Invoicer.Domain.Entities
{
    public enum InvoiceStatus
    {
        Unpaid = 0,
        Partial = 1,
        Paid = 2,
    }

    public class Invoice
    {
        public Guid Id { get; init; }
        public required string InvoiceNumber { get; set; }
        public required DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public required DateTime InvoiceDue { get; set; } = DateTime.UtcNow.AddDays(30);
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
        public required ICollection<ProductInvoice> Products { get; init; } =
            new List<ProductInvoice>();
        public ICollection<Payment> Payments { get; init; } = [];
        public required Client Client { get; set; }
        public required Guid ClientId { get; set; }
        public required Company Company { get; init; }
        public required Guid CompanyId { get; init; }

        public static InvoiceStatus ComputeStatus(decimal totalDue, decimal totalPaid)
        {
            if (totalPaid <= 0)
                return InvoiceStatus.Unpaid;
            if (totalPaid >= totalDue)
                return InvoiceStatus.Paid;
            return InvoiceStatus.Partial;
        }
    }
}
