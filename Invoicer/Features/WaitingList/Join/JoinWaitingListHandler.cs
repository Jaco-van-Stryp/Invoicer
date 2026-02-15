using Invoicer.Domain.Data;
using Invoicer.Infrastructure.EmailValidationService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.WaitingList.Join;

public class JoinWaitingListHandler(AppDbContext _dbContext, IEmailValidationService _emailValidationService) : IRequestHandler<JoinWaitingListCommand>
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
    }
}