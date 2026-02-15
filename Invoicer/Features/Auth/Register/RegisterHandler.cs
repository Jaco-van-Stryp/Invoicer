using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Features.Auth.GetAccessToken;
using Invoicer.Infrastructure.EmailValidationService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Auth.Register
{
    public class RegisterHandler(AppDbContext _dbContext, ISender _sender, IEmailValidationService _emailValidationService)
        : IRequestHandler<RegisterCommand, RegisterResponse>
    {
        public async Task<RegisterResponse> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken
        )
        {
            // TOOD - if joining from waiting list, account for that by updating the waiting list entry instead.

            var isValidEmail = await _emailValidationService.IsValidEmail(request.Email);
            if (!isValidEmail)
                return new RegisterResponse(Guid.NewGuid());

            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(
                x => x.Email == request.Email,
                cancellationToken
            );

            if (existingUser == null)
            {
                var user = new User
                {
                    Email = request.Email,
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
                catch (DbUpdateException)
                {
                    _dbContext.Entry(user).State = EntityState.Detached;
                    existingUser = await _dbContext.Users.FirstOrDefaultAsync(
                        x => x.Email == request.Email,
                        cancellationToken
                    );
                }
            }

            if (
                existingUser != null
                && existingUser.IsLocked
                && existingUser.LockoutEnd.HasValue
                && existingUser.LockoutEnd.Value > DateTime.UtcNow
            )
            {
                // User is locked — return success to avoid email enumeration, but don't send token
                return new RegisterResponse(Guid.NewGuid());
            }

            var accessTokenKey = await _sender.Send(
                new GetAccessTokenQuery(request.Email),
                cancellationToken
            );
            return new RegisterResponse(accessTokenKey.AccessTokenKey);
        }
    }
}
