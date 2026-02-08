using MediatR;

namespace Invoicer.Features.Auth.GetAccessToken
{
    public static class GetAccessTokenEndpoint
    {
        public static IEndpointRouteBuilder MapGetAccessTokenEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapGet(
                    "GetAccessToken",
                    async (GetAccessTokenCommand command, ISender sender) =>
                    {
                        await sender.Send(command);
                        return TypedResults.Ok();
                    }
                )
                .WithTags("GetAccessToken");
            return app;
        }
    }
}
