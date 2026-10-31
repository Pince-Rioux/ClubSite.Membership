using VereinMember.Data;
using VereinMember.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Services.Validators;

public class MemberValidator : AbstractValidator<Member>
{
    private readonly MembershipDbContext _context;

    public MemberValidator(MembershipDbContext context)
    {
        _context = context;

        RuleFor(m => m.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(m => m.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(m => m.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.");

        RuleFor(m => m.BirthDate)
            .NotEmpty().WithMessage("Birth date is required.")
            .Must(bd => bd <= DateTime.UtcNow).WithMessage("Birth date must be in the past.");

        RuleFor(m => m.Phone)
            .MaximumLength(50).WithMessage("Phone must not exceed 50 characters.")
            .When(m => !string.IsNullOrEmpty(m.Phone));

        RuleFor(m => m.Street)
            .MaximumLength(200).WithMessage("Street must not exceed 200 characters.")
            .When(m => !string.IsNullOrEmpty(m.Street));

        RuleFor(m => m.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(m => !string.IsNullOrEmpty(m.City));

        RuleFor(m => m.ZipCode)
            .MaximumLength(20).WithMessage("Zip code must not exceed 20 characters.")
            .When(m => !string.IsNullOrEmpty(m.ZipCode));

        RuleFor(m => m.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
            .When(m => !string.IsNullOrEmpty(m.Country));

        RuleFor(m => m.BankIban)
            .Must(BeValidIban).WithMessage("A valid IBAN is required.")
            .When(m => !string.IsNullOrEmpty(m.BankIban));

        RuleFor(m => m.BankBic)
            .MaximumLength(11).WithMessage("BIC must not exceed 11 characters.")
            .When(m => !string.IsNullOrEmpty(m.BankBic));

        RuleFor(m => m.MembershipFeeId)
            .GreaterThan(0).WithMessage("A membership fee must be selected.");

        // Category-specific: Guardian required for Youth
        RuleFor(m => m.GuardianMemberId)
            .NotEmpty().WithMessage("A guardian is required for Youth members.")
            .WhenAsync(async (m, _) =>
            {
                var fee = await GetFeeAsync(m.MembershipFeeId);
                return fee?.Category == FeeCategory.Youth;
            });

        RuleFor(m => m.GuardianMemberId)
            .NotEqual(m => m.Id).WithMessage("A member cannot be their own guardian.")
            .When(m => m.GuardianMemberId.HasValue);

        // Note: FamilyGroupId is validated and set programmatically in CreateFamilyAsync.
        // The Create form shows inline sub-member rows instead of a FamilyGroup dropdown,
        // so this rule is not enforced at the validator level for Family members.
        // For non-Family members, FamilyGroupId must stay null.
        RuleFor(m => m.FamilyGroupId)
            .Null().WithMessage("Family group should not be set for non-Family members.")
            .WhenAsync(async (m, _) =>
            {
                var fee = await GetFeeAsync(m.MembershipFeeId);
                return fee?.Category != FeeCategory.Family;
            });
    }

    private async Task<MembershipFee?> GetFeeAsync(int feeId)
    {
        return await _context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == feeId);
    }

    private static bool BeValidIban(string? iban)
    {
        if (string.IsNullOrEmpty(iban)) return true;

        // Step 1: Remove spaces, to uppercase
        var cleaned = iban.Replace(" ", "").Replace("-", "").ToUpper();
        if (cleaned.Length < 4 || cleaned.Length > 34) return false;

        // Check first 2 chars are letters and next 2 are digits
        if (!char.IsLetter(cleaned[0]) || !char.IsLetter(cleaned[1])) return false;
        if (!char.IsDigit(cleaned[2]) || !char.IsDigit(cleaned[3])) return false;

        // Remaining chars must be alphanumeric
        if (!cleaned[4..].All(char.IsLetterOrDigit)) return false;

        // Step 2: MOD-97 checksum (ISO 7064)
        // Move first 4 characters to end, replace letters with numbers (A=10..Z=35),
        // then calc mod 97 using modular arithmetic (avoids overflow).
        var map = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var rearranged = cleaned[4..] + cleaned[0..4];
        int remainder = 0;
        for (int i = 0; i < rearranged.Length; i++)
        {
            int value = map.IndexOf(rearranged[i]);
            if (value < 0) return false; // invalid character
            remainder = (remainder * 10 + value) % 97;
        }

        return remainder == 1;
    }
}