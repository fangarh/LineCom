using LineCom.Api.Modules.Auth.Services;

namespace LineCom.Api.Tests.Modules.Auth;

public sealed class AuthPasswordPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public void IsValidPassword_RejectsMissingOrShortPasswords(string? password)
    {
        Assert.False(AuthPasswordPolicy.IsValidPassword(password));
    }

    [Fact]
    public void IsValidPassword_RejectsPasswordsLongerThan128Characters()
    {
        Assert.False(AuthPasswordPolicy.IsValidPassword(new string('a', 129)));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public void IsValidPassword_AcceptsBoundaryLengths(int length)
    {
        Assert.True(AuthPasswordPolicy.IsValidPassword(new string('a', length)));
    }
}
