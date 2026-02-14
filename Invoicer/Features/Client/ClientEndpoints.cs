using Invoicer.Features.Client.CreateClient;
using Invoicer.Features.Client.DeleteClient;
using Invoicer.Features.Client.GetAllClients;
using Invoicer.Features.Client.UpdateClient;

namespace Invoicer.Features.Client;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/client").WithTags("Client");

        group.MapCreateClientEndpoint();
        group.MapGetAllClientsEndpoint();
        group.MapUpdateClientEndpoint();
        group.MapDeleteClientEndpoint();

        return app;
    }
}
