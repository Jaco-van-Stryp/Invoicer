namespace Invoicer.Features.Client.DeleteClient
{
    public readonly record struct DeleteClientCommand(Guid CompanyId, Guid ClientId)
        : MediatR.IRequest;
}
