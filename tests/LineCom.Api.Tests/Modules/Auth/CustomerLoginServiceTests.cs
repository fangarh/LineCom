using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Auth;

public sealed class CustomerLoginServiceTests
{
    [Fact]
    public async Task LoginAsync_WithEmail_ReturnsCurrentUserAfterPasswordVerification()
    {
        var userId = Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6");
        var repository = new CapturingUserLoginRepository(new LoginUser(
            userId,
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            "customer",
            "stored-password-hash",
            IsActive: true));
        var passwordHasher = new CapturingPasswordHasher(verified: true);
        var service = new CustomerLoginService(repository, passwordHasher);

        var user = await service.LoginAsync(
            new LoginRequest(" IVAN@Example.COM ", "secure-password"),
            CancellationToken.None);

        Assert.Equal(userId, user.Id);
        Assert.Equal("Ivan Petrov", user.Name);
        Assert.Equal("ivan@example.com", user.Email);
        Assert.Equal("+79000000000", user.Phone);
        Assert.Equal("customer", user.Role);
        Assert.Equal("ivan@example.com", repository.LastEmail);
        Assert.Null(repository.LastPhone);
        Assert.Equal("stored-password-hash", passwordHasher.LastPasswordHash);
        Assert.Equal("secure-password", passwordHasher.LastPassword);
    }

    [Fact]
    public async Task LoginAsync_WithPhone_ReturnsCurrentUserAfterPhoneNormalization()
    {
        var repository = new CapturingUserLoginRepository(new LoginUser(
            Guid.Parse("9dfb98f3-9958-4b46-bc14-ec6e8094bb26"),
            "Ivan Petrov",
            null,
            "+79000000000",
            "customer",
            "stored-password-hash",
            IsActive: true));
        var service = new CustomerLoginService(repository, new CapturingPasswordHasher(verified: true));

        await service.LoginAsync(
            new LoginRequest(" +7 (900) 000-00-00 ", "secure-password"),
            CancellationToken.None);

        Assert.Null(repository.LastEmail);
        Assert.Equal("+79000000000", repository.LastPhone);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordDoesNotMatch_ReturnsInvalidCredentials()
    {
        var repository = new CapturingUserLoginRepository(new LoginUser(
            Guid.NewGuid(),
            "Ivan Petrov",
            "ivan@example.com",
            null,
            "customer",
            "stored-password-hash",
            IsActive: true));
        var service = new CustomerLoginService(repository, new CapturingPasswordHasher(verified: false));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.LoginAsync(
                new LoginRequest("ivan@example.com", "wrong-password"),
                CancellationToken.None));

        Assert.Equal("auth.invalid_credentials", exception.Code);
        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ReturnsInvalidCredentials()
    {
        var service = new CustomerLoginService(
            new CapturingUserLoginRepository(user: null),
            new CapturingPasswordHasher(verified: true));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.LoginAsync(
                new LoginRequest("ivan@example.com", "secure-password"),
                CancellationToken.None));

        Assert.Equal("auth.invalid_credentials", exception.Code);
        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ReturnsUserInactive()
    {
        var repository = new CapturingUserLoginRepository(new LoginUser(
            Guid.NewGuid(),
            "Ivan Petrov",
            "ivan@example.com",
            null,
            "customer",
            "stored-password-hash",
            IsActive: false));
        var service = new CustomerLoginService(repository, new CapturingPasswordHasher(verified: true));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.LoginAsync(
                new LoginRequest("ivan@example.com", "secure-password"),
                CancellationToken.None));

        Assert.Equal("auth.user_inactive", exception.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
    }

    [Theory]
    [InlineData(null, "secure-password")]
    [InlineData("   ", "secure-password")]
    [InlineData("ivan@example.com", null)]
    [InlineData("ivan@example.com", "   ")]
    public async Task LoginAsync_WhenRequestIsIncomplete_ReturnsInvalidRequest(string? login, string? password)
    {
        var service = new CustomerLoginService(
            new CapturingUserLoginRepository(user: null),
            new CapturingPasswordHasher(verified: true));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.LoginAsync(
                new LoginRequest(login, password),
                CancellationToken.None));

        Assert.Equal("validation.invalid_request", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    private sealed class CapturingUserLoginRepository : IUserLoginRepository
    {
        private readonly LoginUser? _user;

        public CapturingUserLoginRepository(LoginUser? user)
        {
            _user = user;
        }

        public string? LastEmail { get; private set; }

        public string? LastPhone { get; private set; }

        public Task<LoginUser?> FindByEmailOrPhoneAsync(
            string? email,
            string? phone,
            CancellationToken cancellationToken = default)
        {
            LastEmail = email;
            LastPhone = phone;
            return Task.FromResult(_user);
        }

        public Task<CurrentAuthUser?> FindCurrentUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingPasswordHasher : IPasswordHasher
    {
        private readonly bool _verified;

        public CapturingPasswordHasher(bool verified)
        {
            _verified = verified;
        }

        public string? LastPasswordHash { get; private set; }

        public string? LastPassword { get; private set; }

        public string HashPassword(string password)
        {
            throw new NotSupportedException();
        }

        public bool VerifyPassword(string passwordHash, string password)
        {
            LastPasswordHash = passwordHash;
            LastPassword = password;
            return _verified;
        }
    }
}
