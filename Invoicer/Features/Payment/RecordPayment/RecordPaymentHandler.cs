using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Enums;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Payment.RecordPayment
{
    public class RecordPaymentHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService
    ) : IRequestHandler<RecordPaymentCommand, RecordPaymentResponse>
    {
        public async Task<RecordPaymentResponse> Handle(
            RecordPaymentCommand request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Products)
                            .ThenInclude(pi => pi.Product)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Payments)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Client)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                throw new UserNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            var invoice = company.Invoices.FirstOrDefault(i => i.Id == request.InvoiceId);
            if (invoice == null)
                throw new InvoiceNotFoundException();

            var payment = new Domain.Entities.Payment
            {
                Amount = request.Amount,
                PaidOn = DateTime.SpecifyKind(request.PaidOn, DateTimeKind.Utc),
                Notes = request.Notes,
                InvoiceId = invoice.Id,
                Invoice = invoice,
                CompanyId = request.CompanyId,
                Company = company,
            };

            invoice.Payments.Add(payment);

            var totalDue = invoice.Products.Sum(pi => pi.Product.Price * pi.Quantity);
            var totalPaid = invoice.Payments.Sum(p => p.Amount);
            var outstanding = totalDue - totalPaid;
            invoice.Status = Domain.Entities.Invoice.ComputeStatus(totalDue, totalPaid);

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await SendPaymentEmailsAsync(invoice, company, payment, outstanding, cancellationToken);

            return new RecordPaymentResponse(
                payment.Id,
                payment.Amount,
                payment.PaidOn,
                invoice.Status
            );
        }

        private async Task SendPaymentEmailsAsync(
            Domain.Entities.Invoice invoice,
            Domain.Entities.Company company,
            Domain.Entities.Payment payment,
            decimal outstanding,
            CancellationToken cancellationToken
        )
        {
            var invoiceLink = $"https://invoicer.co.nz/invoice/{invoice.Id}";
            var statusLabel = invoice.Status == InvoiceStatus.Paid ? "Paid"
                : invoice.Status == InvoiceStatus.Unpaid ? "Unpaid"
                : "Partial";
            var statusSection = BuildStatusSection(invoice.Status, outstanding);

            var sharedPlaceholders = new Dictionary<string, string>
            {
                { "ClientName", invoice.Client.Name },
                { "CompanyName", company.Name },
                { "InvoiceNumber", invoice.InvoiceNumber },
                { "PaymentAmount", $"{payment.Amount:C}" },
                { "PaymentDate", payment.PaidOn.ToString("MMMM dd, yyyy") },
                { "InvoiceStatus", statusLabel },
                { "InvoiceLink", invoiceLink },
                { "StatusSection", statusSection },
            };

            // Email to client
            var clientHtml = emailTemplateService.RenderTemplate(
                EmailTemplateName.PaymentReceived,
                sharedPlaceholders
            );

            await emailService.SendEmailAsync(
                invoice.Client.Email,
                $"Payment received for invoice {invoice.InvoiceNumber}",
                clientHtml
            );

            // Confirmation email to company owner
            var ownerHtml = emailTemplateService.RenderTemplate(
                EmailTemplateName.PaymentConfirmation,
                sharedPlaceholders
            );

            await emailService.SendEmailAsync(
                currentUserService.Email,
                $"Payment recorded — {invoice.InvoiceNumber} ({invoice.Client.Name})",
                ownerHtml
            );
        }

        private static string BuildStatusSection(InvoiceStatus status, decimal outstanding)
        {
            if (status == InvoiceStatus.Paid)
            {
                return """
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                      style="background-color:rgba(34,197,94,0.1);border-radius:10px;border:1px solid rgba(34,197,94,0.3);margin-bottom:24px;">
                      <tr>
                        <td style="padding:16px 20px;">
                          <p style="margin:0;font-size:14px;color:#22c55e;font-weight:500;">
                            &#10003; This invoice is now fully paid. Thank you!
                          </p>
                        </td>
                      </tr>
                    </table>
                    """;
            }

            return $"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                  style="background-color:rgba(251,191,36,0.1);border-radius:10px;border:1px solid rgba(251,191,36,0.3);margin-bottom:24px;">
                  <tr>
                    <td style="padding:16px 20px;">
                      <p style="margin:0;font-size:14px;color:#fbbf24;font-weight:500;">
                        Outstanding balance: {outstanding:C} — this invoice is partially paid.
                      </p>
                    </td>
                  </tr>
                </table>
                """;
        }
    }
}
