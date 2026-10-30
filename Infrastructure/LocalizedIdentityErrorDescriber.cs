using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace WebShop.Infrastructure;

/// <summary>
/// ASP.NET Identity reports problems in English only ("Passwords must have at
/// least one digit"). This routes the ones a shopper can actually meet through
/// the shop's own resources. Anything not overridden keeps Identity's text.
/// </summary>
public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer<SharedResource> _l;

    public LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer) => _l = localizer;

    private IdentityError Error(string code, string key, params object[] args) =>
        new() { Code = code, Description = _l[key, args].Value };

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), "Identity_DuplicateEmail");

    // Usernames are email addresses here, so a taken username and a taken
    // email are the same event. Same wording, so the page does not say it twice.
    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), "Identity_DuplicateEmail");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), "Validation_Email");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), "Identity_PasswordTooShort", length);

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Identity_PasswordRequiresDigit");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Identity_PasswordRequiresLower");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Identity_PasswordRequiresUpper");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Identity_PasswordRequiresNonAlphanumeric");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "Identity_PasswordMismatch");

    public override IdentityError InvalidToken() =>
        Error(nameof(InvalidToken), "Identity_InvalidToken");

    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Identity_DefaultError");
}
