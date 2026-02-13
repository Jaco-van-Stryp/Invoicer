
using MediatR;

namespace Invoicer.Features.File.Upload;

public readonly record struct UploadFileCommand(Stream FileStream) : IRequest<string>;