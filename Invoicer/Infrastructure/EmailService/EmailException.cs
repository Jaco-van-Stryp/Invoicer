using Invoicer.Infrastructure.ExceptionHandling;

namespace Invoicer.Infrastructure.EmailService
{
    public class EmailException()
        : ApiException(
            "Something went wrong while sending you an email, please try again later.",
            StatusCodes.Status503ServiceUnavailable
        );
}
