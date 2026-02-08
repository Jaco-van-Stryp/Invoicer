namespace Invoicer.Infrastructure.Entities
{
    public class Product
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required decimal Price { get; init; }
        public required string Description { get; init; }
        public string? ImageUrl { get; init; }
        public required Guid CompanyId { get; init; }
        public required Company Company { get; init; }
    }
}
