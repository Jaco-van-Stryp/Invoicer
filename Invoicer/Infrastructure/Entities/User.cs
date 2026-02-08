namespace Invoicer.Infrastructure.Entities
{
    public class User
    {
        public required Guid Id { get; init; }
        public required string Email { get; init; }
        public required ICollection<AuthToken> AuthTokens { get; init; } = new List<AuthToken>();
        public required ICollection<Company> Companies { get; init; } = new List<Company>();
    }
}
