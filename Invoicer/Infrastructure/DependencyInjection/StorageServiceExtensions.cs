using Invoicer.Infrastructure.StorageService;
using Minio;

namespace Invoicer.Infrastructure.DependencyInjection;

public static class StorageServiceExtensions
{
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var options =
            configuration.GetSection("MinIO").Get<StorageServiceOptions>()
            ?? new StorageServiceOptions();

        services.Configure<StorageServiceOptions>(configuration.GetSection("MinIO"));

        services.AddMinio(client =>
            client
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(false)
                .Build()
        );

        services.AddSingleton<IStorageService, StorageService.StorageService>();

        return services;
    }
}
