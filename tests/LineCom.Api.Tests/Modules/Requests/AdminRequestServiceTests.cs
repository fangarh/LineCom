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

    [Fact]
    public async Task GetRequestsAsync_NormalizesFilters()
    {
        var repository = new CapturingAdminRequestRepository();
        var service = CreateService("seller", repository);

        await service.GetRequestsAsync(
            new DefaultHttpContext(),
            new AdminRequestListQuery(
                2,
                10,
                " new ",
                " REQ-1 ",
                " customer@example.com ",
                " Acme "),
            CancellationToken.None);

        Assert.NotNull(repository.LastListQuery);
        var query = repository.LastListQuery;
        Assert.Equal(2, query.Page);
        Assert.Equal(10, query.PageSize);
        Assert.Equal("new", query.Status);
        Assert.Equal("REQ-1", query.Number);
        Assert.Equal("customer@example.com", query.Contact);
        Assert.Equal("Acme", query.Organization);
    }

    [Fact]
    public async Task GetRequestsAsync_RejectsQuotedStatus()
    {
        var service = CreateService("seller", new CapturingAdminRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetRequestsAsync(
                new DefaultHttpContext(),
                new AdminRequestListQuery(1, 20, "quoted", null, null, null),
                CancellationToken.None));

        Assert.Equal("request.invalid_status", exception.Code);
    }

    [Fact]
    public async Task GetRequestAsync_ReturnsDetail()
    {
        var repository = new CapturingAdminRequestRepository();
        var service = CreateService("seller", repository);

        var response = await service.GetRequestAsync(
            new DefaultHttpContext(),
            " REQ-1 ",
            CancellationToken.None);

        Assert.Equal("REQ-1", repository.LastDetailNumber);
        Assert.Equal("REQ-1", response.Number);
        Assert.Equal("new", response.Status.Code);
        Assert.Equal(new RequestReferenceData().GetStatus("new").Label, response.Status.Label);
        Assert.Equal("internal note", response.InternalComment);
        Assert.Equal("Customer Name", response.Customer.Name);
        Assert.NotNull(response.Organization);
        var organization = response.Organization;
        Assert.Equal("Acme", organization.Name);

        var item = Assert.Single(response.Items);
        Assert.Equal("Cable", item.ProductName);
        Assert.Equal("coil", item.SaleUnit.Code);
        Assert.Equal(new LineCom.Api.Modules.Catalog.Services.PublicCatalogReferenceData()
            .GetSaleUnit("coil")
            .Label, item.SaleUnit.Label);

        var history = Assert.Single(response.History);
        Assert.Equal("created", history.Event);
        Assert.Equal("Request created", history.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_NormalizesStatusAndPassesActor()
    {
        var repository = new CapturingAdminRequestRepository();
        var service = CreateService("admin", repository);

        var response = await service.UpdateStatusAsync(
            new DefaultHttpContext(),
            " REQ-1 ",
            new UpdateAdminRequestStatusCommand(" in_progress "),
            CancellationToken.None);

        Assert.NotNull(repository.LastStatusUpdate);
        var update = repository.LastStatusUpdate;
        Assert.Equal("REQ-1", update.Number);
        Assert.Equal("in_progress", update.Status);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), update.ActorUserId);
        Assert.Equal("REQ-1", response.Number);
        Assert.Equal("new", response.Status.Code);
    }

    [Fact]
    public async Task UpdateInternalCommentAsync_NormalizesBlankToNull()
    {
        var repository = new CapturingAdminRequestRepository();
        var service = CreateService("seller", repository);

        var response = await service.UpdateInternalCommentAsync(
            new DefaultHttpContext(),
            " REQ-1 ",
            new UpdateAdminRequestInternalCommentCommand("   "),
            CancellationToken.None);

        Assert.NotNull(repository.LastCommentUpdate);
        var update = repository.LastCommentUpdate;
        Assert.Equal("REQ-1", update.Number);
        Assert.Null(update.InternalComment);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), update.ActorUserId);
        Assert.Equal("REQ-1", response.Number);
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
        private static readonly AdminRequestDetailRecord DetailRecord = new(
            "REQ-1",
            "new",
            "cart",
            new RequestCustomerSnapshotRecord(
                "Customer Name",
                "customer@example.com",
                "+79990000000"),
            new RequestOrganizationSnapshotRecord(
                "Acme",
                "1234567890",
                "Contact Person"),
            "customer note",
            "internal note",
            new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 2, 10, 0, 0, TimeSpan.Zero),
            new[]
            {
                new CreatedCustomerRequestItemRecord(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Cable",
                    "CBL-1",
                    "coil",
                    "100 m",
                    2,
                    "item note")
            },
            new[]
            {
                new CustomerRequestHistoryRecord(
                    "created",
                    "Request created",
                    new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero))
            });

        public AdminRequestReadListQuery? LastListQuery { get; private set; }
        public string? LastDetailNumber { get; private set; }
        public AdminRequestStatusUpdate? LastStatusUpdate { get; private set; }
        public AdminRequestInternalCommentUpdate? LastCommentUpdate { get; private set; }

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
            LastDetailNumber = number;

            return Task.FromResult<AdminRequestDetailRecord?>(DetailRecord);
        }

        public Task<AdminRequestDetailRecord?> UpdateStatusAsync(
            AdminRequestStatusUpdate update,
            CancellationToken cancellationToken = default)
        {
            LastStatusUpdate = update;

            return Task.FromResult<AdminRequestDetailRecord?>(DetailRecord);
        }

        public Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(
            AdminRequestInternalCommentUpdate update,
            CancellationToken cancellationToken = default)
        {
            LastCommentUpdate = update;

            return Task.FromResult<AdminRequestDetailRecord?>(DetailRecord);
        }
    }
}
