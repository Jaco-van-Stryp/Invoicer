using MediatR;

namespace Invoicer.Features.Auth.GetAccessToken
{
    public record GetAccessTokenQuery(string Email) : IRequest<GetAccessTokenResponse>;
}
