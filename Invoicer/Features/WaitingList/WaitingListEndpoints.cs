using Invoicer.Features.WaitingList.Join;

namespace Invoicer.Features.WaitingList;

public static class WaitingListEndpoints
{
    public static IEndpointRouteBuilder MapWaitingListEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapJoinWaitingListEndpoint();
        return app;
    }
}
