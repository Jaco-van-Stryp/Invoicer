using MediatR;

namespace Invoicer.Features.Auth.Login
{
    public readonly record struct LoginCommand(
        string Email,
        string AccessToken,
        Guid AccessTokenKey
    ) : IRequest<LoginResponse>;
}
