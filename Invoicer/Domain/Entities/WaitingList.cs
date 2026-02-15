namespace Invoicer.Domain.Entities
{
    public class WaitingList
    {
        public Guid Id { get; init; }
        public required string Email { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public bool Joined { get; set; } = false;
        public DateTime? JoinedAt { get; set; } = null;
        public int TotalEmailsSent { get; set; } = 0;
        public DateTime? LastEmailSentAt { get; set; } = DateTime.UtcNow;
    }
}
