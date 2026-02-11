using System.Security.Claims;

namespace Invoicer.Infrastructure.CurrentUserService
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string Email =>
            _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        public Guid UserId
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
                    throw new UnauthorizedAccessException("User is not authenticated.");
                return userId;
            }
        }
    }
}
