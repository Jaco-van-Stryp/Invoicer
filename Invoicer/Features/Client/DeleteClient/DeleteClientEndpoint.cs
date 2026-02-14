using MediatR;

namespace Invoicer.Features.Client.DeleteClient
{
    public static class DeleteClientEndpoint
    {
        public static IEndpointRouteBuilder MapDeleteClientEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "delete-client/{CompanyId}/{ClientId}",
                    async (Guid CompanyId, Guid ClientId, ISender sender) =>
                    {
                        var command = new DeleteClientCommand(CompanyId, ClientId);
                        await sender.Send(command);
                        return Results.NoContent();
                    }
                )
                .WithName("DeleteClient")
                .RequireAuthorization();
            return app;
        }
    }
}
