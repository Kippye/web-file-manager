using System.Security.Claims;

namespace Infrastructure.Contracts;

/// <summary>
/// Provides access to current user's information.
/// </summary>
public interface IUserResolverService
{
    /// <summary>
    /// The current user's claims principal.
    /// </summary>
    public ClaimsPrincipal? CurrentUser { get; }
    /// <summary>
    /// Parse the user's Guid from a ClaimsPrincipal.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Guid? GetUserGuid(ClaimsPrincipal user);
    /// <summary>
    /// Shortcut for parsing the current user's Guid.
    /// </summary>
    /// <returns></returns>
    Guid? GetCurrentUserGuid();
}