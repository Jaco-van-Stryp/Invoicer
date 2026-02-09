using MediatR;

namespace Invoicer.Features.Auth.Login
{
    public record LoginCommand(string Email, string AccessToken, Guid AccessTokenKey)
        : IRequest<LoginResponse>;
}
