namespace Invoicer.Features.Invoice.GetPublicInvoice;

public record GetPublicInvoiceResponse(
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

public record CompanyInfo(
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    string? LogoUrl
);

public record ClientInfo(string Name, string? Email, string? Phone, string? Address);

public record ProductInfo(string Name, int Quantity, decimal UnitPrice, decimal TotalPrice);
