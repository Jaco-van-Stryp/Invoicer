namespace Invoicer.Features.Client.UpdateClient
{
    public readonly record struct UpdateClientCommand(
        Guid CompanyId,
        Guid ClientId,
        string? Name,
        string? Email,
        string? Address,
        string? TaxNumber,
        string? PhoneNumber
    ) : MediatR.IRequest;
}
