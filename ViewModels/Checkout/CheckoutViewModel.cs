using System.ComponentModel.DataAnnotations;
using WebShop.Infrastructure.Validation;
using WebShop.Models.Orders;
using WebShop.ViewModels.Cart;

namespace WebShop.ViewModels.Checkout;

/// <summary>
/// Only the shopper's own details. Nothing about money lives here — prices,
/// the discount and the total are recomputed on the server from the cart, so
/// a doctored form cannot change what is charged.
///
/// The ErrorMessage strings are resource keys, not English: data annotations
/// localisation is wired up in Program.cs against SharedResource, so each key
/// is looked up in the shopper's language.
/// </summary>
public class CheckoutFormViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100)]
    [Display(Name = "Checkout_FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100)]
    [Display(Name = "Checkout_LastName")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email")]
    [StringLength(256)]
    [Display(Name = "Checkout_Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Deliberately loose. Serbian mobile numbers get written +381 6x, 06x,
    /// with spaces, dashes or neither, and a strict pattern turns away real
    /// customers to no benefit — the courier reads it, not a machine.
    /// </summary>
    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(40, MinimumLength = 6, ErrorMessage = "Validation_Phone")]
    [Display(Name = "Checkout_Phone")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(250)]
    [Display(Name = "Checkout_Street")]
    public string Street { get; set; } = string.Empty;

    [StringLength(40)]
    [Display(Name = "Checkout_StreetNumber")]
    public string? StreetNumber { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(120)]
    [Display(Name = "Checkout_City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(20)]
    [Display(Name = "Checkout_PostalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [Display(Name = "Checkout_DifferentBilling")]
    public bool DifferentBillingAddress { get; set; }

    [StringLength(250)]
    public string? BillingStreet { get; set; }

    [StringLength(120)]
    public string? BillingCity { get; set; }

    [StringLength(20)]
    public string? BillingPostalCode { get; set; }

    [Display(Name = "Checkout_WantsInvoice")]
    public bool WantsInvoice { get; set; }

    [StringLength(250)]
    [Display(Name = "Checkout_CompanyName")]
    public string? CompanyName { get; set; }

    [StringLength(40)]
    [Display(Name = "Checkout_TaxNumber")]
    public string? CompanyTaxNumber { get; set; }

    [StringLength(120)]
    [Display(Name = "Checkout_Courier")]
    public string Courier { get; set; } = "Pošta Srbije";

    [Display(Name = "Checkout_Payment")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

    [StringLength(1000)]
    [Display(Name = "Checkout_Note")]
    public string? Note { get; set; }

    /// <summary>
    /// MustBeTrue rather than Range(typeof(bool)...): the Range version makes
    /// the ticked box fail validation in the browser, so no order could be
    /// placed with JavaScript switched on. See MustBeTrueAttribute.
    /// </summary>
    [Display(Name = "Checkout_AcceptTerms")]
    [MustBeTrue(ErrorMessage = "Validation_AcceptTerms")]
    public bool AcceptsTerms { get; set; }

    /// <summary>
    /// Validation that cannot be expressed as an attribute, because whether a
    /// field is required depends on another field.
    /// </summary>
    public IEnumerable<ValidationResult> ValidateConditional()
    {
        if (DifferentBillingAddress)
        {
            if (string.IsNullOrWhiteSpace(BillingStreet))
            {
                yield return new ValidationResult("Validation_Required", [nameof(BillingStreet)]);
            }

            if (string.IsNullOrWhiteSpace(BillingCity))
            {
                yield return new ValidationResult("Validation_Required", [nameof(BillingCity)]);
            }

            if (string.IsNullOrWhiteSpace(BillingPostalCode))
            {
                yield return new ValidationResult("Validation_Required", [nameof(BillingPostalCode)]);
            }
        }

        if (!WantsInvoice)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            yield return new ValidationResult("Validation_Required", [nameof(CompanyName)]);
        }

        // A Serbian tax number (PIB) is nine digits. Worth checking, because a
        // wrong one means the invoice has to be reissued.
        var pib = CompanyTaxNumber?.Trim();

        if (string.IsNullOrWhiteSpace(pib))
        {
            yield return new ValidationResult("Validation_Required", [nameof(CompanyTaxNumber)]);
        }
        else if (pib.Length != 9 || !pib.All(char.IsDigit))
        {
            yield return new ValidationResult("Validation_TaxNumber", [nameof(CompanyTaxNumber)]);
        }
    }
}

public class CheckoutPageViewModel
{
    public CheckoutFormViewModel Form { get; set; } = new();

    public CartViewModel Cart { get; set; } = new();

    public IReadOnlyList<string> Couriers { get; set; } = WebShop.Infrastructure.Couriers.All;

    /// <summary>Set when an order failed between the cart and the confirmation.</summary>
    public string? Problem { get; set; }

    /// <summary>Product that ran out, when that is why the order failed.</summary>
    public string? SoldOutProduct { get; set; }
}
