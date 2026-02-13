using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Invoicer.Infrastructure.StorageService;

public class StorageService(
    IMinioClient minioClient,
    IOptions<StorageServiceOptions> options) : IStorageService
{
    private readonly string _bucketName = options.Value.BucketName;

    public async Task<string> UploadFileAsync(Stream fileStream)
    {
        await EnsureBucketExistsAsync();

        var fileName = Guid.NewGuid();

        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName.ToString())
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType("application/octet-stream");

        await minioClient.PutObjectAsync(putArgs);

        return fileName.ToString();
    }

    public async Task<Stream> DownloadFileAsync(Guid fileName)
    {
        var memoryStream = new MemoryStream();

        var getArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName.ToString())
            .WithCallbackStream(async (stream, ct) =>
            {
                await stream.CopyToAsync(memoryStream, ct);
                memoryStream.Position = 0;
            });

        await minioClient.GetObjectAsync(getArgs);

        return memoryStream;
    }

    private async Task EnsureBucketExistsAsync()
    {
        var exists = await minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName));

        if (!exists)
        {
            await minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucketName));
        }
    }
}
