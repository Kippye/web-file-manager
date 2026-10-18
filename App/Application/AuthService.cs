using Application.Contracts;
using Domain.Identity;
using Infrastructure.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application;

/// <summary>
/// IAuthService implementation with logging.
/// <para>
/// DOES NOT log passwords or tokens.
/// </para>
/// DOES log email addresses to identify users who are not logged in.
/// </summary>
/// <param name="logger"></param>
/// <param name="userManager"></param>
/// <param name="signInManager"></param>
public class AuthService(IAppLogger<AuthService> logger, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) : IAuthService
{
    private static readonly EventId RegisterEvent = new(100, "User registered");
    private static readonly EventId LogInEvent = new(101, "User logged in");
    private static readonly EventId LogOutEvent = new(102, "User logged out");
    private static readonly EventId GenerateEmailTokenEvent = new(110, "Generated user email confirmation token");
    private static readonly EventId ConfirmEmailEvent = new(111, "Confirmed user email");
    private static readonly EventId GeneratePasswordResetTokenEvent = new(115, "Generated user password reset token");
    private static readonly EventId ResetPasswordEvent = new(116, "Reset user password");

    public bool SupportsUserEmail => userManager.SupportsUserEmail;
    public IdentityOptions Options { get => userManager.Options; set => userManager.Options = value; }

    public async Task<IdentityResult> CreateUserAsync(AppUser user, string password)
    {
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            // TODO: Maybe check what the error codes are
            logger.LogInformation(RegisterEvent, EventOutcome.Failure, "User failed to create a new account with password and email '{Email}'", user.Email);
        }
        else
        {
            logger.LogInformation(RegisterEvent, EventOutcome.Success, "User created a new account with password and email '{Email}'", user.Email);
        }
        return result;
    }

    public async Task<AppUser?> FindUserAsync(Guid userId)
    {
        return await userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<AppUser?> FindUserByEmailAsync(string email)
    {
        return await userManager.FindByEmailAsync(email);
    }

    public async Task<string> GetUserIdAsync(AppUser user)
    {
        return await userManager.GetUserIdAsync(user);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(AppUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        logger.LogInformation(GenerateEmailTokenEvent, EventOutcome.Success, "Generated email confirmation token for user email '{Email}'", user.Email);
        return token;
    }

    public async Task<bool> IsEmailConfirmedAsync(AppUser user)
    {
        return await userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token)
    {
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            logger.LogWarning(ConfirmEmailEvent, EventOutcome.Failure, "Failed to confirm user email '{Email}'", user.Email);
        }
        else
        {
            logger.LogInformation(ConfirmEmailEvent, EventOutcome.Success, "Confirmed user email '{Email}'", user.Email);
        }
        return result;
    }

    public async Task SignInAsync(AppUser user, bool isPersistent)
    {
        await signInManager.SignInAsync(user, isPersistent);

        // TODO: Should this be a separate event?
        logger.LogInformation(LogInEvent, EventOutcome.Success, "User logged in with email '{Email}'", user.Email);
    }

    public async Task<SignInResult> SignInAsync(string email, string password, bool rememberMe, bool lockoutOnFailure)
    {
        var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure);

        if (result.IsLockedOut)
        {
            logger.LogWarning(LogInEvent, EventOutcome.Denied, "User account locked out with email '{Email}'", email);
        }
        else if (!result.Succeeded)
        {
            logger.LogWarning(LogInEvent, EventOutcome.Failure, "User failed to log in with email '{Email}'", email);
        }
        else
        {
            logger.LogInformation(LogInEvent, EventOutcome.Success, "User logged in with email '{Email}'", email);
        }

        return result;
    }

    public async Task SignOutAsync()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation(LogOutEvent, EventOutcome.Success, "User logged out");
    }

    public async Task<string> GeneratePasswordResetTokenAsync(AppUser user)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        logger.LogInformation(GeneratePasswordResetTokenEvent, EventOutcome.Success, "Generated password reset token for user with email '{Email}'", user.Email);
        return token;
    }

    public async Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword)
    {
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning(ResetPasswordEvent, EventOutcome.Failure, "Failed to reset password for user with email '{Email}'", user.Email);
        }
        else
        {
            logger.LogInformation(ResetPasswordEvent, EventOutcome.Success, "Reset password for user with email '{Email}'", user.Email);
        }
        return result;
    }
}
