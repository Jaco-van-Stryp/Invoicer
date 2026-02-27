namespace Invoicer.Domain.Entities
{
    public class Client
    {
        public Guid Id { get; init; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public required Guid CompanyId { get; init; }
        public required Company Company { get; init; }
        public bool IsDeleted { get; set; } = false;
    }
}
