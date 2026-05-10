# Admin Request Processing v1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Построить первый рабочий контур обработки заявок для ролей `seller` и `admin`: общий список, карточка, смена статуса, один текущий внутренний комментарий и история изменений.

**Architecture:** Backend остается модульным монолитом ASP.NET Core с отдельным admin-срезом внутри `Modules/Requests`. Customer endpoints не получают admin-only поля; admin endpoints используют отдельные DTO/service/repository contracts и общую справочную модель статусов без `quoted`.

**Tech Stack:** ASP.NET Core Web API, Cookie Auth, Npgsql, Dapper, DbUp SQL migrations, xUnit, Next.js App Router, React, TypeScript, Vitest, React Testing Library.

---

## File Structure

Backend:

- Modify `apps/dbmigrator/Migrations/004_requests.sql`: убрать `quoted` из исходной release-схемы, чтобы новые базы создавались сразу с 4 статусами.
- Create `apps/dbmigrator/Migrations/006_admin_request_status_cleanup.sql`: миграция для существующих БД, переводящая `quoted` в `in_progress` и пересоздающая constraints.
- Modify `apps/api/Modules/Requests/Services/RequestReferenceData.cs`: убрать `quoted` из справочника статусов.
- Create `apps/api/Modules/Requests/DTOs/AdminRequestDtos.cs`: DTO admin list/detail/status/comment.
- Create `apps/api/Modules/Requests/Services/IAdminRequestService.cs`.
- Create `apps/api/Modules/Requests/Services/AdminRequestService.cs`: auth role checks, normalization, mapping records to DTO.
- Create `apps/api/Modules/Requests/Repositories/IAdminRequestRepository.cs`: admin query/update records.
- Create `apps/api/Modules/Requests/Repositories/DapperAdminRequestRepository.cs`: Dapper read/update operations and transaction boundaries for mutations.
- Create `apps/api/Modules/Requests/Repositories/AdminRequestSql.cs`: SQL constants for admin endpoints.
- Create `apps/api/Modules/Requests/Controllers/AdminRequestsController.cs`: `/api/admin/requests`.
- Modify `apps/api/Modules/Requests/RequestServiceCollectionExtensions.cs`: register admin service/repository.

Backend tests:

- Modify `tests/LineCom.Api.Tests/Infrastructure/Database/RequestCoreMigrationTests.cs`.
- Create `tests/LineCom.Api.Tests/Infrastructure/Database/AdminRequestStatusCleanupMigrationTests.cs`.
- Create `tests/LineCom.Api.Tests/Modules/Requests/RequestReferenceDataTests.cs`.
- Create `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestServiceTests.cs`.
- Create `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestsEndpointTests.cs`.
- Create `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestSqlTests.cs`.
- Create `tests/LineCom.Api.Tests/Modules/Requests/DapperAdminRequestRepositoryMappingTests.cs`.

Frontend:

- Modify `apps/front/src/lib/routes.ts`: admin routes.
- Create `apps/front/src/lib/api/admin-requests.ts`: typed admin API client.
- Create `apps/front/src/components/admin/admin-request-list.tsx`.
- Create `apps/front/src/components/admin/admin-request-detail.tsx`.
- Create `apps/front/src/app/admin/requests/page.tsx`.
- Create `apps/front/src/app/admin/requests/requests-page-client.tsx`.
- Create `apps/front/src/app/admin/requests/[number]/page.tsx`.
- Create `apps/front/src/app/admin/requests/[number]/request-detail-page-client.tsx`.
- Modify `apps/front/src/app/globals.css`: admin layout styles following current B2B style.
- Modify `apps/front/src/components/layout/site-header.tsx`: show admin link when role is `seller` or `admin` if current auth state is available; this is navigation convenience only, not access control.

Frontend tests:

- Create `apps/front/src/lib/api/admin-requests.test.ts`.
- Create `apps/front/src/components/admin/admin-request-list.test.tsx`.
- Create `apps/front/src/components/admin/admin-request-detail.test.tsx`.
- Create `apps/front/src/app/admin/requests/requests-page-client.test.tsx`.
- Create `apps/front/src/app/admin/requests/[number]/request-detail-page-client.test.tsx`.

Docs:

- Create `vault/Человекочитаемое/Admin Request Processing API.md`.
- Update `vault/Человекочитаемое/Продуктовая модель.md`.
- Update `vault/Человекочитаемое/Auth Request Core API.md`.
- Create `vault/Человекочитаемое/Admin Request Processing iterations.md`.

---

### Task 1: Four-status request model and migration cleanup

**Files:**
- Modify: `apps/dbmigrator/Migrations/004_requests.sql`
- Create: `apps/dbmigrator/Migrations/006_admin_request_status_cleanup.sql`
- Modify: `tests/LineCom.Api.Tests/Infrastructure/Database/RequestCoreMigrationTests.cs`
- Create: `tests/LineCom.Api.Tests/Infrastructure/Database/AdminRequestStatusCleanupMigrationTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Requests/RequestReferenceDataTests.cs`
- Modify: `apps/api/Modules/Requests/Services/RequestReferenceData.cs`

- [ ] **Step 1: Write failing migration tests for four-status model**

Add these assertions to `RequestCoreMigrationTests.RequestCore_ConstrainsReleaseValues`:

```csharp
[InlineData("CONSTRAINT ck_requests_status CHECK (status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
[InlineData("CONSTRAINT ck_request_history_old_status CHECK (old_status IS NULL OR old_status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
[InlineData("CONSTRAINT ck_request_history_new_status CHECK (new_status IS NULL OR new_status IN ('new', 'in_progress', 'completed', 'cancelled'))")]
```

Remove the old inline data containing `quoted`.

Create `AdminRequestStatusCleanupMigrationTests.cs`:

```csharp
namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class AdminRequestStatusCleanupMigrationTests
{
    private static readonly string CleanupSql = ReadMigration("006_admin_request_status_cleanup.sql");

    [Fact]
    public void Cleanup_MapsQuotedStatusToInProgress()
    {
        Assert.Contains("UPDATE requests", CleanupSql);
        Assert.Contains("status = 'in_progress'", CleanupSql);
        Assert.Contains("status = 'quoted'", CleanupSql);
        Assert.Contains("UPDATE request_history", CleanupSql);
        Assert.Contains("old_status = CASE WHEN old_status = 'quoted' THEN 'in_progress'", CleanupSql);
        Assert.Contains("new_status = CASE WHEN new_status = 'quoted' THEN 'in_progress'", CleanupSql);
    }

    [Fact]
    public void Cleanup_RecreatesConstraintsWithoutQuoted()
    {
        Assert.Contains("DROP CONSTRAINT IF EXISTS ck_requests_status", CleanupSql);
        Assert.Contains("DROP CONSTRAINT IF EXISTS ck_request_history_old_status", CleanupSql);
        Assert.Contains("DROP CONSTRAINT IF EXISTS ck_request_history_new_status", CleanupSql);
        Assert.Contains("CHECK (status IN ('new', 'in_progress', 'completed', 'cancelled'))", CleanupSql);
        Assert.Contains("CHECK (old_status IS NULL OR old_status IN ('new', 'in_progress', 'completed', 'cancelled'))", CleanupSql);
        Assert.Contains("CHECK (new_status IS NULL OR new_status IN ('new', 'in_progress', 'completed', 'cancelled'))", CleanupSql);
        Assert.DoesNotContain("'quoted'", CleanupSql.Replace("status = 'quoted'", string.Empty, StringComparison.Ordinal));
    }

    private static string ReadMigration(string fileName)
    {
        var migrationFile = Path.Combine(FindRepositoryRoot(), "apps", "dbmigrator", "Migrations", fileName);
        return File.ReadAllText(migrationFile);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionFile = Path.Combine(directory.FullName, "LineCom.sln");
            if (File.Exists(solutionFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
```

- [ ] **Step 2: Write failing reference-data tests**

Create `RequestReferenceDataTests.cs`:

```csharp
using LineCom.Api.Modules.Requests.Services;
using LineCom.Api.Shared.Errors;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class RequestReferenceDataTests
{
    [Theory]
    [InlineData("new", "Новая")]
    [InlineData("in_progress", "В работе")]
    [InlineData("completed", "Завершена")]
    [InlineData("cancelled", "Отменена")]
    public void GetStatus_ReturnsReleaseStatusLabels(string code, string label)
    {
        var data = new RequestReferenceData();

        var status = data.GetStatus(code);

        Assert.Equal(code, status.Code);
        Assert.Equal(label, status.Label);
    }

    [Fact]
    public void GetStatus_RejectsQuoted()
    {
        var data = new RequestReferenceData();

        var exception = Assert.Throws<ApiException>(() => data.GetStatus("quoted"));

        Assert.Equal("request.invalid_status", exception.Code);
    }
}
```

- [ ] **Step 3: Run failing backend tests**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter "RequestCoreMigrationTests|AdminRequestStatusCleanupMigrationTests|RequestReferenceDataTests"
```

Expected: FAIL because `006_admin_request_status_cleanup.sql` does not exist and `RequestReferenceData` still accepts `quoted`.

- [ ] **Step 4: Implement migration and reference data**

Edit `004_requests.sql` constraints to remove `quoted`.

Create `006_admin_request_status_cleanup.sql`:

```sql
UPDATE requests
SET status = 'in_progress'
WHERE status = 'quoted';

UPDATE request_history
SET
    old_status = CASE WHEN old_status = 'quoted' THEN 'in_progress' ELSE old_status END,
    new_status = CASE WHEN new_status = 'quoted' THEN 'in_progress' ELSE new_status END
WHERE old_status = 'quoted'
   OR new_status = 'quoted';

ALTER TABLE requests
    DROP CONSTRAINT IF EXISTS ck_requests_status;

ALTER TABLE requests
    ADD CONSTRAINT ck_requests_status CHECK (status IN ('new', 'in_progress', 'completed', 'cancelled'));

ALTER TABLE request_history
    DROP CONSTRAINT IF EXISTS ck_request_history_old_status;

ALTER TABLE request_history
    ADD CONSTRAINT ck_request_history_old_status
    CHECK (old_status IS NULL OR old_status IN ('new', 'in_progress', 'completed', 'cancelled'));

ALTER TABLE request_history
    DROP CONSTRAINT IF EXISTS ck_request_history_new_status;

ALTER TABLE request_history
    ADD CONSTRAINT ck_request_history_new_status
    CHECK (new_status IS NULL OR new_status IN ('new', 'in_progress', 'completed', 'cancelled'));
```

Update `RequestReferenceData.cs` status labels:

```csharp
private static readonly IReadOnlyDictionary<string, string> StatusLabels =
    new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["new"] = "Новая",
        ["in_progress"] = "В работе",
        ["completed"] = "Завершена",
        ["cancelled"] = "Отменена"
    };
```

- [ ] **Step 5: Run tests and commit**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter "RequestCoreMigrationTests|AdminRequestStatusCleanupMigrationTests|RequestReferenceDataTests"
```

Expected: PASS.

Commit:

```powershell
git add apps/dbmigrator/Migrations/004_requests.sql apps/dbmigrator/Migrations/006_admin_request_status_cleanup.sql tests/LineCom.Api.Tests/Infrastructure/Database/RequestCoreMigrationTests.cs tests/LineCom.Api.Tests/Infrastructure/Database/AdminRequestStatusCleanupMigrationTests.cs tests/LineCom.Api.Tests/Modules/Requests/RequestReferenceDataTests.cs apps/api/Modules/Requests/Services/RequestReferenceData.cs
git commit -m "fix: reduce request statuses to release model"
```

---

### Task 2: Admin request DTOs, service contract, and role guard

**Files:**
- Create: `apps/api/Modules/Requests/DTOs/AdminRequestDtos.cs`
- Create: `apps/api/Modules/Requests/Services/IAdminRequestService.cs`
- Create: `apps/api/Modules/Requests/Services/AdminRequestService.cs`
- Create: `apps/api/Modules/Requests/Repositories/IAdminRequestRepository.cs`
- Modify: `apps/api/Modules/Requests/RequestServiceCollectionExtensions.cs`
- Test: `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestServiceTests.cs`

- [ ] **Step 1: Write failing service role tests**

Create `AdminRequestServiceTests.cs` with the first role tests:

```csharp
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

        await service.GetRequestsAsync(new DefaultHttpContext(), new AdminRequestListQuery(1, 20, null, null, null, null), CancellationToken.None);

        Assert.NotNull(repository.LastListQuery);
    }

    [Fact]
    public async Task GetRequestsAsync_RejectsCustomer()
    {
        var service = CreateService("customer", new CapturingAdminRequestRepository());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetRequestsAsync(new DefaultHttpContext(), new AdminRequestListQuery(1, 20, null, null, null, null), CancellationToken.None));

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

        public Task<AuthSessionDto> GetCurrentSessionAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthSessionDto(_user, "csrf-token"));
        }
    }

    private sealed class CapturingAdminRequestRepository : IAdminRequestRepository
    {
        public AdminRequestReadListQuery? LastListQuery { get; private set; }

        public Task<AdminRequestListRecordResponse> GetRequestsAsync(AdminRequestReadListQuery query, CancellationToken cancellationToken = default)
        {
            LastListQuery = query;
            return Task.FromResult(new AdminRequestListRecordResponse(Array.Empty<AdminRequestListRecord>(), 0));
        }

        public Task<AdminRequestDetailRecord?> GetRequestAsync(string number, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AdminRequestDetailRecord?> UpdateStatusAsync(AdminRequestStatusUpdate update, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(AdminRequestInternalCommentUpdate update, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
```

- [ ] **Step 2: Run failing service tests**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter AdminRequestServiceTests
```

Expected: FAIL because admin DTO/service/repository types do not exist.

- [ ] **Step 3: Add DTOs and contracts**

Create `AdminRequestDtos.cs`:

```csharp
namespace LineCom.Api.Modules.Requests.DTOs;

public sealed record AdminRequestListQuery(
    int? Page,
    int? PageSize,
    string? Status,
    string? Number,
    string? Contact,
    string? Organization);

public sealed record AdminRequestListResponse(
    IReadOnlyList<AdminRequestListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AdminRequestListItemDto(
    string Number,
    RequestStatusDto Status,
    string Source,
    int ItemsCount,
    RequestCustomerSnapshotDto Customer,
    RequestOrganizationSnapshotDto? Organization,
    string? CustomerComment,
    string? InternalComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminRequestDetailDto(
    string Number,
    RequestStatusDto Status,
    string Source,
    RequestCustomerSnapshotDto Customer,
    RequestOrganizationSnapshotDto? Organization,
    string? CustomerComment,
    string? InternalComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CustomerRequestItemDto> Items,
    IReadOnlyList<CustomerRequestHistoryDto> History);

public sealed record UpdateAdminRequestStatusCommand(string? Status);

public sealed record UpdateAdminRequestInternalCommentCommand(string? InternalComment);
```

Create `IAdminRequestService.cs`:

```csharp
using LineCom.Api.Modules.Requests.DTOs;

namespace LineCom.Api.Modules.Requests.Services;

public interface IAdminRequestService
{
    Task<AdminRequestListResponse> GetRequestsAsync(HttpContext httpContext, AdminRequestListQuery query, CancellationToken cancellationToken = default);
    Task<AdminRequestDetailDto> GetRequestAsync(HttpContext httpContext, string number, CancellationToken cancellationToken = default);
    Task<AdminRequestDetailDto> UpdateStatusAsync(HttpContext httpContext, string number, UpdateAdminRequestStatusCommand command, CancellationToken cancellationToken = default);
    Task<AdminRequestDetailDto> UpdateInternalCommentAsync(HttpContext httpContext, string number, UpdateAdminRequestInternalCommentCommand command, CancellationToken cancellationToken = default);
}
```

Create `IAdminRequestRepository.cs` with records matching the service test names:

```csharp
namespace LineCom.Api.Modules.Requests.Repositories;

public sealed record AdminRequestReadListQuery(
    int Page,
    int PageSize,
    string? Status,
    string? Number,
    string? Contact,
    string? Organization);

public sealed record AdminRequestListRecordResponse(
    IReadOnlyList<AdminRequestListRecord> Items,
    int TotalItems);

public sealed record AdminRequestListRecord(
    string Number,
    string Status,
    string Source,
    int ItemsCount,
    RequestCustomerSnapshotRecord Customer,
    RequestOrganizationSnapshotRecord? Organization,
    string? CustomerComment,
    string? InternalComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminRequestDetailRecord(
    string Number,
    string Status,
    string Source,
    RequestCustomerSnapshotRecord Customer,
    RequestOrganizationSnapshotRecord? Organization,
    string? CustomerComment,
    string? InternalComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CreatedCustomerRequestItemRecord> Items,
    IReadOnlyList<CustomerRequestHistoryRecord> History);

public sealed record AdminRequestStatusUpdate(
    string Number,
    string Status,
    Guid ActorUserId);

public sealed record AdminRequestInternalCommentUpdate(
    string Number,
    string? InternalComment,
    Guid ActorUserId);

public interface IAdminRequestRepository
{
    Task<AdminRequestListRecordResponse> GetRequestsAsync(AdminRequestReadListQuery query, CancellationToken cancellationToken = default);
    Task<AdminRequestDetailRecord?> GetRequestAsync(string number, CancellationToken cancellationToken = default);
    Task<AdminRequestDetailRecord?> UpdateStatusAsync(AdminRequestStatusUpdate update, CancellationToken cancellationToken = default);
    Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(AdminRequestInternalCommentUpdate update, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Implement minimal service role guard and registration**

Create `AdminRequestService.cs` with role guard and list mapping. Detail and mutation methods throw `NotSupportedException` in this task because Task 3 immediately replaces them after adding the behavior tests:

```csharp
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Repositories;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Requests.Services;

public sealed class AdminRequestService : IAdminRequestService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 60;

    private readonly IAuthCurrentUserService _currentUserService;
    private readonly IAdminRequestRepository _repository;
    private readonly IRequestReferenceData _requestReferenceData;
    private readonly IPublicCatalogReferenceData _catalogReferenceData;

    public AdminRequestService(
        IAuthCurrentUserService currentUserService,
        IAdminRequestRepository repository,
        IRequestReferenceData requestReferenceData,
        IPublicCatalogReferenceData catalogReferenceData)
    {
        _currentUserService = currentUserService;
        _repository = repository;
        _requestReferenceData = requestReferenceData;
        _catalogReferenceData = catalogReferenceData;
    }

    public async Task<AdminRequestListResponse> GetRequestsAsync(HttpContext httpContext, AdminRequestListQuery query, CancellationToken cancellationToken = default)
    {
        await RequireStaffAsync(httpContext, cancellationToken);
        var status = NormalizeText(query.Status);
        if (status is not null)
        {
            _requestReferenceData.GetStatus(status);
        }

        var page = NormalizePage(query.Page);
        var pageSize = NormalizePageSize(query.PageSize);
        var result = await _repository.GetRequestsAsync(
            new AdminRequestReadListQuery(
                page,
                pageSize,
                status,
                NormalizeText(query.Number),
                NormalizeText(query.Contact),
                NormalizeText(query.Organization)),
            cancellationToken);
        var totalPages = result.TotalItems == 0 ? 0 : (int)Math.Ceiling(result.TotalItems / (double)pageSize);
        var items = result.Items.Select(ToListDto).ToArray();
        return new AdminRequestListResponse(items, page, pageSize, result.TotalItems, totalPages);
    }

    public Task<AdminRequestDetailDto> GetRequestAsync(HttpContext httpContext, string number, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<AdminRequestDetailDto> UpdateStatusAsync(HttpContext httpContext, string number, UpdateAdminRequestStatusCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<AdminRequestDetailDto> UpdateInternalCommentAsync(HttpContext httpContext, string number, UpdateAdminRequestInternalCommentCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    private async Task<CurrentUserDto> RequireStaffAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var session = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        if (session.User.Role is "seller" or "admin")
        {
            return session.User;
        }

        throw AuthErrors.Forbidden();
    }

    private AdminRequestListItemDto ToListDto(AdminRequestListRecord record)
    {
        return new AdminRequestListItemDto(
            record.Number,
            _requestReferenceData.GetStatus(record.Status),
            record.Source,
            record.ItemsCount,
            new RequestCustomerSnapshotDto(record.Customer.Name, record.Customer.Email, record.Customer.Phone),
            record.Organization is null ? null : new RequestOrganizationSnapshotDto(record.Organization.Name, record.Organization.Inn, record.Organization.ContactPerson),
            record.CustomerComment,
            record.InternalComment,
            record.CreatedAt,
            record.UpdatedAt);
    }

    private static int NormalizePage(int? value) => value is null or < 1 ? DefaultPage : value.Value;
    private static int NormalizePageSize(int? value) => value is null or < 1 ? DefaultPageSize : Math.Min(value.Value, MaxPageSize);
    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
```

Register in `RequestServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAdminRequestService, AdminRequestService>();
services.AddScoped<IAdminRequestRepository, DapperAdminRequestRepository>();
```

Create `DapperAdminRequestRepository.cs` as a compiling stub before Task 4 replaces it with Dapper behavior:

```csharp
public sealed class DapperAdminRequestRepository : IAdminRequestRepository
{
    public Task<AdminRequestListRecordResponse> GetRequestsAsync(AdminRequestReadListQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<AdminRequestDetailRecord?> GetRequestAsync(string number, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<AdminRequestDetailRecord?> UpdateStatusAsync(AdminRequestStatusUpdate update, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(AdminRequestInternalCommentUpdate update, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
```

- [ ] **Step 5: Run tests and commit**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter AdminRequestServiceTests
```

Expected: PASS for role guard tests.

Commit:

```powershell
git add apps/api/Modules/Requests/DTOs/AdminRequestDtos.cs apps/api/Modules/Requests/Services/IAdminRequestService.cs apps/api/Modules/Requests/Services/AdminRequestService.cs apps/api/Modules/Requests/Repositories/IAdminRequestRepository.cs apps/api/Modules/Requests/Repositories/DapperAdminRequestRepository.cs apps/api/Modules/Requests/RequestServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Requests/AdminRequestServiceTests.cs
git commit -m "feat: add admin request service contract"
```

---

### Task 3: Admin service detail and mutation behavior

**Files:**
- Modify: `apps/api/Modules/Requests/Services/AdminRequestService.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestServiceTests.cs`

- [ ] **Step 1: Add failing tests for query normalization, detail, status, comment**

Extend `AdminRequestServiceTests.cs`:

```csharp
[Fact]
public async Task GetRequestsAsync_NormalizesFilters()
{
    var repository = new CapturingAdminRequestRepository();
    var service = CreateService("seller", repository);

    await service.GetRequestsAsync(
        new DefaultHttpContext(),
        new AdminRequestListQuery(2, 10, " new ", " ЗК26 ", " ivan@example.com ", " ООО "),
        CancellationToken.None);

    Assert.NotNull(repository.LastListQuery);
    Assert.Equal(2, repository.LastListQuery.Page);
    Assert.Equal(10, repository.LastListQuery.PageSize);
    Assert.Equal("new", repository.LastListQuery.Status);
    Assert.Equal("ЗК26", repository.LastListQuery.Number);
    Assert.Equal("ivan@example.com", repository.LastListQuery.Contact);
    Assert.Equal("ООО", repository.LastListQuery.Organization);
}

[Fact]
public async Task GetRequestsAsync_RejectsQuotedStatus()
{
    var service = CreateService("seller", new CapturingAdminRequestRepository());

    var exception = await Assert.ThrowsAsync<ApiException>(() =>
        service.GetRequestsAsync(new DefaultHttpContext(), new AdminRequestListQuery(1, 20, "quoted", null, null, null), CancellationToken.None));

    Assert.Equal("request.invalid_status", exception.Code);
}

[Fact]
public async Task GetRequestAsync_ReturnsDetail()
{
    var service = CreateService("admin", new CapturingAdminRequestRepository());

    var response = await service.GetRequestAsync(new DefaultHttpContext(), " ЗК26-0008 ", CancellationToken.None);

    Assert.Equal("ЗК26-0008", response.Number);
    Assert.Equal("Новая", response.Status.Label);
    Assert.Equal("Позвонить после 15:00.", response.InternalComment);
    Assert.Equal("Ivan Petrov", response.Customer.Name);
    Assert.Equal("ООО Сеть", response.Organization?.Name);
    Assert.Single(response.Items);
    Assert.Single(response.History);
}

[Fact]
public async Task UpdateStatusAsync_NormalizesStatusAndPassesActor()
{
    var repository = new CapturingAdminRequestRepository();
    var service = CreateService("seller", repository);

    var response = await service.UpdateStatusAsync(
        new DefaultHttpContext(),
        " ЗК26-0008 ",
        new UpdateAdminRequestStatusCommand(" in_progress "),
        CancellationToken.None);

    Assert.Equal("ЗК26-0008", repository.LastStatusUpdate?.Number);
    Assert.Equal("in_progress", repository.LastStatusUpdate?.Status);
    Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), repository.LastStatusUpdate?.ActorUserId);
    Assert.Equal("in_progress", response.Status.Code);
}

[Fact]
public async Task UpdateInternalCommentAsync_NormalizesBlankToNull()
{
    var repository = new CapturingAdminRequestRepository();
    var service = CreateService("admin", repository);

    await service.UpdateInternalCommentAsync(
        new DefaultHttpContext(),
        "ЗК26-0008",
        new UpdateAdminRequestInternalCommentCommand("   "),
        CancellationToken.None);

    Assert.Null(repository.LastCommentUpdate?.InternalComment);
}
```

Update `CapturingAdminRequestRepository` to return a reusable detail record and capture mutation records:

```csharp
public AdminRequestStatusUpdate? LastStatusUpdate { get; private set; }
public AdminRequestInternalCommentUpdate? LastCommentUpdate { get; private set; }

public Task<AdminRequestDetailRecord?> GetRequestAsync(string number, CancellationToken cancellationToken = default)
{
    return Task.FromResult<AdminRequestDetailRecord?>(TestDetail(number, "new", "Позвонить после 15:00."));
}

public Task<AdminRequestDetailRecord?> UpdateStatusAsync(AdminRequestStatusUpdate update, CancellationToken cancellationToken = default)
{
    LastStatusUpdate = update;
    return Task.FromResult<AdminRequestDetailRecord?>(TestDetail(update.Number, update.Status, "Позвонить после 15:00."));
}

public Task<AdminRequestDetailRecord?> UpdateInternalCommentAsync(AdminRequestInternalCommentUpdate update, CancellationToken cancellationToken = default)
{
    LastCommentUpdate = update;
    return Task.FromResult<AdminRequestDetailRecord?>(TestDetail(update.Number, "new", update.InternalComment));
}

private static AdminRequestDetailRecord TestDetail(string number, string status, string? internalComment)
{
    return new AdminRequestDetailRecord(
        number,
        status,
        "cart",
        new RequestCustomerSnapshotRecord("Ivan Petrov", "ivan@example.com", "+79000000000"),
        new RequestOrganizationSnapshotRecord("ООО Сеть", "7700000000", "Ivan Petrov"),
        "Need delivery date",
        internalComment,
        new DateTimeOffset(2026, 5, 10, 12, 40, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 5, 10, 13, 10, 0, TimeSpan.Zero),
        new[]
        {
            new CreatedCustomerRequestItemRecord(
                Guid.Parse("3d6e4e11-2a88-4d01-9d44-1cfb7400924f"),
                "Кабель U/UTP Cat 5e 305 м",
                "LC-UTP5E-CU-305",
                "coil",
                "305 м",
                2,
                null)
        },
        new[]
        {
            new CustomerRequestHistoryRecord(
                "created",
                "Заявка создана.",
                new DateTimeOffset(2026, 5, 10, 12, 40, 0, TimeSpan.Zero))
        });
}
```

- [ ] **Step 2: Run failing service tests**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter AdminRequestServiceTests
```

Expected: FAIL because detail and mutation methods are not implemented.

- [ ] **Step 3: Implement detail and mutation methods**

Add to `AdminRequestService.cs`:

```csharp
public async Task<AdminRequestDetailDto> GetRequestAsync(HttpContext httpContext, string number, CancellationToken cancellationToken = default)
{
    await RequireStaffAsync(httpContext, cancellationToken);
    var normalizedNumber = NormalizeText(number);
    if (normalizedNumber is null)
    {
        throw RequestErrors.NotFound();
    }

    var record = await _repository.GetRequestAsync(normalizedNumber, cancellationToken);
    if (record is null)
    {
        throw RequestErrors.NotFound();
    }

    return ToDetailDto(record);
}

public async Task<AdminRequestDetailDto> UpdateStatusAsync(HttpContext httpContext, string number, UpdateAdminRequestStatusCommand command, CancellationToken cancellationToken = default)
{
    var actor = await RequireStaffAsync(httpContext, cancellationToken);
    var normalizedNumber = NormalizeText(number);
    var status = NormalizeText(command.Status);
    if (normalizedNumber is null || status is null)
    {
        throw AuthErrors.InvalidRequest();
    }

    _requestReferenceData.GetStatus(status);
    var record = await _repository.UpdateStatusAsync(new AdminRequestStatusUpdate(normalizedNumber, status, actor.Id), cancellationToken);
    if (record is null)
    {
        throw RequestErrors.NotFound();
    }

    return ToDetailDto(record);
}

public async Task<AdminRequestDetailDto> UpdateInternalCommentAsync(HttpContext httpContext, string number, UpdateAdminRequestInternalCommentCommand command, CancellationToken cancellationToken = default)
{
    var actor = await RequireStaffAsync(httpContext, cancellationToken);
    var normalizedNumber = NormalizeText(number);
    if (normalizedNumber is null)
    {
        throw RequestErrors.NotFound();
    }

    var record = await _repository.UpdateInternalCommentAsync(
        new AdminRequestInternalCommentUpdate(normalizedNumber, NormalizeText(command.InternalComment), actor.Id),
        cancellationToken);
    if (record is null)
    {
        throw RequestErrors.NotFound();
    }

    return ToDetailDto(record);
}

private AdminRequestDetailDto ToDetailDto(AdminRequestDetailRecord record)
{
    return new AdminRequestDetailDto(
        record.Number,
        _requestReferenceData.GetStatus(record.Status),
        record.Source,
        new RequestCustomerSnapshotDto(record.Customer.Name, record.Customer.Email, record.Customer.Phone),
        record.Organization is null ? null : new RequestOrganizationSnapshotDto(record.Organization.Name, record.Organization.Inn, record.Organization.ContactPerson),
        record.CustomerComment,
        record.InternalComment,
        record.CreatedAt,
        record.UpdatedAt,
        record.Items.Select(item => new CustomerRequestItemDto(
            item.ProductId,
            item.ProductName,
            item.ProductSku,
            _catalogReferenceData.GetSaleUnit(item.SaleUnit),
            item.UnitQuantity,
            item.Quantity,
            item.CustomerComment)).ToArray(),
        record.History.Select(history => new CustomerRequestHistoryDto(
            history.Event,
            history.Message,
            history.CreatedAt)).ToArray());
}
```

- [ ] **Step 4: Run tests and commit**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter AdminRequestServiceTests
```

Expected: PASS.

Commit:

```powershell
git add apps/api/Modules/Requests/Services/AdminRequestService.cs tests/LineCom.Api.Tests/Modules/Requests/AdminRequestServiceTests.cs
git commit -m "feat: map admin request service operations"
```

---

### Task 4: Admin request SQL and Dapper repository

**Files:**
- Create: `apps/api/Modules/Requests/Repositories/AdminRequestSql.cs`
- Modify: `apps/api/Modules/Requests/Repositories/DapperAdminRequestRepository.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestSqlTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Requests/DapperAdminRequestRepositoryMappingTests.cs`

- [ ] **Step 1: Write SQL contract tests**

Create `AdminRequestSqlTests.cs`:

```csharp
using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class AdminRequestSqlTests
{
    [Fact]
    public void FindRequests_FiltersWithoutUserScope()
    {
        Assert.Contains("FROM requests request", AdminRequestSql.FindRequests);
        Assert.DoesNotContain("request.user_id = @UserId", AdminRequestSql.FindRequests);
        Assert.Contains("(@Status IS NULL OR request.status = @Status)", AdminRequestSql.FindRequests);
        Assert.Contains("(@Number IS NULL OR request.number ILIKE '%' || @Number || '%')", AdminRequestSql.FindRequests);
        Assert.Contains("request.customer_email::text ILIKE '%' || @Contact || '%'", AdminRequestSql.FindRequests);
        Assert.Contains("request.organization_inn ILIKE '%' || @Organization || '%'", AdminRequestSql.FindRequests);
    }

    [Fact]
    public void UpdateStatus_IsTransactionalFriendlyAndIdempotent()
    {
        Assert.Contains("FOR UPDATE", AdminRequestSql.FindRequestForUpdate);
        Assert.Contains("UPDATE requests", AdminRequestSql.UpdateStatus);
        Assert.Contains("WHERE id = @RequestId", AdminRequestSql.UpdateStatus);
        Assert.Contains("INSERT INTO request_history", AdminRequestSql.InsertStatusChangedHistory);
        Assert.Contains("status_changed", AdminRequestSql.InsertStatusChangedHistory);
    }

    [Fact]
    public void UpdateInternalComment_WritesCurrentCommentAndHistory()
    {
        Assert.Contains("internal_comment = @InternalComment", AdminRequestSql.UpdateInternalComment);
        Assert.Contains("comment_added", AdminRequestSql.InsertInternalCommentHistory);
        Assert.Contains("actor_user_id", AdminRequestSql.InsertInternalCommentHistory);
    }
}
```

Create `DapperAdminRequestRepositoryMappingTests.cs`:

```csharp
using System.Reflection;
using LineCom.Api.Modules.Requests.Repositories;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class DapperAdminRequestRepositoryMappingTests
{
    [Theory]
    [InlineData("AdminRequestListRow")]
    [InlineData("AdminRequestDetailRow")]
    [InlineData("AdminRequestHistoryRow")]
    public void DapperRowTypes_UseDateTimeForPostgresTimestamptz(string nestedTypeName)
    {
        var nestedType = typeof(DapperAdminRequestRepository).GetNestedType(nestedTypeName, BindingFlags.NonPublic);
        Assert.NotNull(nestedType);

        var createdAtProperty = nestedType.GetProperty("CreatedAt");
        Assert.NotNull(createdAtProperty);
        Assert.Equal(typeof(DateTime), createdAtProperty.PropertyType);
    }
}
```

- [ ] **Step 2: Run failing SQL tests**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter "AdminRequestSqlTests|DapperAdminRequestRepositoryMappingTests"
```

Expected: FAIL because SQL constants and nested row types do not exist.

- [ ] **Step 3: Implement SQL constants**

Create `AdminRequestSql.cs`:

```csharp
namespace LineCom.Api.Modules.Requests.Repositories;

internal static class AdminRequestSql
{
    public const string CountRequests = """
        SELECT COUNT(*)::int
        FROM requests request
        WHERE (@Status IS NULL OR request.status = @Status)
            AND (@Number IS NULL OR request.number ILIKE '%' || @Number || '%')
            AND (
                @Contact IS NULL
                OR request.customer_name ILIKE '%' || @Contact || '%'
                OR request.customer_email::text ILIKE '%' || @Contact || '%'
                OR request.customer_phone::text ILIKE '%' || @Contact || '%'
            )
            AND (
                @Organization IS NULL
                OR request.organization_name ILIKE '%' || @Organization || '%'
                OR request.organization_inn ILIKE '%' || @Organization || '%'
            );
        """;

    public const string FindRequests = """
        SELECT
            request.number AS "Number",
            request.status AS "Status",
            request.source AS "Source",
            COUNT(item.id)::int AS "ItemsCount",
            request.customer_name AS "CustomerName",
            request.customer_email AS "CustomerEmail",
            request.customer_phone AS "CustomerPhone",
            request.organization_name AS "OrganizationName",
            request.organization_inn AS "OrganizationInn",
            request.organization_contact_person AS "OrganizationContactPerson",
            request.customer_comment AS "CustomerComment",
            request.internal_comment AS "InternalComment",
            request.created_at AS "CreatedAt",
            request.updated_at AS "UpdatedAt"
        FROM requests request
        LEFT JOIN request_items item ON item.request_id = request.id
        WHERE (@Status IS NULL OR request.status = @Status)
            AND (@Number IS NULL OR request.number ILIKE '%' || @Number || '%')
            AND (
                @Contact IS NULL
                OR request.customer_name ILIKE '%' || @Contact || '%'
                OR request.customer_email::text ILIKE '%' || @Contact || '%'
                OR request.customer_phone::text ILIKE '%' || @Contact || '%'
            )
            AND (
                @Organization IS NULL
                OR request.organization_name ILIKE '%' || @Organization || '%'
                OR request.organization_inn ILIKE '%' || @Organization || '%'
            )
        GROUP BY request.id
        ORDER BY request.created_at DESC, request.number DESC
        LIMIT @PageSize
        OFFSET @Offset;
        """;

    public const string FindRequestDetail = """
        SELECT
            request.id AS "Id",
            request.number AS "Number",
            request.status AS "Status",
            request.source AS "Source",
            request.customer_name AS "CustomerName",
            request.customer_email AS "CustomerEmail",
            request.customer_phone AS "CustomerPhone",
            request.organization_name AS "OrganizationName",
            request.organization_inn AS "OrganizationInn",
            request.organization_contact_person AS "OrganizationContactPerson",
            request.customer_comment AS "CustomerComment",
            request.internal_comment AS "InternalComment",
            request.created_at AS "CreatedAt",
            request.updated_at AS "UpdatedAt"
        FROM requests request
        WHERE request.number = @Number
        LIMIT 1;
        """;

    public const string FindRequestForUpdate = """
        SELECT id AS "Id", status AS "Status", internal_comment AS "InternalComment"
        FROM requests
        WHERE number = @Number
        FOR UPDATE;
        """;

    public const string UpdateStatus = """
        UPDATE requests
        SET status = @Status
        WHERE id = @RequestId;
        """;

    public const string InsertStatusChangedHistory = """
        INSERT INTO request_history (request_id, event_type, actor_user_id, old_status, new_status)
        VALUES (@RequestId, 'status_changed', @ActorUserId, @OldStatus, @NewStatus);
        """;

    public const string UpdateInternalComment = """
        UPDATE requests
        SET internal_comment = @InternalComment
        WHERE id = @RequestId;
        """;

    public const string InsertInternalCommentHistory = """
        INSERT INTO request_history (request_id, event_type, actor_user_id, comment)
        VALUES (@RequestId, 'comment_added', @ActorUserId, @Comment);
        """;

    public const string FindRequestItems = CustomerRequestSql.FindRequestItems;

    public const string FindRequestHistory = """
        SELECT
            history.event_type AS "Event",
            CASE history.event_type
                WHEN 'created' THEN 'Заявка создана.'
                WHEN 'status_changed' THEN 'Статус заявки изменен.'
                WHEN 'comment_added' THEN 'Внутренний комментарий изменен.'
                ELSE history.event_type
            END AS "Message",
            history.created_at AS "CreatedAt"
        FROM request_history history
        WHERE history.request_id = @RequestId
        ORDER BY history.created_at, history.id;
        """;
}
```

- [ ] **Step 4: Implement Dapper repository**

Implement `DapperAdminRequestRepository` using the same `ToUtcDateTimeOffset(DateTime value)` pattern as `DapperCustomerRequestRepository`.

Core mutation algorithm for status:

```csharp
await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
var current = await connection.QuerySingleOrDefaultAsync<AdminRequestForUpdateRow>(
    new CommandDefinition(AdminRequestSql.FindRequestForUpdate, new { update.Number }, transaction, cancellationToken: cancellationToken));
if (current is null) return null;
if (current.Status != update.Status)
{
    await connection.ExecuteAsync(new CommandDefinition(AdminRequestSql.UpdateStatus, new { RequestId = current.Id, update.Status }, transaction, cancellationToken: cancellationToken));
    await connection.ExecuteAsync(new CommandDefinition(AdminRequestSql.InsertStatusChangedHistory, new { RequestId = current.Id, update.ActorUserId, OldStatus = current.Status, NewStatus = update.Status }, transaction, cancellationToken: cancellationToken));
}
await transaction.CommitAsync(cancellationToken);
return await GetRequestAsync(update.Number, cancellationToken);
```

Core mutation algorithm for internal comment:

```csharp
var normalizedComment = update.InternalComment;
if (!string.Equals(current.InternalComment, normalizedComment, StringComparison.Ordinal))
{
    await connection.ExecuteAsync(new CommandDefinition(AdminRequestSql.UpdateInternalComment, new { RequestId = current.Id, InternalComment = normalizedComment }, transaction, cancellationToken: cancellationToken));
    await connection.ExecuteAsync(new CommandDefinition(AdminRequestSql.InsertInternalCommentHistory, new { RequestId = current.Id, update.ActorUserId, Comment = "Внутренний комментарий изменен." }, transaction, cancellationToken: cancellationToken));
}
```

Nested row types must include:

```csharp
private sealed record AdminRequestListRow(
    string Number,
    string Status,
    string Source,
    int ItemsCount,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string? OrganizationName,
    string? OrganizationInn,
    string? OrganizationContactPerson,
    string? CustomerComment,
    string? InternalComment,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

- [ ] **Step 5: Run tests and commit**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter "AdminRequestSqlTests|DapperAdminRequestRepositoryMappingTests"
```

Expected: PASS.

Commit:

```powershell
git add apps/api/Modules/Requests/Repositories/AdminRequestSql.cs apps/api/Modules/Requests/Repositories/DapperAdminRequestRepository.cs tests/LineCom.Api.Tests/Modules/Requests/AdminRequestSqlTests.cs tests/LineCom.Api.Tests/Modules/Requests/DapperAdminRequestRepositoryMappingTests.cs
git commit -m "feat: add admin request repository"
```

---

### Task 5: Admin requests controller and endpoint authorization tests

**Files:**
- Create: `apps/api/Modules/Requests/Controllers/AdminRequestsController.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestsEndpointTests.cs`

- [ ] **Step 1: Write failing endpoint tests**

Create `AdminRequestsEndpointTests.cs` mirroring customer endpoint style. Include:

```csharp
[Fact]
public async Task GetRequests_WithoutAuth_ReturnsUnauthorizedError()
{
    await using var factory = CreateFactory(new ReturningAdminRequestService(), TestUser("seller"));
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    using var response = await client.GetAsync("/api/admin/requests");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    var body = await ReadJsonAsync<ApiErrorResponse>(response);
    Assert.Equal("auth.unauthorized", body.Code);
}

[Fact]
public async Task GetRequests_AsCustomer_ReturnsForbiddenError()
{
    await using var factory = CreateFactory(new ReturningAdminRequestService(), TestUser("customer"));
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    await LoginAsync(client);

    using var response = await client.GetAsync("/api/admin/requests");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task GetRequests_AsSeller_ReturnsAdminRequests()
{
    await using var factory = CreateFactory(new ReturningAdminRequestService(), TestUser("seller"));
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    await LoginAsync(client);

    using var response = await client.GetAsync("/api/admin/requests?page=2&pageSize=10&status=new&number=ЗК26&contact=ivan&organization=Сеть");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var body = await ReadJsonAsync<AdminRequestListResponse>(response);
    Assert.Equal(2, body.Page);
    Assert.Equal("ЗК26-0008", Assert.Single(body.Items).Number);
}
```

Also include tests for:

- `GET /api/admin/requests/ЗК26-0008`;
- `PATCH /api/admin/requests/ЗК26-0008/status` with CSRF;
- `PATCH` without CSRF returns `auth.forbidden`;
- `PUT /api/admin/requests/ЗК26-0008/internal-comment` with CSRF.

- [ ] **Step 2: Run failing endpoint tests**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter AdminRequestsEndpointTests
```

Expected: FAIL because controller does not exist.

- [ ] **Step 3: Implement controller**

Create `AdminRequestsController.cs`:

```csharp
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Requests.DTOs;
using LineCom.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Requests.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/requests")]
public sealed class AdminRequestsController : ControllerBase
{
    private readonly IAdminRequestService _requestService;

    public AdminRequestsController(IAdminRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminRequestListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminRequestListResponse>> GetRequests([FromQuery] AdminRequestListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _requestService.GetRequestsAsync(HttpContext, query, cancellationToken));
    }

    [HttpGet("{number}")]
    [ProducesResponseType(typeof(AdminRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminRequestDetailDto>> GetRequest(string number, CancellationToken cancellationToken)
    {
        return Ok(await _requestService.GetRequestAsync(HttpContext, number, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPatch("{number}/status")]
    [ProducesResponseType(typeof(AdminRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminRequestDetailDto>> UpdateStatus(string number, UpdateAdminRequestStatusCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _requestService.UpdateStatusAsync(HttpContext, number, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{number}/internal-comment")]
    [ProducesResponseType(typeof(AdminRequestDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminRequestDetailDto>> UpdateInternalComment(string number, UpdateAdminRequestInternalCommentCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _requestService.UpdateInternalCommentAsync(HttpContext, number, command, cancellationToken));
    }
}
```

- [ ] **Step 4: Run endpoint tests and commit**

Run:

```powershell
dotnet test LineCom.sln -m:1 --filter AdminRequestsEndpointTests
```

Expected: PASS.

Commit:

```powershell
git add apps/api/Modules/Requests/Controllers/AdminRequestsController.cs tests/LineCom.Api.Tests/Modules/Requests/AdminRequestsEndpointTests.cs
git commit -m "feat: expose admin request endpoints"
```

---

### Task 6: Frontend admin API client and routes

**Files:**
- Modify: `apps/front/src/lib/routes.ts`
- Create: `apps/front/src/lib/api/admin-requests.ts`
- Create: `apps/front/src/lib/api/admin-requests.test.ts`

- [ ] **Step 1: Write failing frontend API tests**

Create `admin-requests.test.ts`:

```ts
import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  getAdminRequests,
  getAdminRequest,
  updateAdminRequestStatus,
  updateAdminRequestInternalComment,
} from "./admin-requests";

describe("admin request api", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("builds filtered list query", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await getAdminRequests({ status: "new", number: "ЗК26", contact: "ivan", organization: "Сеть" });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/admin/requests?status=new&number=%D0%97%D0%9A26&contact=ivan&organization=%D0%A1%D0%B5%D1%82%D1%8C",
      expect.objectContaining({ method: "GET", credentials: "include" }),
    );
  });

  it("sends status update with csrf", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify({ number: "ЗК26-0008" }), { status: 200 }),
    );

    await updateAdminRequestStatus("ЗК26-0008", "in_progress", "csrf-token");

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/admin/requests/%D0%97%D0%9A26-0008/status",
      expect.objectContaining({
        method: "PATCH",
        body: JSON.stringify({ status: "in_progress" }),
      }),
    );
  });
});
```

- [ ] **Step 2: Run failing frontend API test**

Run:

```powershell
npm.cmd test -- src/lib/api/admin-requests.test.ts
```

Expected: FAIL because file does not exist.

- [ ] **Step 3: Implement routes and admin API client**

Update `routes.ts`:

```ts
adminRequests: () => "/admin/requests",
adminRequest: (number: string) => `/admin/requests/${encodeURIComponent(number)}`,
```

Create `admin-requests.ts`:

```ts
import type {
  CustomerRequestHistory,
  CustomerRequestItem,
  RequestCustomerSnapshot,
  RequestOrganizationSnapshot,
  RequestStatus,
} from "./requests";
import { apiJson } from "./http";

export type AdminRequestListItem = {
  number: string;
  status: RequestStatus;
  source: string;
  itemsCount: number;
  customer: RequestCustomerSnapshot;
  organization: RequestOrganizationSnapshot | null;
  customerComment: string | null;
  internalComment: string | null;
  createdAt: string;
  updatedAt: string;
};

export type AdminRequestListResponse = {
  items: AdminRequestListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type AdminRequestDetail = AdminRequestListItem & {
  items: CustomerRequestItem[];
  history: CustomerRequestHistory[];
};

export type AdminRequestListParams = {
  page?: number;
  pageSize?: number;
  status?: string;
  number?: string;
  contact?: string;
  organization?: string;
};

export function getAdminRequests(params: AdminRequestListParams = {}) {
  const search = new URLSearchParams();
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));
  if (params.status) search.set("status", params.status);
  if (params.number) search.set("number", params.number);
  if (params.contact) search.set("contact", params.contact);
  if (params.organization) search.set("organization", params.organization);
  const suffix = search.toString();
  return apiJson<AdminRequestListResponse>(`/api/admin/requests${suffix ? `?${suffix}` : ""}`, { cache: "no-store" });
}

export function getAdminRequest(number: string) {
  return apiJson<AdminRequestDetail>(`/api/admin/requests/${encodeURIComponent(number)}`, { cache: "no-store" });
}

export function updateAdminRequestStatus(number: string, status: string, csrfToken: string) {
  return apiJson<AdminRequestDetail>(`/api/admin/requests/${encodeURIComponent(number)}/status`, {
    method: "PATCH",
    body: { status },
    csrfToken,
  });
}

export function updateAdminRequestInternalComment(number: string, internalComment: string, csrfToken: string) {
  return apiJson<AdminRequestDetail>(`/api/admin/requests/${encodeURIComponent(number)}/internal-comment`, {
    method: "PUT",
    body: { internalComment },
    csrfToken,
  });
}
```

Extend `JsonRequestOptions.method` in `http.ts` to include `"PATCH"`:

```ts
method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
```

- [ ] **Step 4: Run test and commit**

Run:

```powershell
npm.cmd test -- src/lib/api/admin-requests.test.ts
```

Expected: PASS.

Commit:

```powershell
git add apps/front/src/lib/routes.ts apps/front/src/lib/api/http.ts apps/front/src/lib/api/admin-requests.ts apps/front/src/lib/api/admin-requests.test.ts
git commit -m "feat: add admin request api client"
```

---

### Task 7: Frontend admin request list page

**Files:**
- Create: `apps/front/src/components/admin/admin-request-list.tsx`
- Create: `apps/front/src/components/admin/admin-request-list.test.tsx`
- Create: `apps/front/src/app/admin/requests/page.tsx`
- Create: `apps/front/src/app/admin/requests/requests-page-client.tsx`
- Create: `apps/front/src/app/admin/requests/requests-page-client.test.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Write failing component test**

Create `admin-request-list.test.tsx`:

```tsx
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AdminRequestList } from "./admin-request-list";
import type { AdminRequestListItem } from "@/lib/api/admin-requests";

const requests: AdminRequestListItem[] = [
  {
    number: "ЗК26-0008",
    status: { code: "new", label: "Новая" },
    source: "cart",
    itemsCount: 3,
    customer: { name: "Иван Петров", email: "ivan@example.com", phone: "+79000000000" },
    organization: { name: "ООО Сеть", inn: "7700000000", contactPerson: "Иван Петров" },
    customerComment: "Нужна консультация.",
    internalComment: "Позвонить после 15:00.",
    createdAt: "2026-05-10T12:40:00Z",
    updatedAt: "2026-05-10T13:10:00Z",
  },
];

describe("AdminRequestList", () => {
  it("renders request queue with filters and detail link", () => {
    render(<AdminRequestList requests={requests} filters={{ status: "all", number: "", contact: "", organization: "" }} onFiltersChange={vi.fn()} />);

    expect(screen.getByRole("heading", { name: "Заявки" })).toBeInTheDocument();
    expect(screen.getByText("ЗК26-0008")).toBeInTheDocument();
    expect(screen.getByText("ООО Сеть")).toBeInTheDocument();
    expect(screen.getByText("Позвонить после 15:00.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Открыть заявку ЗК26-0008" })).toHaveAttribute("href", "/admin/requests/%D0%97%D0%9A26-0008");
  });

  it("emits filter changes", async () => {
    const onFiltersChange = vi.fn();
    render(<AdminRequestList requests={requests} filters={{ status: "all", number: "", contact: "", organization: "" }} onFiltersChange={onFiltersChange} />);

    await userEvent.selectOptions(screen.getByLabelText("Статус"), "new");

    expect(onFiltersChange).toHaveBeenCalledWith({ status: "new", number: "", contact: "", organization: "" });
  });
});
```

- [ ] **Step 2: Implement `AdminRequestList`**

Create `admin-request-list.tsx` with:

```tsx
"use client";

import Link from "next/link";
import type { AdminRequestListItem } from "@/lib/api/admin-requests";
import { formatDateTime, formatItemsCount, formatSource } from "@/lib/format";
import { routes } from "@/lib/routes";

export type AdminRequestFilters = {
  status: string;
  number: string;
  contact: string;
  organization: string;
};

type AdminRequestListProps = {
  requests: AdminRequestListItem[];
  filters: AdminRequestFilters;
  onFiltersChange: (filters: AdminRequestFilters) => void;
};

const statusOptions = [
  { value: "all", label: "Все" },
  { value: "new", label: "Новые" },
  { value: "in_progress", label: "В работе" },
  { value: "completed", label: "Завершены" },
  { value: "cancelled", label: "Отменены" },
];

export function AdminRequestList({ requests, filters, onFiltersChange }: AdminRequestListProps) {
  const setFilter = (name: keyof AdminRequestFilters, value: string) => {
    onFiltersChange({ ...filters, [name]: value });
  };

  return (
    <section className="admin-requests" aria-labelledby="admin-requests-title">
      <div className="admin-requests__header">
        <div>
          <p className="eyebrow">Админка</p>
          <h1 id="admin-requests-title">Заявки</h1>
        </div>
      </div>

      <div className="admin-requests__filters">
        <label>
          Статус
          <select value={filters.status} onChange={(event) => setFilter("status", event.target.value)}>
            {statusOptions.map((option) => (
              <option key={option.value} value={option.value}>{option.label}</option>
            ))}
          </select>
        </label>
        <label>Номер<input value={filters.number} onChange={(event) => setFilter("number", event.target.value)} /></label>
        <label>Контакт<input value={filters.contact} onChange={(event) => setFilter("contact", event.target.value)} /></label>
        <label>Организация<input value={filters.organization} onChange={(event) => setFilter("organization", event.target.value)} /></label>
      </div>

      {requests.length === 0 ? <p className="empty-state">Заявки не найдены.</p> : null}
      <div className="admin-request-list">
        {requests.map((request) => (
          <article className="admin-request-card" key={request.number}>
            <div>
              <p className="eyebrow">{formatDateTime(request.createdAt)}</p>
              <h2>{request.number}</h2>
              <span className="status-pill">{request.status.label}</span>
            </div>
            <dl className="summary-grid">
              <div><dt>Клиент</dt><dd>{request.customer.name}</dd></div>
              <div><dt>Контакт</dt><dd>{request.customer.email ?? request.customer.phone ?? "Не указан"}</dd></div>
              <div><dt>Организация</dt><dd>{request.organization?.name ?? "Не указана"}</dd></div>
              <div><dt>Позиции</dt><dd>{formatItemsCount(request.itemsCount)}</dd></div>
              <div><dt>Источник</dt><dd>{formatSource(request.source)}</dd></div>
            </dl>
            {request.customerComment ? <p>{request.customerComment}</p> : null}
            {request.internalComment ? <p className="admin-request-card__internal">{request.internalComment}</p> : null}
            <Link className="button button--ghost" href={routes.adminRequest(request.number)}>Открыть заявку {request.number}</Link>
          </article>
        ))}
      </div>
    </section>
  );
}
```

- [ ] **Step 3: Write page-client test and implementation**

Mock `getMe`, `getAdminRequests`, and router in `requests-page-client.test.tsx`. Assert:

- seller loads list;
- customer sees forbidden message;
- unauthorized redirects to `routes.login(routes.adminRequests())`.

Implement `requests-page-client.tsx` with `useEffect` loading `getMe`, checking `session.user.role`, then calling `getAdminRequests`.

- [ ] **Step 4: Run frontend tests and commit**

Run:

```powershell
npm.cmd test -- src/components/admin/admin-request-list.test.tsx src/app/admin/requests/requests-page-client.test.tsx
```

Expected: PASS.

Commit:

```powershell
git add apps/front/src/components/admin/admin-request-list.tsx apps/front/src/components/admin/admin-request-list.test.tsx apps/front/src/app/admin/requests/page.tsx apps/front/src/app/admin/requests/requests-page-client.tsx apps/front/src/app/admin/requests/requests-page-client.test.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin request list page"
```

---

### Task 8: Frontend admin request detail and mutations

**Files:**
- Create: `apps/front/src/components/admin/admin-request-detail.tsx`
- Create: `apps/front/src/components/admin/admin-request-detail.test.tsx`
- Create: `apps/front/src/app/admin/requests/[number]/page.tsx`
- Create: `apps/front/src/app/admin/requests/[number]/request-detail-page-client.tsx`
- Create: `apps/front/src/app/admin/requests/[number]/request-detail-page-client.test.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Write failing detail component test**

Create `admin-request-detail.test.tsx`:

```tsx
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AdminRequestDetail } from "./admin-request-detail";
import type { AdminRequestDetail as AdminRequestDetailDto } from "@/lib/api/admin-requests";

const request: AdminRequestDetailDto = {
  number: "ЗК26-0008",
  status: { code: "new", label: "Новая" },
  source: "cart",
  itemsCount: 1,
  customer: { name: "Иван Петров", email: "ivan@example.com", phone: "+79000000000" },
  organization: { name: "ООО Сеть", inn: "7700000000", contactPerson: "Иван Петров" },
  customerComment: "Нужна консультация.",
  internalComment: "Позвонить после 15:00.",
  createdAt: "2026-05-10T12:40:00Z",
  updatedAt: "2026-05-10T13:10:00Z",
  items: [{ productId: "p1", productName: "Кабель U/UTP Cat 5e", productSku: "LC-1", saleUnit: { code: "coil", label: "бухта" }, unitQuantity: "305 м", quantity: 2, customerComment: null }],
  history: [{ event: "created", message: "Заявка создана.", createdAt: "2026-05-10T12:40:00Z" }],
};

describe("AdminRequestDetail", () => {
  it("renders snapshots, items, status control, internal comment, and history", () => {
    render(<AdminRequestDetail request={request} onStatusSave={vi.fn()} onInternalCommentSave={vi.fn()} isSaving={false} />);

    expect(screen.getByRole("heading", { name: "Заявка ЗК26-0008" })).toBeInTheDocument();
    expect(screen.getByText("Иван Петров")).toBeInTheDocument();
    expect(screen.getByText("ООО Сеть")).toBeInTheDocument();
    expect(screen.getByText("Кабель U/UTP Cat 5e")).toBeInTheDocument();
    expect(screen.getByLabelText("Статус")).toHaveValue("new");
    expect(screen.getByLabelText("Внутренний комментарий")).toHaveValue("Позвонить после 15:00.");
    expect(screen.queryByText(/Цена/i)).not.toBeInTheDocument();
  });

  it("saves status and internal comment separately", async () => {
    const onStatusSave = vi.fn();
    const onInternalCommentSave = vi.fn();
    render(<AdminRequestDetail request={request} onStatusSave={onStatusSave} onInternalCommentSave={onInternalCommentSave} isSaving={false} />);

    await userEvent.selectOptions(screen.getByLabelText("Статус"), "in_progress");
    await userEvent.click(screen.getByRole("button", { name: "Сохранить статус" }));
    expect(onStatusSave).toHaveBeenCalledWith("in_progress");

    await userEvent.clear(screen.getByLabelText("Внутренний комментарий"));
    await userEvent.type(screen.getByLabelText("Внутренний комментарий"), "Уточнить замену.");
    await userEvent.click(screen.getByRole("button", { name: "Сохранить комментарий" }));
    expect(onInternalCommentSave).toHaveBeenCalledWith("Уточнить замену.");
  });
});
```

- [ ] **Step 2: Implement detail component**

Create `admin-request-detail.tsx` with desktop two-column layout and stacked mobile-friendly classes. Include four status options and no `quoted`.

- [ ] **Step 3: Write page-client test and implementation**

In `request-detail-page-client.test.tsx`, mock:

- `getMe`;
- `getAdminRequest`;
- `updateAdminRequestStatus`;
- `updateAdminRequestInternalComment`;
- `useRouter`.

Assert:

- seller loads detail;
- customer sees forbidden state;
- unauthorized redirects to login;
- status save sends `csrfToken`;
- internal comment save sends `csrfToken`.

Implement page client with local `request`, `isLoading`, `isSaving`, `pageError`, `actionMessage`.

- [ ] **Step 4: Run frontend tests and commit**

Run:

```powershell
npm.cmd test -- src/components/admin/admin-request-detail.test.tsx src/app/admin/requests/[number]/request-detail-page-client.test.tsx
```

Expected: PASS.

Commit:

```powershell
git add apps/front/src/components/admin/admin-request-detail.tsx apps/front/src/components/admin/admin-request-detail.test.tsx apps/front/src/app/admin/requests/[number]/page.tsx apps/front/src/app/admin/requests/[number]/request-detail-page-client.tsx apps/front/src/app/admin/requests/[number]/request-detail-page-client.test.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin request detail page"
```

---

### Task 9: Documentation, full verification, and browser QA

**Files:**
- Create: `vault/Человекочитаемое/Admin Request Processing API.md`
- Create: `vault/Человекочитаемое/Admin Request Processing iterations.md`
- Modify: `vault/Человекочитаемое/Продуктовая модель.md`
- Modify: `vault/Человекочитаемое/Auth Request Core API.md`

- [ ] **Step 1: Write docs**

Create `Admin Request Processing API.md` with:

- implemented endpoints;
- DTO summary;
- four statuses only;
- role rules;
- no prices/payments/invoices/shipping;
- customer endpoints do not expose `internalComment`.

Create `Admin Request Processing iterations.md` with the completed implementation summary and verification commands.

Update `Продуктовая модель.md` request statuses to remove any `quoted` equivalent.

Update `Auth Request Core API.md` status list to remove `quoted` and align cancellation spelling with `cancelled`.

- [ ] **Step 2: Run full backend and frontend checks**

Run:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1
```

Expected: build succeeds; all .NET tests pass.

Run from `apps/front`:

```powershell
npm.cmd run lint
npm.cmd test
npm.cmd run build
```

Expected: lint, Vitest, and production build pass.

- [ ] **Step 3: Run scope search**

Run:

```powershell
$markers = @("TO" + "DO", "TB" + "D", "FIX" + "ME", "заг" + "луш", "кос" + "тыл")
$commerce = @("quoted", "Купить", "В корзину", "Розничная цена", "Мелкий опт", "оплат", "счет", "счёт", "отгруз")
rg -n (($commerce + $markers) -join "|") apps/api apps/front/src tests docs/superpowers vault/Человекочитаемое
```

Expected:

- no `quoted` in implementation or release docs except historical design/plan lines that explicitly describe its removal;
- no forbidden commerce/payment/order wording in frontend/admin UI or admin API DTOs;
- no unresolved task markers in implementation files.

- [ ] **Step 4: Browser QA**

Start backend and frontend preview/dev servers according to current project workflow.

Verify in browser:

- `/admin/requests` as unauthenticated user redirects to `/auth/login?returnTo=%2Fadmin%2Frequests`;
- `/admin/requests` as customer shows controlled forbidden state;
- `/admin/requests` as seller/admin renders list;
- filters do not blank the page;
- `/admin/requests/ЗК26-0008` renders detail;
- status update works and remains on detail;
- internal comment update works and remains on detail;
- mobile width around `390px` has no horizontal overflow;
- no visible text `Купить`, `В корзину`, public price, payment, invoice, shipment, or order wording appears in admin pages.

- [ ] **Step 5: Commit documentation and final fixes**

Commit:

```powershell
git add vault/Человекочитаемое/Admin\ Request\ Processing\ API.md vault/Человекочитаемое/Admin\ Request\ Processing\ iterations.md vault/Человекочитаемое/Продуктовая\ модель.md vault/Человекочитаемое/Auth\ Request\ Core\ API.md
git commit -m "docs: document admin request processing"
```

If verification required code fixes, commit them with specific messages before this docs commit.

---

## Self-Review

Spec coverage:

- Shared seller/admin queue: Task 5 backend endpoints, Task 7 frontend list.
- Detail page with snapshots, items, history: Task 3 service mapping, Task 4 repository, Task 8 frontend detail.
- Status change: Task 3 service, Task 4 repository transaction/history, Task 5 endpoint, Task 8 frontend mutation.
- One current internal comment: Task 3 service, Task 4 repository, Task 8 frontend textarea.
- `quoted` removal: Task 1 migration/reference data/docs checks.
- Role rules: Task 2 service role guard, Task 5 endpoint tests, Task 7/8 frontend guards.
- No prices/payment/order scope: Task 9 scope search and component assertions.

Type consistency:

- Backend DTO names use `AdminRequest*Dto`, `AdminRequestListQuery`, `UpdateAdminRequestStatusCommand`, `UpdateAdminRequestInternalCommentCommand`.
- Repository records use `AdminRequest*Record` and update commands use `AdminRequestStatusUpdate`, `AdminRequestInternalCommentUpdate`.
- Frontend API types use `AdminRequestListItem`, `AdminRequestListResponse`, `AdminRequestDetail`.

Plan risk:

- Existing `request_history.actor_user_id` and `requests.internal_comment` already exist in `004_requests.sql`, so the implementation should avoid adding duplicate columns.
- If current test fixtures contain mojibake strings, implementation should not broaden the encoding issue; use correct UTF-8 strings in new tests and docs.
