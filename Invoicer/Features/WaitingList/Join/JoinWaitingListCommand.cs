using MediatR;

namespace Invoicer.Features.WaitingList.Join;

public readonly record struct JoinWaitingListCommand(string Email) : IRequest;
