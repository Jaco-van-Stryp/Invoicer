namespace Invoicer.Infrastructure.CurrentUserService
{
    public interface ICurrentUserService
    {
        string Email { get; }
        Guid UserId { get; }
    }
}
