namespace Invoicer.Domain.Entities
{
    public class User
    {
        public Guid Id { get; init; }
        public required string Email { get; init; }
        public required ICollection<AuthToken> AuthTokens { get; init; } = new List<AuthToken>();
        public required ICollection<Company> Companies { get; init; } = new List<Company>();

        public required int LoginAttempts { get; set; } = 0;
        public required bool IsLocked { get; set; } = false;
        public required DateTime? LockoutEnd { get; set; } = null;
    }
}
