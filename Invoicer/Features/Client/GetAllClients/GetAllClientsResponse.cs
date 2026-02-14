namespace Invoicer.Features.Client.GetAllClients
{
    public record struct GetAllClientsResponse(
        Guid Id,
        string Name,
        string Email,
        string? Address,
        string? TaxNumber,
        string? PhoneNumber
    );
}
