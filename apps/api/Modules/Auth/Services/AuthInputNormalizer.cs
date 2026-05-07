using System.Net.Mail;

namespace LineCom.Api.Modules.Auth.Services;

internal static class AuthInputNormalizer
{
    public const int MinimumPhoneLength = 4;
    public const int MaximumPhoneLength = 32;

    public static string? RequiredText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static string? Email(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        try
        {
            var address = new MailAddress(trimmed);
            if (!string.Equals(trimmed, address.Address, StringComparison.OrdinalIgnoreCase))
            {
                throw AuthErrors.InvalidRequest();
            }

            return address.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw AuthErrors.InvalidRequest();
        }
    }

    public static string? Phone(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        var normalized = new char[trimmed.Length];
        var index = 0;

        if (trimmed[0] == '+')
        {
            normalized[index++] = '+';
        }

        foreach (var character in trimmed)
        {
            if (char.IsAsciiDigit(character))
            {
                normalized[index++] = character;
            }
        }

        var phone = new string(normalized, 0, index);
        var digitCount = phone.Length > 0 && phone[0] == '+' ? phone.Length - 1 : phone.Length;

        if (digitCount is < MinimumPhoneLength or > MaximumPhoneLength)
        {
            throw AuthErrors.InvalidRequest();
        }

        return phone;
    }

    public static (string? Email, string? Phone) LoginContact(string login)
    {
        var trimmed = login.Trim();
        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            return (Email(trimmed), null);
        }

        return (null, Phone(trimmed));
    }
}
