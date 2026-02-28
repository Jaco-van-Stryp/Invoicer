using Invoicer.Infrastructure.StorageService;
using Minio.Exceptions;

namespace Invoicer.Features.File.Download;

public static class DownloadFileEndpoint
{
    public static IEndpointRouteBuilder MapDownloadFileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "download/{filename:guid}",
                async (Guid filename, IStorageService storageService) =>
                {
                    try
                    {
                        var result = await storageService.DownloadFileAsync(filename);
                        return Results.File(result, "application/octet-stream");
                    }
                    catch (ObjectNotFoundException)
                    {
                        return Results.NotFound();
                    }
                }
            )
            .WithName("DownloadFile")
            .Produces<byte[]>(contentType: "application/octet-stream")
            .RequireAuthorization();
        return app;
    }
}
