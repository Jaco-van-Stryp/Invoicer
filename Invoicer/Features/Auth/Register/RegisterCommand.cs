using Invoicer.Features.Auth.GetAccessToken;
using MediatR;

namespace Invoicer.Features.Auth.Register
{
    public record RegisterCommand(string Email) : IRequest<RegisterResponse>;
}
