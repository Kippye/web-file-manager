using Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Application.Contracts;

// TODO: Some auth operations like session management don't go through this logging.

/// <summary>
/// Wrapper around UserManager and SignInManager (maybe others) to add consistent logging.
/// </summary>
public interface IAuthService
{
    bool SupportsUserEmail { get; }
    IdentityOptions Options { get; protected set; }

    Task<IdentityResult> CreateUserAsync(AppUser user, string password);

    Task<AppUser?> FindUserAsync(Guid userId);
    Task<AppUser?> FindUserByEmailAsync(string email);

    Task<string> GetUserIdAsync(AppUser user);

    Task<string> GenerateEmailConfirmationTokenAsync(AppUser user);
    Task<bool> IsEmailConfirmedAsync(AppUser user);
    Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token);

    Task SignInAsync(AppUser user, bool isPersistent);
    Task<SignInResult> SignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure);
    Task SignOutAsync();

    Task<string> GeneratePasswordResetTokenAsync(AppUser user);
    Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword);

}
