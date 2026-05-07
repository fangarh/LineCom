# Customer Request Reading Iteration 8 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement authenticated customer reading of their own request list and request detail by public request number.

**Architecture:** Extend the existing `Requests` module. Controllers keep only HTTP routing, `CustomerRequestService` reads the active authenticated user from cookie-auth context, and `ICustomerRequestRepository` owns all parameterized SQL over request snapshots.

**Tech Stack:** .NET 8, ASP.NET Core controllers and cookie authentication, Dapper, Npgsql, xUnit, WebApplicationFactory.

---

### Task 1: Service RED Tests

**Files:**
- Modify: `tests/LineCom.Api.Tests/Modules/Requests/CustomerRequestServiceTests.cs`

- [x] **Step 1: Write failing service tests**

Add tests for:
- `GetRequestsAsync` passes only current `userId`, normalized page, page size, and optional status to repository.
- `GetRequestsAsync` rejects unknown status with `request.invalid_status`.
- `GetRequestAsync` passes only current `userId` and trimmed public number.
- `GetRequestAsync` maps repository miss to `request.not_found`.

- [x] **Step 2: Run targeted service tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter CustomerRequestServiceTests
```

Expected: fail because read DTOs, service methods, and repository contracts are not implemented.

### Task 2: Endpoint RED Tests

**Files:**
- Modify: `tests/LineCom.Api.Tests/Modules/Requests/CustomerRequestsEndpointTests.cs`

- [x] **Step 1: Write failing endpoint tests**

Add tests for:
- `GET /api/account/requests?page=2&pageSize=10&status=new` returns the current user's request page.
- `GET /api/account/requests/{number}` returns the current user's request detail.
- unauthenticated list and detail return `401 auth.unauthorized`.
- repository/service `request.not_found` maps to `404 request.not_found`.

- [x] **Step 2: Run targeted endpoint tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter CustomerRequestsEndpointTests
```

Expected: fail because the GET routes and service members are not implemented.

### Task 3: Implement Read Contracts and Service

**Files:**
- Modify: `apps/api/Modules/Requests/DTOs/CustomerRequestDtos.cs`
- Modify: `apps/api/Modules/Requests/Repositories/ICustomerRequestRepository.cs`
- Modify: `apps/api/Modules/Requests/Services/ICustomerRequestService.cs`
- Modify: `apps/api/Modules/Requests/Services/CustomerRequestService.cs`

- [x] **Step 1: Add DTOs and repository records**

Add list query/response DTOs, list item DTO, detail DTO with customer/organization/history snapshots, repository query, repository list response, and repository detail records.

- [x] **Step 2: Add service behavior**

Normalize paging defaults (`page=1`, `pageSize=20`, max `60`), trim status and number, allow only known request statuses, map status labels through `IRequestReferenceData`, and map missing detail to `request.not_found`.

- [x] **Step 3: Run service tests and verify GREEN**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter CustomerRequestServiceTests
```

Expected: service tests pass.

### Task 4: Implement Repository SQL and Controller Routes

**Files:**
- Modify: `apps/api/Modules/Requests/Repositories/CustomerRequestSql.cs`
- Modify: `apps/api/Modules/Requests/Repositories/DapperCustomerRequestRepository.cs`
- Modify: `apps/api/Modules/Requests/Controllers/CustomerRequestsController.cs`

- [x] **Step 1: Add parameterized SQL**

Add SQL for current user's paged request list, total count, request detail by `user_id + number`, detail items, and history ordered by creation time.

- [x] **Step 2: Add Dapper read methods**

Implement list and detail methods without exposing internal ids in API DTOs. Detail query must return `null` when the request does not belong to the current user.

- [x] **Step 3: Add controller GET actions**

Add `GET /api/account/requests` and `GET /api/account/requests/{number}` using service methods and existing auth/error middleware.

- [x] **Step 4: Run endpoint tests and verify GREEN**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter CustomerRequestsEndpointTests
```

Expected: endpoint tests pass.

### Task 5: Verify and Close Iteration

**Files:**
- Modify: `vault/Человекочитаемое/Auth Request Core iterations.md`
- Modify: `C:\Users\Fangarh\.codex\memories\linecom.md` if the memory file exists and needs the continuation point updated.

- [x] **Step 1: Run solution build**

Run:

```powershell
dotnet build LineCom.sln
```

Expected: build succeeds.

- [x] **Step 2: Run solution tests**

Run:

```powershell
dotnet test LineCom.sln
```

Expected: tests pass.

- [x] **Step 3: Update Obsidian status**

Mark iteration 8 complete and set the recommended next continuation point to iteration 9.

- [x] **Step 4: Check debt markers**

Search changed project files for `TODO`, `TBD`, `заглуш`, `костыл` and resolve accidental markers before finishing.
