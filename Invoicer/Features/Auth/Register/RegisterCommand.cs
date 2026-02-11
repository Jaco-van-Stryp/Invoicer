using System.ComponentModel.DataAnnotations;
using Invoicer.Features.Auth.GetAccessToken;
using MediatR;

namespace Invoicer.Features.Auth.Register
{
    public readonly record struct RegisterCommand([property: Required, EmailAddress] string Email)
        : IRequest<RegisterResponse>;
}
