namespace Invoicer.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; init; }
        public required string Name { get; set; }
        public required decimal Price { get; set; }
        public required string Description { get; set; }
        public string? ImageUrl { get; set; }
        public required Guid CompanyId { get; init; }
        public required Company Company { get; init; }
    }
}
