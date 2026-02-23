namespace Invoicer.Features.Invoice.GetPublicInvoice;

public record struct GetPublicInvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    string Status,
    decimal TotalAmount,
    string? Notes,
    CompanyInfo Company,
    ClientInfo Client,
    List<ProductInfo> Products
);

public record struct CompanyInfo(
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    string? LogoUrl
);

public record struct ClientInfo(string Name, string? Email, string? Phone, string? Address);

public record struct ProductInfo(string Name, int Quantity, decimal UnitPrice, decimal TotalPrice);
