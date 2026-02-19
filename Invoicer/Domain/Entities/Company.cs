namespace Invoicer.Domain.Entities
{
    public class Company
    {
        public Guid Id { get; init; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string TaxNumber { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Email { get; set; }
        public required string PaymentDetails { get; set; }
        public required string LogoUrl { get; set; }
        public int NextInvoiceNumber { get; set; } = 1;
        public int NextEstimateNumber { get; set; } = 1;
        public required Guid UserId { get; init; }
        public required User User { get; init; } // In the future we could set up many to many here
        public ICollection<Product> Products { get; init; } = new List<Product>();
        public ICollection<Invoice> Invoices { get; init; } = new List<Invoice>();
        public ICollection<Estimate> Estimates { get; init; } = new List<Estimate>();
        public ICollection<Client> Clients { get; init; } = new List<Client>();
    }
}
