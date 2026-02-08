namespace Invoicer.Features.Auth.Login
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base("Invalid email or password.") { }
    }
}
