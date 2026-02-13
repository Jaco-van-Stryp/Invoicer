using MediatR;

namespace Invoicer.Features.File.Download;

public readonly record struct DownloadFileQuery(Guid Filename) : IRequest<Stream>;
