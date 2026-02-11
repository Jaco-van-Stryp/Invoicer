namespace Invoicer.Domain.Entities
{
    public class ProductInvoice
    {
        public Guid Id { get; init; }
        public required Guid ProductId { get; init; }
        public required Product Product { get; init; }
        public required Guid InvoiceId { get; init; }
        public required Invoice Invoice { get; init; }
        public required Guid CompanyId { get; init; }
        public required Company Company { get; init; }
        public required int Quantity { get; set; } = 0;
    }
}
