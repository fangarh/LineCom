using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Repositories;
using LineCom.Api.Modules.Requests.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class CustomerRequestServiceTests
{
    [Fact]
    public async Task CreateRequestAsync_NormalizesCommandAndCreatesRequestForCurrentUser()
    {
        var user = TestUser();
        var repository = new CapturingCustomerRequestRepository();
        var service = new CustomerRequestService(
            new ReturningCurrentUserService(user),
            repository,
            new RequestReferenceData(),
            new LineCom.Api.Modules.Catalog.Services.PublicCatalogReferenceData());

        var response = await service.CreateRequestAsync(
            new DefaultHttpContext(),
            new CreateRequestCommand(
                " cart ",
                "  Need delivery date  ",
                new[]
                {
                    new CreateRequestItemCommand(
                        Guid.Parse("3d6e4e11-2a88-4d01-9d44-1cfb7400924f"),
                        2,
                        "  Replace with analogue if faster  ")
                }),
            CancellationToken.None);

        Assert.NotNull(repository.LastDraft);
        Assert.Equal(user.Id, repository.LastDraft.User.Id);
        Assert.Equal("cart", repository.LastDraft.Source);
        Assert.Equal("Need delivery date", repository.LastDraft.CustomerComment);
        var item = Assert.Single(repository.LastDraft.Items);
        Assert.Equal(2, item.Quantity);
        Assert.Equal("Replace with analogue if faster", item.CustomerComment);

        Assert.Equal("ЗК26-0001", response.Number);
        Assert.Equal("new", response.Status.Code);
        Assert.Equal("Новая", response.Status.Label);
        Assert.Equal("cart", response.Source);
        Assert.Equal("Need delivery date", response.CustomerComment);
        var responseItem = Assert.Single(response.Items);
        Assert.Equal("coil", responseItem.SaleUnit.Code);
        Assert.Equal("бухта", responseItem.SaleUnit.Label);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("warehouse")]
    public async Task CreateRequestAsync_RejectsInvalidSource(string? source)
    {
        var service = CreateService(new CapturingCustomerRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateRequestAsync(
                new DefaultHttpContext(),
                new CreateRequestCommand(
                    source,
                    null,
                    new[]
                    {
                        new CreateRequestItemCommand(Guid.NewGuid(), 1, null)
                    }),
                CancellationToken.None));

        Assert.Equal("validation.invalid_request", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task CreateRequestAsync_RejectsEmptyItems()
    {
        var service = CreateService(new CapturingCustomerRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateRequestAsync(
                new DefaultHttpContext(),
                new CreateRequestCommand("cart", null, Array.Empty<CreateRequestItemCommand>()),
                CancellationToken.None));

        Assert.Equal("request.invalid_items", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 1)]
    [InlineData("3d6e4e11-2a88-4d01-9d44-1cfb7400924f", 0)]
    public async Task CreateRequestAsync_RejectsInvalidItems(string productId, int quantity)
    {
        var service = CreateService(new CapturingCustomerRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateRequestAsync(
                new DefaultHttpContext(),
                new CreateRequestCommand(
                    "cart",
                    null,
                    new[]
                    {
                        new CreateRequestItemCommand(Guid.Parse(productId), quantity, null)
                    }),
                CancellationToken.None));

        Assert.Equal("request.invalid_items", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task CreateRequestAsync_MapsUnavailableProductToPublicError()
    {
        var service = CreateService(new ProductUnavailableCustomerRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.CreateRequestAsync(
                new DefaultHttpContext(),
                new CreateRequestCommand(
                    "cart",
                    null,
                    new[]
                    {
                        new CreateRequestItemCommand(Guid.NewGuid(), 1, null)
                    }),
                CancellationToken.None));

        Assert.Equal("request.product_not_available", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GetRequestsAsync_NormalizesQueryAndReadsRequestsForCurrentUser()
    {
        var user = TestUser();
        var repository = new CapturingCustomerRequestRepository();
        var service = new CustomerRequestService(
            new ReturningCurrentUserService(user),
            repository,
            new RequestReferenceData(),
            new LineCom.Api.Modules.Catalog.Services.PublicCatalogReferenceData());

        var response = await service.GetRequestsAsync(
            new DefaultHttpContext(),
            new CustomerRequestListQuery(2, 10, " new "),
            CancellationToken.None);

        Assert.NotNull(repository.LastListQuery);
        Assert.Equal(user.Id, repository.LastListQuery.UserId);
        Assert.Equal(2, repository.LastListQuery.Page);
        Assert.Equal(10, repository.LastListQuery.PageSize);
        Assert.Equal("new", repository.LastListQuery.Status);

        Assert.Equal(2, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(21, response.TotalItems);
        Assert.Equal(3, response.TotalPages);
        var item = Assert.Single(response.Items);
        Assert.Equal("Р—Рљ26-0002", item.Number);
        Assert.Equal("new", item.Status.Code);
        Assert.Equal(2, item.ItemsCount);
    }

    [Fact]
    public async Task GetRequestsAsync_RejectsInvalidStatus()
    {
        var service = CreateService(new CapturingCustomerRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetRequestsAsync(
                new DefaultHttpContext(),
                new CustomerRequestListQuery(1, 20, "archived"),
                CancellationToken.None));

        Assert.Equal("request.invalid_status", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GetRequestAsync_NormalizesNumberAndReadsRequestForCurrentUser()
    {
        var user = TestUser();
        var repository = new CapturingCustomerRequestRepository();
        var service = new CustomerRequestService(
            new ReturningCurrentUserService(user),
            repository,
            new RequestReferenceData(),
            new LineCom.Api.Modules.Catalog.Services.PublicCatalogReferenceData());

        var response = await service.GetRequestAsync(
            new DefaultHttpContext(),
            " Р—Рљ26-0002 ",
            CancellationToken.None);

        Assert.NotNull(repository.LastDetailQuery);
        Assert.Equal(user.Id, repository.LastDetailQuery.UserId);
        Assert.Equal("Р—Рљ26-0002", repository.LastDetailQuery.Number);

        Assert.Equal("Р—Рљ26-0002", response.Number);
        Assert.Equal("Ivan Petrov", response.Customer?.Name);
        Assert.Equal("OOO Cable", response.Organization?.Name);
        Assert.Equal("created", Assert.Single(response.History!).Event);
    }

    [Fact]
    public async Task GetRequestAsync_WhenRepositoryReturnsNull_ThrowsNotFound()
    {
        var service = CreateService(new MissingCustomerRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetRequestAsync(
                new DefaultHttpContext(),
                "Р—Рљ26-4040",
                CancellationToken.None));

        Assert.Equal("request.not_found", exception.Code);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    private static CustomerRequestService CreateService(ICustomerRequestRepository repository)
    {
        return new CustomerRequestService(
            new ReturningCurrentUserService(TestUser()),
            repository,
            new RequestReferenceData(),
            new LineCom.Api.Modules.Catalog.Services.PublicCatalogReferenceData());
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

    private sealed class CapturingCustomerRequestRepository : ICustomerRequestRepository
    {
        public CustomerRequestDraft? LastDraft { get; private set; }
        public CustomerRequestReadListQuery? LastListQuery { get; private set; }
        public CustomerRequestReadDetailQuery? LastDetailQuery { get; private set; }

        public Task<CreatedCustomerRequestRecord> CreateAsync(
            CustomerRequestDraft draft,
            CancellationToken cancellationToken = default)
        {
            LastDraft = draft;

            return Task.FromResult(new CreatedCustomerRequestRecord(
                "ЗК26-0001",
                "new",
                draft.Source,
                draft.CustomerComment,
                new DateTimeOffset(2026, 5, 7, 10, 15, 30, TimeSpan.Zero),
                new[]
                {
                    new CreatedCustomerRequestItemRecord(
                        draft.Items[0].ProductId,
                        "Кабель U/UTP Cat 5e 4 пары CU 305 м",
                        "LC-UTP5E-CU-305",
                        "coil",
                        "305 м",
                        draft.Items[0].Quantity,
                        draft.Items[0].CustomerComment)
                }));
        }

        public Task<CustomerRequestListRecordResponse> GetRequestsAsync(
            CustomerRequestReadListQuery query,
            CancellationToken cancellationToken = default)
        {
            LastListQuery = query;

            return Task.FromResult(new CustomerRequestListRecordResponse(
                new[]
                {
                    new CustomerRequestListRecord(
                        "Р—Рљ26-0002",
                        "new",
                        "cart",
                        2,
                        "Need delivery date",
                        new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero))
                },
                TotalItems: 21));
        }

        public Task<CustomerRequestDetailRecord?> GetRequestAsync(
            CustomerRequestReadDetailQuery query,
            CancellationToken cancellationToken = default)
        {
            LastDetailQuery = query;

            return Task.FromResult<CustomerRequestDetailRecord?>(TestDetailRecord(query.Number));
        }

        private static CustomerRequestDetailRecord TestDetailRecord(string number)
        {
            var productId = Guid.Parse("3d6e4e11-2a88-4d01-9d44-1cfb7400924f");

            return new CustomerRequestDetailRecord(
                number,
                "new",
                "cart",
                new RequestCustomerSnapshotRecord(
                    "Ivan Petrov",
                    "ivan@example.com",
                    "+79000000000"),
                new RequestOrganizationSnapshotRecord(
                    "OOO Cable",
                    "7700000000",
                    "Ivan Petrov"),
                "Need delivery date",
                new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero),
                new[]
                {
                    new CreatedCustomerRequestItemRecord(
                        productId,
                        "РљР°Р±РµР»СЊ U/UTP Cat 5e 4 РїР°СЂС‹ CU 305 Рј",
                        "LC-UTP5E-CU-305",
                        "coil",
                        "305 Рј",
                        2,
                        "Replace with analogue if faster")
                },
                new[]
                {
                    new CustomerRequestHistoryRecord(
                        "created",
                        "Р—Р°СЏРІРєР° СЃРѕР·РґР°РЅР°.",
                        new DateTimeOffset(2026, 5, 7, 11, 15, 30, TimeSpan.Zero))
                });
        }
    }

    private sealed class ProductUnavailableCustomerRequestRepository : ICustomerRequestRepository
    {
        public Task<CreatedCustomerRequestRecord> CreateAsync(
            CustomerRequestDraft draft,
            CancellationToken cancellationToken = default)
        {
            throw new ProductNotAvailableException();
        }

        public Task<CustomerRequestListRecordResponse> GetRequestsAsync(
            CustomerRequestReadListQuery query,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CustomerRequestDetailRecord?> GetRequestAsync(
            CustomerRequestReadDetailQuery query,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class MissingCustomerRequestRepository : ICustomerRequestRepository
    {
        public Task<CreatedCustomerRequestRecord> CreateAsync(
            CustomerRequestDraft draft,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CustomerRequestListRecordResponse> GetRequestsAsync(
            CustomerRequestReadListQuery query,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CustomerRequestDetailRecord?> GetRequestAsync(
            CustomerRequestReadDetailQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CustomerRequestDetailRecord?>(null);
        }
    }
}
