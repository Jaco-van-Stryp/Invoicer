using Invoicer.Domain.Data;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using Invoicer.Infrastructure.EmailValidationService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.WaitingList.Join;

public class JoinWaitingListHandler(
    AppDbContext _dbContext,
    IEmailValidationService _emailValidationService,
    IEmailService _emailService,
    IEmailTemplateService _emailTemplateService) : IRequestHandler<JoinWaitingListCommand>
{
    public async Task Handle(JoinWaitingListCommand request, CancellationToken cancellationToken)
    {


        var isValidEmail = await _emailValidationService.IsValidEmail(request.Email);
        if (!isValidEmail) return;

        var waitingListUser = await _dbContext.WaitingList
                .FirstOrDefaultAsync(w => w.Email == request.Email, cancellationToken);

        if (waitingListUser is not null)
            return;

        var newWaitingListUser = new Domain.Entities.WaitingList
        {
            Email = request.Email,
        };

        _dbContext.WaitingList.Add(newWaitingListUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var body = _emailTemplateService.RenderTemplate(
            EmailTemplateName.WaitingListConfirmation,
            new Dictionary<string, string>
            {
                { "Email", request.Email },
                { "AppUrl", "https://invoicer.co.nz" },
            });

        await _emailService.SendEmailAsync(
            request.Email,
            "You're on the Invoicer waiting list! 🎉",
            body);
    }
}