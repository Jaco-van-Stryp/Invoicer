using Invoicer.Infrastructure.StorageService;

namespace Invoicer.Features.File.Download;

public static class DownloadFileEndpoint
{
    public static IEndpointRouteBuilder MapDownloadFileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "download/{filename:guid}",
                async (Guid filename, IStorageService storageService) =>
                {
                    var result = await storageService.DownloadFileAsync(filename);
                    return TypedResults.File(result, "application/octet-stream");
                }
            )
            .WithName("DownloadFile")
            .Produces<byte[]>(contentType: "application/octet-stream");
        return app;
    }
}
