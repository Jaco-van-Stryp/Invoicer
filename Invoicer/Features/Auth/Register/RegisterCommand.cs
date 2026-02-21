using System.ComponentModel.DataAnnotations;
using Invoicer.Features.Auth.GetAccessToken;
using MediatR;

namespace Invoicer.Features.Auth.Register
{
    public readonly record struct RegisterCommand(string Email) : IRequest<RegisterResponse>;
}
