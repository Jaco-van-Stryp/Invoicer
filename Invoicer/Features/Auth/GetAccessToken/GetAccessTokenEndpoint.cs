using MediatR;

namespace Invoicer.Features.Auth.GetAccessToken
{
    public static class GetAccessTokenEndpoint
    {
        public static IEndpointRouteBuilder MapGetAccessTokenEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapPost(
                    "GetAccessToken",
                    async (GetAccessTokenQuery query, ISender sender) =>
                    {
                        return TypedResults.Ok(await sender.Send(query));
                    }
                )
                .WithName("GetAccessToken");
            return app;
        }
    }
}
