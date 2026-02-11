using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Domain.Exceptions
{
    public class UserNotFoundException()
        : ApiException(
            "You do not have permission to perform this action.",
            StatusCodes.Status401Unauthorized
        ); // Unauthorized, because if the user does not exist, they should not be able to perform the action.
}
