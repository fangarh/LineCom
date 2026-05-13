using LineCom.Api.Modules.Account.DTOs;
using LineCom.Api.Modules.Account.Repositories;
using LineCom.Api.Modules.Account.Services;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Account;

public sealed class AccountProfileServiceTests
{
    [Fact]
    public async Task ChangePasswordAsync_VerifiesCurrentPasswordAndStoresNewHash()
    {
        var user = TestUser();
        var repository = new CapturingAccountProfileRepository { PasswordHash = "old-hash" };
        var hasher = new CapturingPasswordHasher(verified: true, hash: "new-hash");
        var service = new AccountProfileService(new ReturningCurrentUserService(user), repository, hasher);

        await service.ChangePasswordAsync(
            new DefaultHttpContext(),
            new ChangeAccountPasswordRequest("old-password", "new-password"),
            CancellationToken.None);

        Assert.Equal(user.Id, repository.LastPasswordUserId);
        Assert.Equal("old-hash", hasher.LastVerifiedHash);
        Assert.Equal("old-password", hasher.LastVerifiedPassword);
        Assert.Equal("new-password", hasher.LastHashedPassword);
        Assert.Equal("new-hash", repository.LastPasswordHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_RejectsInvalidCurrentPassword()
    {
        var repository = new CapturingAccountProfileRepository { PasswordHash = "old-hash" };
        var hasher = new CapturingPasswordHasher(verified: false, hash: "new-hash");
        var service = new AccountProfileService(new ReturningCurrentUserService(TestUser()), repository, hasher);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.ChangePasswordAsync(
                new DefaultHttpContext(),
                new ChangeAccountPasswordRequest("wrong-password", "new-password"),
                CancellationToken.None));

        Assert.Equal("account.invalid_current_password", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Null(repository.LastPasswordHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_RequiresCurrentPassword()
    {
        var repository = new CapturingAccountProfileRepository { PasswordHash = "old-hash" };
        var hasher = new CapturingPasswordHasher(verified: true, hash: "new-hash");
        var service = new AccountProfileService(new ReturningCurrentUserService(TestUser()), repository, hasher);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.ChangePasswordAsync(
                new DefaultHttpContext(),
                new ChangeAccountPasswordRequest("", "new-password"),
                CancellationToken.None));

        Assert.Equal("auth.invalid_password", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Null(hasher.LastVerifiedPassword);
        Assert.Null(repository.LastPasswordHash);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task ChangePasswordAsync_RequiresNewPasswordLengthBetween8And128(string newPassword)
    {
        var repository = new CapturingAccountProfileRepository { PasswordHash = "old-hash" };
        var hasher = new CapturingPasswordHasher(verified: true, hash: "new-hash");
        var service = new AccountProfileService(new ReturningCurrentUserService(TestUser()), repository, hasher);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.ChangePasswordAsync(
                new DefaultHttpContext(),
                new ChangeAccountPasswordRequest("old-password", newPassword),
                CancellationToken.None));

        Assert.Equal("auth.invalid_password", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Null(repository.LastPasswordHash);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsCurrentUserAndOrganization()
    {
        var user = TestUser();
        var repository = new CapturingAccountProfileRepository
        {
            Organization = new AccountOrganizationRecord(
                "ООО Сеть",
                "7700000000",
                "Ivan Petrov",
                "+79000000000",
                "sales@example.com",
                "Main organization")
        };
        var service = new AccountProfileService(new ReturningCurrentUserService(user), repository, TestPasswordHasher());

        var profile = await service.GetProfileAsync(new DefaultHttpContext(), CancellationToken.None);

        Assert.Equal(user, profile.User);
        Assert.NotNull(profile.Organization);
        Assert.Equal("ООО Сеть", profile.Organization.Name);
        Assert.Equal("7700000000", profile.Organization.Inn);
        Assert.Equal("Ivan Petrov", profile.Organization.ContactPerson);
        Assert.Equal("+79000000000", profile.Organization.Phone);
        Assert.Equal("sales@example.com", profile.Organization.Email);
        Assert.Equal("Main organization", profile.Organization.Comment);
        Assert.Equal(user.Id, repository.LastFindOrganizationUserId);
    }

    [Fact]
    public async Task UpdateProfileAsync_NormalizesContactsAndUpdatesOnlyCurrentUser()
    {
        var user = TestUser();
        var repository = new CapturingAccountProfileRepository();
        var service = new AccountProfileService(new ReturningCurrentUserService(user), repository, TestPasswordHasher());

        var updated = await service.UpdateProfileAsync(
            new DefaultHttpContext(),
            new UpdateAccountProfileRequest("  Ivan Petrov  ", "  IVAN@Example.COM  ", " +7 (900) 000-00-00 "),
            CancellationToken.None);

        Assert.Equal(user.Id, repository.LastUpdateUserId);
        Assert.NotNull(repository.LastProfileUpdate);
        Assert.Equal("Ivan Petrov", repository.LastProfileUpdate.Name);
        Assert.Equal("ivan@example.com", repository.LastProfileUpdate.Email);
        Assert.Equal("+79000000000", repository.LastProfileUpdate.Phone);
        Assert.Equal("Ivan Petrov", updated.Name);
        Assert.Equal("ivan@example.com", updated.Email);
        Assert.Equal("+79000000000", updated.Phone);
        Assert.Equal("customer", updated.Role);
    }

    [Fact]
    public async Task UpdateProfileAsync_MapsDuplicateContactToPublicAuthError()
    {
        var service = new AccountProfileService(
            new ReturningCurrentUserService(TestUser()),
            new DuplicateContactAccountProfileRepository(),
            TestPasswordHasher());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateProfileAsync(
                new DefaultHttpContext(),
                new UpdateAccountProfileRequest("Ivan Petrov", "ivan@example.com", null),
                CancellationToken.None));

        Assert.Equal("auth.user_already_exists", exception.Code);
        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateProfileAsync_RequiresEmailOrPhone()
    {
        var service = new AccountProfileService(
            new ReturningCurrentUserService(TestUser()),
            new CapturingAccountProfileRepository(),
            TestPasswordHasher());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateProfileAsync(
                new DefaultHttpContext(),
                new UpdateAccountProfileRequest("Ivan Petrov", " ", null),
                CancellationToken.None));

        Assert.Equal("auth.invalid_contact", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task UpsertOrganizationAsync_NormalizesOrganizationForCurrentUser()
    {
        var user = TestUser();
        var repository = new CapturingAccountProfileRepository();
        var service = new AccountProfileService(new ReturningCurrentUserService(user), repository, TestPasswordHasher());

        var organization = await service.UpsertOrganizationAsync(
            new DefaultHttpContext(),
            new UpsertAccountOrganizationRequest(
                "  ООО Сеть  ",
                "  7700000000  ",
                "  Ivan Petrov  ",
                " +7 (900) 000-00-00 ",
                "  SALES@Example.COM  ",
                "  Main organization  "),
            CancellationToken.None);

        Assert.Equal(user.Id, repository.LastUpsertOrganizationUserId);
        Assert.NotNull(repository.LastOrganizationUpsert);
        Assert.Equal("ООО Сеть", repository.LastOrganizationUpsert.Name);
        Assert.Equal("7700000000", repository.LastOrganizationUpsert.Inn);
        Assert.Equal("Ivan Petrov", repository.LastOrganizationUpsert.ContactPerson);
        Assert.Equal("+79000000000", repository.LastOrganizationUpsert.Phone);
        Assert.Equal("sales@example.com", repository.LastOrganizationUpsert.Email);
        Assert.Equal("Main organization", repository.LastOrganizationUpsert.Comment);
        Assert.Equal("ООО Сеть", organization.Name);
        Assert.Equal("sales@example.com", organization.Email);
    }

    [Fact]
    public async Task UpsertOrganizationAsync_RequiresOrganizationName()
    {
        var service = new AccountProfileService(
            new ReturningCurrentUserService(TestUser()),
            new CapturingAccountProfileRepository(),
            TestPasswordHasher());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpsertOrganizationAsync(
                new DefaultHttpContext(),
                new UpsertAccountOrganizationRequest(" ", null, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("validation.invalid_request", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    private static CurrentUserDto TestUser()
    {
        return new CurrentUserDto(
            Guid.Parse("1f0d787f-10a4-4f4b-b5bd-df2d5fa28df6"),
            "Ivan Petrov",
            "ivan@example.com",
            "+79000000000",
            "customer");
    }

    private static CapturingPasswordHasher TestPasswordHasher()
    {
        return new CapturingPasswordHasher(verified: true, hash: "unused-hash");
    }

    private sealed class ReturningCurrentUserService : IAuthCurrentUserService
    {
        private readonly CurrentUserDto _user;

        public ReturningCurrentUserService(CurrentUserDto user)
        {
            _user = user;
        }

        public Task<AuthSessionDto> GetCurrentSessionAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthSessionDto(_user, "csrf-token"));
        }
    }

    private sealed class CapturingAccountProfileRepository : IAccountProfileRepository
    {
        public AccountOrganizationRecord? Organization { get; init; }
        public string? PasswordHash { get; init; }
        public Guid? LastFindOrganizationUserId { get; private set; }
        public Guid? LastUpdateUserId { get; private set; }
        public AccountProfileUpdate? LastProfileUpdate { get; private set; }
        public Guid? LastUpsertOrganizationUserId { get; private set; }
        public AccountOrganizationUpsert? LastOrganizationUpsert { get; private set; }
        public Guid? LastPasswordUserId { get; private set; }
        public string? LastPasswordHash { get; private set; }

        public Task<AccountOrganizationRecord?> FindOrganizationAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            LastFindOrganizationUserId = userId;
            return Task.FromResult(Organization);
        }

        public Task<CurrentUserDto> UpdateProfileAsync(
            Guid userId,
            AccountProfileUpdate profile,
            CancellationToken cancellationToken = default)
        {
            LastUpdateUserId = userId;
            LastProfileUpdate = profile;

            return Task.FromResult(new CurrentUserDto(userId, profile.Name, profile.Email, profile.Phone, "customer"));
        }

        public Task<AccountOrganizationRecord> UpsertOrganizationAsync(
            Guid userId,
            AccountOrganizationUpsert organization,
            CancellationToken cancellationToken = default)
        {
            LastUpsertOrganizationUserId = userId;
            LastOrganizationUpsert = organization;

            return Task.FromResult(new AccountOrganizationRecord(
                organization.Name,
                organization.Inn,
                organization.ContactPerson,
                organization.Phone,
                organization.Email,
                organization.Comment));
        }

        public Task<string?> FindPasswordHashAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            LastPasswordUserId = userId;
            return Task.FromResult(PasswordHash);
        }

        public Task UpdatePasswordHashAsync(
            Guid userId,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            LastPasswordUserId = userId;
            LastPasswordHash = passwordHash;
            return Task.CompletedTask;
        }
    }

    private sealed class DuplicateContactAccountProfileRepository : IAccountProfileRepository
    {
        public Task<AccountOrganizationRecord?> FindOrganizationAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CurrentUserDto> UpdateProfileAsync(
            Guid userId,
            AccountProfileUpdate profile,
            CancellationToken cancellationToken = default)
        {
            throw new DuplicateUserContactException();
        }

        public Task<AccountOrganizationRecord> UpsertOrganizationAsync(
            Guid userId,
            AccountOrganizationUpsert organization,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<string?> FindPasswordHashAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdatePasswordHashAsync(
            Guid userId,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingPasswordHasher : IPasswordHasher
    {
        private readonly bool _verified;
        private readonly string _hash;

        public CapturingPasswordHasher(bool verified, string hash)
        {
            _verified = verified;
            _hash = hash;
        }

        public string? LastHashedPassword { get; private set; }
        public string? LastVerifiedHash { get; private set; }
        public string? LastVerifiedPassword { get; private set; }

        public string HashPassword(string password)
        {
            LastHashedPassword = password;
            return _hash;
        }

        public bool VerifyPassword(string passwordHash, string password)
        {
            LastVerifiedHash = passwordHash;
            LastVerifiedPassword = password;
            return _verified;
        }
    }
}
