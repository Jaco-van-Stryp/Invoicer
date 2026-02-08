namespace Invoicer.Domain.Entities
{
    public class Client
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public required string Email { get; init; }
        public string? Address { get; init; }
        public string? TaxNumber { get; init; }
        public string? PhoneNumber { get; init; }
        public required Guid CompanyId { get; init; }
        public required Company Company { get; init; }
    }
}
