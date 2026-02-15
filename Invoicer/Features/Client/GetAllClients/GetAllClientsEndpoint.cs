using MediatR;

namespace Invoicer.Features.Client.GetAllClients
{
    public static class GetAllClientsEndpoint
    {
        public static IEndpointRouteBuilder MapGetAllClientsEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "get-all-clients/{CompanyId}",
                    async (Guid CompanyId, ISender sender) =>
                    {
                        var query = new GetAllClientsQuery(CompanyId);
                        var result = await sender.Send(query);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("GetAllClients")
                .RequireAuthorization();
            return app;
        }
    }
}
