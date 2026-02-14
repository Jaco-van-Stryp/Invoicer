using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Invoicer.Infrastructure.StorageService;

public class StorageService(IMinioClient minioClient, IOptions<StorageServiceOptions> options)
    : IStorageService
{
    private readonly string _bucketName = options.Value.BucketName;
    private readonly SemaphoreSlim _bucketInitLock = new(1, 1);
    private bool _bucketInitialized;

    public async Task<string> UploadFileAsync(Stream fileStream)
    {
        await EnsureBucketExistsAsync();

        var fileName = Guid.NewGuid();

        Stream streamToUpload = fileStream;
        MemoryStream? bufferedStream = null;

        try
        {
            if (!fileStream.CanSeek)
            {
                bufferedStream = new MemoryStream();
                await fileStream.CopyToAsync(bufferedStream);
                bufferedStream.Position = 0;
                streamToUpload = bufferedStream;
            }

            var putArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName.ToString())
                .WithStreamData(streamToUpload)
                .WithObjectSize(streamToUpload.Length)
                .WithContentType("application/octet-stream");

            await minioClient.PutObjectAsync(putArgs);

            return fileName.ToString();
        }
        finally
        {
            if (bufferedStream != null)
                await bufferedStream.DisposeAsync();
        }
    }

    public async Task<Stream> DownloadFileAsync(Guid fileName)
    {
        var memoryStream = new MemoryStream();

        var getArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName.ToString())
            .WithCallbackStream(
                async (stream, ct) =>
                {
                    await stream.CopyToAsync(memoryStream, ct);
                    memoryStream.Position = 0;
                }
            );

        await minioClient.GetObjectAsync(getArgs);

        return memoryStream;
    }

    private async Task EnsureBucketExistsAsync()
    {
        if (_bucketInitialized)
            return;

        await _bucketInitLock.WaitAsync();
        try
        {
            if (_bucketInitialized)
                return;

            var exists = await minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName)
            );

            if (!exists)
            {
                try
                {
                    await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                }
                catch (MinioException ex)
                    when (ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
                {
                    // Another caller created the bucket concurrently; safe to ignore.
                }
            }

            _bucketInitialized = true;
        }
        finally
        {
            _bucketInitLock.Release();
        }
    }
}
