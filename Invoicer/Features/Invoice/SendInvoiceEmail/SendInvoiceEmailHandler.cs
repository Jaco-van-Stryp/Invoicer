using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;

namespace Invoicer.Features.Invoice.SendInvoiceEmail;

public class SendInvoiceEmailHandler(
    AppDbContext _dbContext,
    ICurrentUserService currentUserService,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService
) : IRequestHandler<SendInvoiceEmailCommand>
{
    public async Task Handle(SendInvoiceEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var user = await _dbContext
            .Users.Include(u => u.Companies)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new UserNotFoundException();
        }

        var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
        if (company is null)
        {
            throw new CompanyNotFoundException();
        }

        var invoice = await _dbContext
            .Invoices.Include(i => i.Client)
            .Include(i => i.Company)
            .Include(i => i.Products)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(
                i => i.Id == request.InvoiceId && i.CompanyId == request.CompanyId,
                cancellationToken
            );

        if (invoice is null)
        {
            throw new InvoiceNotFoundException();
        }

        var totalAmount = invoice.Products.Sum(pi => pi.Quantity * pi.Product.Price);
        var invoiceLink = $"https://invoicer.co.nz/invoice/{invoice.Id}";

        // --- Email to client ---
        var clientPlaceholders = new Dictionary<string, string>
        {
            { "ClientName", invoice.Client.Name },
            { "CompanyName", invoice.Company.Name },
            { "InvoiceNumber", invoice.InvoiceNumber },
            { "InvoiceDate", invoice.InvoiceDate.ToString("MMMM dd, yyyy") },
            { "InvoiceDueDate", invoice.InvoiceDue.ToString("MMMM dd, yyyy") },
            { "TotalAmount", $"{totalAmount:C}" },
            { "InvoiceLink", invoiceLink },
        };

        var clientHtml = emailTemplateService.RenderTemplate(
            EmailTemplateName.SendInvoiceEmail,
            clientPlaceholders
        );

        await emailService.SendEmailAsync(
            invoice.Client.Email,
            $"Invoice {invoice.InvoiceNumber} from {invoice.Company.Name}",
            clientHtml
        );

        // --- Confirmation email to company owner ---
        var ownerPlaceholders = new Dictionary<string, string>
        {
            { "ClientName", invoice.Client.Name },
            { "CompanyName", invoice.Company.Name },
            { "InvoiceNumber", invoice.InvoiceNumber },
            { "InvoiceDate", invoice.InvoiceDate.ToString("MMMM dd, yyyy") },
            { "InvoiceDueDate", invoice.InvoiceDue.ToString("MMMM dd, yyyy") },
            { "TotalAmount", $"{totalAmount:C}" },
            { "InvoiceLineItems", BuildLineItemsHtml(invoice.Products) },
        };

        var ownerHtml = emailTemplateService.RenderTemplate(
            EmailTemplateName.InvoiceSent,
            ownerPlaceholders
        );

        await emailService.SendEmailAsync(
            currentUserService.Email,
            $"Invoice {invoice.InvoiceNumber} sent to {invoice.Client.Name}",
            ownerHtml
        );
    }

    private static string BuildLineItemsHtml(
        IEnumerable<ProductInvoice> products
    )
    {
        var sb = new StringBuilder();
        sb.Append(
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;\">"
        );
        sb.Append("<tr style=\"border-bottom:1px solid rgba(255,255,255,0.15);\">");
        sb.Append(
            "<th style=\"text-align:left;padding:6px 4px;font-size:12px;color:#6b7280;text-transform:uppercase;letter-spacing:1px;\">Item</th>"
        );
        sb.Append(
            "<th style=\"text-align:center;padding:6px 4px;font-size:12px;color:#6b7280;text-transform:uppercase;letter-spacing:1px;\">Qty</th>"
        );
        sb.Append(
            "<th style=\"text-align:right;padding:6px 4px;font-size:12px;color:#6b7280;text-transform:uppercase;letter-spacing:1px;\">Total</th>"
        );
        sb.Append("</tr>");

        decimal grandTotal = 0;
        foreach (var pi in products)
        {
            var lineTotal = pi.Quantity * pi.Product.Price;
            grandTotal += lineTotal;
            sb.Append("<tr style=\"border-bottom:1px solid rgba(255,255,255,0.06);\">");
            sb.Append(
                $"<td style=\"padding:8px 4px;font-size:14px;color:#d1d5db;\">{WebUtility.HtmlEncode(pi.Product.Name)}</td>"
            );
            sb.Append(
                $"<td style=\"text-align:center;padding:8px 4px;font-size:14px;color:#9ca3af;\">{pi.Quantity}</td>"
            );
            sb.Append(
                $"<td style=\"text-align:right;padding:8px 4px;font-size:14px;color:#d1d5db;\">{lineTotal:C}</td>"
            );
            sb.Append("</tr>");
        }

        sb.Append("<tr>");
        sb.Append(
            "<td colspan=\"2\" style=\"padding:10px 4px;font-size:14px;font-weight:600;color:#f3f4f6;text-align:right;\">Total</td>"
        );
        sb.Append(
            $"<td style=\"text-align:right;padding:10px 4px;font-size:16px;font-weight:700;color:#a855f7;\">{grandTotal:C}</td>"
        );
        sb.Append("</tr>");
        sb.Append("</table>");

        return sb.ToString();
    }
}
