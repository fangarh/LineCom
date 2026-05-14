namespace LineCom.Api.Modules.Auth.Services;

internal static class AuthPasswordPolicy
{
    private const int MinimumPasswordLength = 8;
    private const int MaximumPasswordLength = 128;

    public static bool IsValidPassword(string? password)
    {
        return password?.Length is >= MinimumPasswordLength and <= MaximumPasswordLength;
    }
}
