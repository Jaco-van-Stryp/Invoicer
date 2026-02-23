using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        // Calculate total amount
        var totalAmount = invoice.Products.Sum(pi => pi.Quantity * pi.Product.Price);

        // Prepare placeholders for template
        var placeholders = new Dictionary<string, string>
        {
            { "ClientName", invoice.Client.Name },
            { "CompanyName", invoice.Company.Name },
            { "InvoiceNumber", invoice.InvoiceNumber },
            { "InvoiceDate", invoice.InvoiceDate.ToString("MMMM dd, yyyy") },
            { "InvoiceDueDate", invoice.InvoiceDue.ToString("MMMM dd, yyyy") },
            { "TotalAmount", $"{totalAmount:C}" },
            { "InvoiceLink", $"https://invoicer.co.nz/invoice/{invoice.Id}" },
        };

        // Render the email template
        var htmlBody = emailTemplateService.RenderTemplate(
            EmailTemplateName.SendInvoiceEmail,
            placeholders
        );

        // Send the email
        var subject = $"Invoice {invoice.InvoiceNumber} from {invoice.Company.Name}";
        await emailService.SendEmailAsync(invoice.Client.Email, subject, htmlBody);
    }
}
