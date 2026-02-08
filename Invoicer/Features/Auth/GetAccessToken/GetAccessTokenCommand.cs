namespace Invoicer.Features.Auth.GetAccessToken
{
    public record GetAccessTokenCommand(string Email) : MediatR.IRequest;
}
