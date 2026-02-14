using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions;

public class ClientAlreadyExistsException()
    : ApiException(
        "The client you are trying to register already exists.",
        StatusCodes.Status409Conflict
    );
