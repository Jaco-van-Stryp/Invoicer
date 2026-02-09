using MediatR;

namespace Invoicer.Features.Auth.Login
{
    public static class LoginEndpoint
    {
        public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "login",
                    async (LoginCommand command, ISender sender) =>
                    {
                        var result = await sender.Send(command);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("Login");
            return app;
        }
    }
}
