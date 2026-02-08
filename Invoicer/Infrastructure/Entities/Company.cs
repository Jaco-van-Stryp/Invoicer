namespace Invoicer.Infrastructure.Entities
{
    public class Company
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required string Address { get; init; }
        public required string TaxNumber { get; init; }
        public required string PhoneNumber { get; init; }
        public required string Email { get; init; }
        public required string PaymentDetails { get; init; }
        public required string LogoUrl { get; init; }
        public required Guid UserId { get; init; }
        public required User User { get; init; } // In the future we could set up many to many here
    }
}
