namespace Invoicer.Infrastructure.JWTTokenService
{
    public interface IJwtTokenService
    {
        string GenerateToken(Guid userId, string email);
    }
}
