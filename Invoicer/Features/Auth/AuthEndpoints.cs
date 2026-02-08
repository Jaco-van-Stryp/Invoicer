using Invoicer.Features.Auth.GetAccessToken;
using Invoicer.Features.Auth.Login;

namespace Invoicer.Features.Auth
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/auth").WithTags("Auth");

            // Access Token
            group.MapGetAccessTokenEndpoint();

            //Login
            group.MapLoginEndpoint();

            // Register

            return app;
        }
    }
}
