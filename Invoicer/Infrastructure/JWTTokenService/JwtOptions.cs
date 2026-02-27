namespace Invoicer.Infrastructure.JWTTokenService
{
    public class JwtOptions
    {
        public required string Secret { get; set; }

        public string Issuer { get; set; } = "Invoicer";

        public string Audience { get; set; } = "InvoicerClient";

        public int ExpirationMinutes { get; set; } = 525600;
    }
}
