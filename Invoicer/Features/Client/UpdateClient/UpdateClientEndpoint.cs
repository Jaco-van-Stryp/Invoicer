using MediatR;

namespace Invoicer.Features.Client.UpdateClient
{
    public static class UpdateClientEndpoint
    {
        public static IEndpointRouteBuilder MapUpdateClientEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPatch(
                    "update-client",
                    async (UpdateClientCommand command, ISender sender) =>
                    {
                        await sender.Send(command);
                        return TypedResults.Ok();
                    }
                )
                .WithName("UpdateClient")
                .RequireAuthorization();
            return app;
        }
    }
}
