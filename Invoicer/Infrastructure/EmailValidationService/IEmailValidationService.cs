namespace Invoicer.Infrastructure.EmailValidationService;

public interface IEmailValidationService
{
    Task<bool> IsValidEmail(string email);
}
