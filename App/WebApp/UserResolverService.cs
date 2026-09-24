using Infrastructure.Contracts;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace WebApp;

public class UserResolverService : IUserResolverService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ClaimsPrincipal? CurrentUser => _httpContextAccessor.HttpContext?.User;

    public UserResolverService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUserGuid(ClaimsPrincipal user)
    {
        if (user is null) { return null; }
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || !Guid.TryParse(userId, out var userGuid)) { return null; }
        return userGuid;
    }

    public Guid? GetCurrentUserGuid()
    {
        if (CurrentUser is null) { return null; }

        return GetUserGuid(CurrentUser);
    }
}
