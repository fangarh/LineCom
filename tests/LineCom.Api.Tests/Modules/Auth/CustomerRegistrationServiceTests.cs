using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Auth;

public sealed class CustomerRegistrationServiceTests
{
    [Fact]
    public async Task RegisterCustomerAsync_CreatesCustomerWithNormalizedContactsAndDefaultRole()
    {
        var userId = Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6");
        var repository = new CapturingUserRegistrationRepository(userId);
        var passwordHasher = new CapturingPasswordHasher("hashed-password");
        var service = new CustomerRegistrationService(repository, passwordHasher);

        var user = await service.RegisterCustomerAsync(
            new RegisterRequest(
                "  Ivan Petrov  ",
                "  IVAN@Example.COM ",
                " +7 (900) 000-00-00 ",
                "secure-password"),
            CancellationToken.None);

        Assert.Equal(userId, user.Id);
        Assert.Equal("Ivan Petrov", user.Name);
        Assert.Equal("ivan@example.com", user.Email);
        Assert.Equal("+79000000000", user.Phone);
        Assert.Equal("customer", user.Role);

        Assert.NotNull(repository.LastRegistration);
        Assert.Equal("Ivan Petrov", repository.LastRegistration.Name);
        Assert.Equal("ivan@example.com", repository.LastRegistration.Email);
        Assert.Equal("+79000000000", repository.LastRegistration.Phone);
        Assert.Equal("customer", repository.LastRegistration.Role);
        Assert.True(repository.LastRegistration.IsActive);
    }

    [Fact]
    public async Task RegisterCustomerAsync_RequiresEmailOrPhone()
    {
        var service = new CustomerRegistrationService(
            new CapturingUserRegistrationRepository(Guid.NewGuid()),
            new CapturingPasswordHasher("hashed-password"));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.RegisterCustomerAsync(
                new RegisterRequest("Ivan Petrov", "   ", null, "secure-password"),
                CancellationToken.None));

        Assert.Equal("auth.invalid_contact", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public async Task RegisterCustomerAsync_RejectsInvalidPassword(string password)
    {
        var service = new CustomerRegistrationService(
            new CapturingUserRegistrationRepository(Guid.NewGuid()),
            new CapturingPasswordHasher("hashed-password"));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.RegisterCustomerAsync(
                new RegisterRequest("Ivan Petrov", "ivan@example.com", null, password),
                CancellationToken.None));

        Assert.Equal("auth.invalid_password", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task RegisterCustomerAsync_MapsDuplicateContactToPublicAuthError()
    {
        var service = new CustomerRegistrationService(
            new DuplicateUserRegistrationRepository(),
            new CapturingPasswordHasher("hashed-password"));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.RegisterCustomerAsync(
                new RegisterRequest("Ivan Petrov", "ivan@example.com", null, "secure-password"),
                CancellationToken.None));

        Assert.Equal("auth.user_already_exists", exception.Code);
        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task RegisterCustomerAsync_StoresPasswordHashWithoutPlaintextPassword()
    {
        var repository = new CapturingUserRegistrationRepository(Guid.NewGuid());
        var passwordHasher = new CapturingPasswordHasher("pbkdf2-sha256$hash-only");
        var service = new CustomerRegistrationService(repository, passwordHasher);

        await service.RegisterCustomerAsync(
            new RegisterRequest("Ivan Petrov", "ivan@example.com", null, "secure-password"),
            CancellationToken.None);

        Assert.Equal("secure-password", passwordHasher.LastPassword);
        Assert.NotNull(repository.LastRegistration);
        Assert.Equal("pbkdf2-sha256$hash-only", repository.LastRegistration.PasswordHash);
        Assert.DoesNotContain("secure-password", repository.LastRegistration.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public void Pbkdf2PasswordHasher_HashPassword_ReturnsVersionedHashWithoutPlaintextPassword()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hash = hasher.HashPassword("secure-password");

        Assert.StartsWith("pbkdf2-sha256$", hash, StringComparison.Ordinal);
        Assert.DoesNotContain("secure-password", hash, StringComparison.Ordinal);
    }

    private sealed class CapturingUserRegistrationRepository : IUserRegistrationRepository
    {
        private readonly Guid _userId;

        public CapturingUserRegistrationRepository(Guid userId)
        {
            _userId = userId;
        }

        public NewUserRegistration? LastRegistration { get; private set; }

        public Task<RegisteredUser> CreateCustomerAsync(
            NewUserRegistration registration,
            CancellationToken cancellationToken = default)
        {
            LastRegistration = registration;

            return Task.FromResult(new RegisteredUser(
                _userId,
                registration.Name,
                registration.Email,
                registration.Phone,
                registration.Role));
        }
    }

    private sealed class DuplicateUserRegistrationRepository : IUserRegistrationRepository
    {
        public Task<RegisteredUser> CreateCustomerAsync(
            NewUserRegistration registration,
            CancellationToken cancellationToken = default)
        {
            throw new DuplicateUserContactException();
        }
    }

    private sealed class CapturingPasswordHasher : IPasswordHasher
    {
        private readonly string _hash;

        public CapturingPasswordHasher(string hash)
        {
            _hash = hash;
        }

        public string? LastPassword { get; private set; }

        public string HashPassword(string password)
        {
            LastPassword = password;
            return _hash;
        }

        public bool VerifyPassword(string passwordHash, string password)
        {
            throw new NotSupportedException();
        }
    }
}
