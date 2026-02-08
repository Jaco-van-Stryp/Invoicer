namespace Invoicer.Infrastructure.Entities
{
    public class AuthToken
    {
        public required Guid Id { get; init; }
        public required string AccessToken { get; set; } = string.Empty;
        public required DateTime AccessTokenExpire { get; set; } = DateTime.UtcNow.AddMinutes(15);

        public required Guid UserId { get; init; }
        public required User User { get; init; }
    }
}
