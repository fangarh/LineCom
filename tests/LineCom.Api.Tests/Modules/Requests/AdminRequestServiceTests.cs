using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Repositories;
using LineCom.Api.Modules.Requests.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class AdminRequestServiceTests
{
    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task GetRequestsAsync_AllowsSellerAndAdmin(string role)
    {
        var repository = new CapturingAdminRequestRepository();
        var service = CreateService(role, repository);

        await service.GetRequestsAsync(
            new DefaultHttpContext(),
            new AdminRequestListQuery(1, 20, null, null, null, null),
            CancellationToken.None);

        Assert.NotNull(repository.LastListQuery);
    }

    [Fact]
    public async Task GetRequestsAsync_RejectsCustomer()
    {
        var service = CreateService("customer", new CapturingAdminRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetRequestsAsync(
                new DefaultHttpContext(),
                new AdminRequestListQuery(1, 20, null, null, null, null),
                CancellationToken.None));

        Assert.Equal("auth.forbidden", exception.Code);
    }

    private static AdminRequestService CreateService(string role, IAdminRequestRepository repository)
    {
        return new AdminRequestService(
            new ReturningCurrentUserService(new CurrentUserDto(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Staff User",
                "staff@example.com",
                null,
                role)),
            repository,
            new RequestReferenceData(),
            new LineCom.Api.Modules.Catalog.Services.PublicCatalogReferenceData());
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

    private sealed class CapturingAdminRequestRepository : IAdminRequestRepository
    {
        public AdminRequestReadListQuery? LastListQuery { get; private set; }

        public Task<AdminRequestListRecordResponse> GetRequestsAsync(
            AdminRequestReadListQuery query,
            CancellationToken cancellationToken = default)
        {
            LastListQuery = query;

            return Task.FromResult(new AdminRequestListRecordResponse(
                Array.Empty<AdminRequestListRecord>(),
                TotalItems: 0));
        }

        public Task<AdminRequestDetailRecord?> GetRequestAsync(
            string number,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AdminRequestDetailRecord?> UpdateStatusAsync(
            AdminRequestStatusUpdate update,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(
            AdminRequestInternalCommentUpdate update,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
