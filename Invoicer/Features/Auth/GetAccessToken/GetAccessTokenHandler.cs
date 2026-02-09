using System.Security.Cryptography;
using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Auth.GetAccessToken
{
    public class GetAccessTokenHandler(
        IEmailService _emailService,
        IEmailTemplateService _emailTemplateService,
        AppDbContext _dbContext
    ) : IRequestHandler<GetAccessTokenQuery, GetAccessTokenResponse>
    {
        public async Task<GetAccessTokenResponse> Handle(
            GetAccessTokenQuery request,
            CancellationToken cancellationToken
        )
        {
            var user = await _dbContext
                .Users.Include(a => a.AuthTokens)
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
            var fakeResponse = new GetAccessTokenResponse(Guid.NewGuid());

            // Don't reveal if the email exists or not
            if (user == null)
                return fakeResponse;

            // Check if user is currently locked out
            if (user.IsLocked)
            {
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    return fakeResponse;
                }

                // Lockout expired — reset
                user.IsLocked = false;
                user.LockoutEnd = null;
                user.LoginAttempts = 0;
            }

            // Each token request counts as a login attempt
            user.LoginAttempts++;

            if (user.LoginAttempts >= 5)
            {
                user.IsLocked = true;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return fakeResponse;
            }

            // Invalidate all existing unused tokens
            foreach (var token in user.AuthTokens.Where(t => !t.Used))
            {
                token.Used = true;
            }

            var secureCode = GenerateAccessToken();

            var authToken = new AuthToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AccessToken = secureCode,
                AccessTokenExpire = DateTime.UtcNow.AddMinutes(15),
                User = user,
                Used = false,
                AccessTokenCreated = DateTime.UtcNow,
            };
            await _dbContext.AuthTokens.AddAsync(authToken, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var htmlBody = _emailTemplateService.RenderTemplate(
                    EmailTemplateName.AccessToken,
                    new Dictionary<string, string> { ["AccessToken"] = secureCode }
                );

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Invoicer - Your Access Code",
                    htmlBody
                );
            }
            catch
            {
                _dbContext.AuthTokens.Remove(authToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                throw;
            }

            return new GetAccessTokenResponse(authToken.Id);
        }

        private string GenerateAccessToken()
        {
            int secureCode = RandomNumberGenerator.GetInt32(100000, 1000000);
            return secureCode.ToString();
        }
    }
}
