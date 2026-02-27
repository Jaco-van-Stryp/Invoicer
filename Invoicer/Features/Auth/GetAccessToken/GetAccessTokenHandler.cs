using System.Security.Cryptography;
using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using Invoicer.Infrastructure.EmailValidationService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Auth.GetAccessToken
{
    public class GetAccessTokenHandler(
        IEmailService _emailService,
        IEmailTemplateService _emailTemplateService,
        AppDbContext _dbContext,
        IEmailValidationService _emailValidationService
    ) : IRequestHandler<GetAccessTokenQuery, GetAccessTokenResponse>
    {
        public async Task<GetAccessTokenResponse> Handle(
            GetAccessTokenQuery request,
            CancellationToken cancellationToken
        )
        {
            var fakeResponse = new GetAccessTokenResponse(Guid.NewGuid());
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _dbContext
                .Users.Include(a => a.AuthTokens)
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

            // Create user if not exists (unified login/register flow)
            if (user == null)
            {
                var isValidEmail = await _emailValidationService.IsValidEmail(normalizedEmail);
                if (!isValidEmail)
                    return fakeResponse;

                user = new User
                {
                    Email = normalizedEmail,
                    AuthTokens = new List<AuthToken>(),
                    Companies = new List<Domain.Entities.Company>(),
                    LoginAttempts = 0,
                    IsLocked = false,
                    LockoutEnd = null,
                };

                try
                {
                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex)
                {
                    // Only swallow unique-email constraint violations (PostgreSQL error code 23505)
                    if (ex.InnerException is not Npgsql.PostgresException { SqlState: "23505" })
                        throw;

                    _dbContext.Entry(user).State = EntityState.Detached;
                    user = await _dbContext
                        .Users.Include(a => a.AuthTokens)
                        .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
                    if (user == null)
                        throw;
                }
            }

            // Check if user is locked out
            if (user.IsLocked)
            {
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                    return fakeResponse;

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
