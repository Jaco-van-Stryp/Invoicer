using Invoicer.Features.Auth.Login;
using MediatR;

namespace Invoicer.Features.Auth.Register
{
    public static class RegisterEndpoint
    {
        public static IEndpointRouteBuilder MapRegisterEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "Register",
                    async (RegisterCommand command, ISender sender) =>
                    {
                        var result = await sender.Send(command);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("Register");
            return app;
        }
    }
}
