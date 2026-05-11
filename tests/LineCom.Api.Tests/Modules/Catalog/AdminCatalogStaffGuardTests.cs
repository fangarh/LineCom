using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogStaffGuardTests
{
    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task RequireStaffAsync_AllowsSellerAndAdmin(string role)
    {
        var guard = new AdminCatalogStaffGuard(new ReturningCurrentUserService(role));

        var user = await guard.RequireStaffAsync(new DefaultHttpContext(), CancellationToken.None);

        Assert.Equal(role, user.Role);
    }

    [Fact]
    public async Task RequireStaffAsync_RejectsCustomer()
    {
        var guard = new AdminCatalogStaffGuard(new ReturningCurrentUserService("customer"));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            guard.RequireStaffAsync(new DefaultHttpContext(), CancellationToken.None));

        Assert.Equal("auth.forbidden", exception.Code);
    }

    private sealed class ReturningCurrentUserService : IAuthCurrentUserService
    {
        private readonly string _role;

        public ReturningCurrentUserService(string role)
        {
            _role = role;
        }

        public Task<AuthSessionDto> GetCurrentSessionAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthSessionDto(
                new CurrentUserDto(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Staff User",
                    "staff@example.com",
                    null,
                    _role),
                "csrf-token"));
        }
    }
}
