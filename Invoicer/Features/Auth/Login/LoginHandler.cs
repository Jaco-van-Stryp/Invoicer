using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using Invoicer.Infrastructure.JWTTokenService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Auth.Login
{
    public class LoginHandler(
        IJwtTokenService _tokenService,
        AppDbContext _dbContext,
        IEmailService _emailService,
        IEmailTemplateService _emailTemplateService
    ) : IRequestHandler<LoginCommand, LoginResponse>
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
                throw new UserNotFoundException();
            }
            if (user.IsLocked)
            {
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    throw new InvalidCredentialsException();
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
                user.LoginAttempts = 0;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                var htmlBody = _emailTemplateService.RenderTemplate(
                    EmailTemplateName.AccountLockedOut,
                    new Dictionary<string, string> { ["LockoutMinutes"] = "15" }
                );
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Security Alert - Account Locked.",
                    htmlBody
                );
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }
    }
}
