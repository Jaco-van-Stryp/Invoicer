using Invoicer.Domain.Data;
using Invoicer.Infrastructure.JWTTokenService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Auth.Login
{
    public class LoginHandler(IJwtTokenService _tokenService, AppDbContext _dbContext)
        : IRequestHandler<LoginCommand, LoginResponse>
    {
        public async Task<LoginResponse> Handle(
            LoginCommand request,
            CancellationToken cancellationToken
        )
        {
            var user = await _dbContext
                .Users.Include(x => x.AuthTokens)
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
            if (user == null)
            {
                throw new UnauthorizedException();
            }
            if (user.IsLocked)
            {
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    throw new UnauthorizedException();
                }

                user.IsLocked = false;
                user.LockoutEnd = null;
                user.LoginAttempts = 0;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var latestToken = user.AuthTokens.FirstOrDefault(x => x.Id == request.AccessTokenKey);
            if (
                latestToken != null
                && latestToken.AccessToken == request.AccessToken
                && latestToken.AccessTokenExpire > DateTime.UtcNow
                && !latestToken.Used
            )
            {
                user.LoginAttempts = 0;
                latestToken.Used = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
                var token = _tokenService.GenerateToken(user.Id, user.Email);
                return new LoginResponse(token);
            }

            user.LoginAttempts++;
            if (user.LoginAttempts >= 5)
            {
                user.IsLocked = true;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException();
        }
    }
}
