using MediatR;

namespace Invoicer.Features.Auth.GetAccessToken
{
    public readonly record struct GetAccessTokenQuery(string Email)
        : IRequest<GetAccessTokenResponse>;
}
