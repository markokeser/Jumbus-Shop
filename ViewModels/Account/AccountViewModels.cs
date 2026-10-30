using System.ComponentModel.DataAnnotations;
using WebShop.Infrastructure.Validation;
using WebShop.Models.Orders;

namespace WebShop.ViewModels.Account;

// ErrorMessage values are resource keys; MVC localises them through
// SharedResource (wired in Program.cs). Labels come from the views directly.

public class LoginViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Validation_PasswordLength")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [Compare(nameof(Password), ErrorMessage = "Validation_PasswordsDiffer")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MustBeTrue(ErrorMessage = "Validation_AcceptTerms")]
    public bool AcceptsTerms { get; set; }

    public bool AcceptsMarketing { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Validation_PasswordLength")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [Compare(nameof(Password), ErrorMessage = "Validation_PasswordsDiffer")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>The reset token from the emailed link.</summary>
    [Required]
    public string Code { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    /// <summary>Shown, not edited: changing it safely needs a confirmation email.</summary>
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(40)]
    public string? PhoneNumber { get; set; }

    public bool AcceptsMarketing { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Validation_PasswordLength")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Validation_PasswordsDiffer")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class AccountOverviewViewModel
{
    public string FirstName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public List<Order> RecentOrders { get; set; } = [];

    public Address? DefaultAddress { get; set; }

    public int WishlistCount { get; set; }

    public int OrderCount { get; set; }
}

public class AddressFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(250)]
    public string Street { get; set; } = string.Empty;

    [StringLength(40)]
    public string? StreetNumber { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(120)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(40, MinimumLength = 6, ErrorMessage = "Validation_Phone")]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

public class TrackOrderViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(32)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Filled once a matching order is found.</summary>
    public Order? Order { get; set; }

    public bool Searched { get; set; }
}

/// <summary>Deleting the account asks for the password and an explicit tick.</summary>
public class DeleteAccountViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [MustBeTrue(ErrorMessage = "Account_DeleteConfirmRequired")]
    public bool Confirm { get; set; }
}
