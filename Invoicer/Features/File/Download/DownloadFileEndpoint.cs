using System;
using MediatR;

namespace Invoicer.Features.File.Download;

public static class DownloadFileEndpoint
{
    public static IEndpointRouteBuilder MapDownloadFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "download/{filename:guid}",
                async (Guid filename, ISender sender) =>
                {
                    var query = new DownloadFileQuery(filename);
                    var result = await sender.Send(query);
                    return TypedResults.File(result, "application/octet-stream");
                }
            )
            .WithName("DownloadFile");
        return app;
    }
}
