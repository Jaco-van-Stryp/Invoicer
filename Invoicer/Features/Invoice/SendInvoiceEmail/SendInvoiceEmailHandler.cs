using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using Invoicer.Infrastructure.EmailService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Invoice.SendInvoiceEmail;

public class SendInvoiceEmailHandler(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IEmailService emailService
) : IRequestHandler<SendInvoiceEmailCommand>
{
    public async Task Handle(SendInvoiceEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var user = await dbContext
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

        var invoice = await dbContext
            .Invoices.Include(i => i.Client)
            .Include(i => i.Company)
            .FirstOrDefaultAsync(
                i => i.Id == request.InvoiceId && i.CompanyId == request.CompanyId,
                cancellationToken
            );

        if (invoice is null)
        {
            throw new InvoiceNotFoundException();
        }

        var invoiceUrl = $"https://localhost:4200/invoice/{invoice.Id}";

        var subject = $"Invoice {invoice.InvoiceNumber} from {invoice.Company.Name}";
        var body = $@"
            <h2>New Invoice from {invoice.Company.Name}</h2>
            <p>Hello {invoice.Client.Name},</p>
            <p>You have received a new invoice.</p>
            <p><strong>Invoice Number:</strong> {invoice.InvoiceNumber}</p>
            <p><strong>Invoice Date:</strong> {invoice.InvoiceDate:MMMM dd, yyyy}</p>
            <p><strong>Due Date:</strong> {invoice.InvoiceDue:MMMM dd, yyyy}</p>
            <p>
                <a href='{invoiceUrl}' style='display: inline-block; padding: 12px 24px; background-color: #6366f1; color: white; text-decoration: none; border-radius: 6px; font-weight: 600;'>
                    View Invoice
                </a>
            </p>
            <p>Thank you for your business!</p>
            <hr>
            <p style='color: #6b7280; font-size: 0.875rem;'>{invoice.Company.Name}<br>{invoice.Company.Address}<br>{invoice.Company.Email}</p>
        ";

        await emailService.SendEmailAsync(invoice.Client.Email, subject, body);
    }
}
