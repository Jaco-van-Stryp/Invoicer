namespace Invoicer.Domain.Entities
{
    public class AuthToken
    {
        public Guid Id { get; init; }
        public required string AccessToken { get; set; } = string.Empty;
        public required DateTime AccessTokenExpire { get; set; } = DateTime.UtcNow.AddMinutes(15);
        public required DateTime AccessTokenCreated { get; set; } = DateTime.UtcNow;
        public required bool Used { get; set; } = false;
        public required Guid UserId { get; init; }
        public required User User { get; init; }
    }
}
