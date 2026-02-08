using System.Security.Cryptography;
using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Infrastructure.EmailService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Auth.GetAccessToken
{
    public class GetAccessTokenHandler(IEmailService _emailService, AppDbContext _dbContext)
        : IRequestHandler<GetAccessTokenCommand>
    {
        public async Task Handle(GetAccessTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _dbContext
                .Users.Include(a => a.AuthTokens)
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
            // Don't reveal if the email exists or not, just send the email if it does and return success either way
            if (user == null)
                return;

            var secureCode = GenerateAccessToken();

            // Check if existing token exists for the user and is not expired
            var existingToken = user.AuthTokens.FirstOrDefault(x =>
                !x.Used && x.AccessTokenExpire > DateTime.UtcNow
            );
            if (existingToken != null)
            {
                return;
            }

            var authToken = new AuthToken
            {
                UserId = user.Id,
                AccessToken = secureCode,
                AccessTokenExpire = DateTime.UtcNow.AddMinutes(15),
                User = user,
                Used = false,
                AccessTokenCreated = DateTime.UtcNow,
            };
            user.AuthTokens.Add(authToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // TODO - Use a proper email template
            await _emailService.SendEmailAsync(
                user.Email,
                "Invoicer - Your Access Token",
                $"Your access token is: {secureCode}. Please enter it in the application to login"
            );
        }

        private string GenerateAccessToken()
        {
            int secureCode = RandomNumberGenerator.GetInt32(100000, 1000000);
            return secureCode.ToString();
        }
    }
}
