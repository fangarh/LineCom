# Account Profile And Organization Iteration 5 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `/api/account/profile` and `/api/account/organization` for the authenticated active customer.

**Architecture:** Add a focused `Account` module that reads `userId` only from the existing cookie-auth context. Controllers delegate to account services; services normalize and validate input; Dapper repositories own parameterized SQL for `users` and `organizations`.

**Tech Stack:** .NET 8, ASP.NET Core controllers and cookie authentication, Dapper, Npgsql, xUnit, WebApplicationFactory.

---

### Task 1: Account Service RED Tests

**Files:**
- Create: `tests/LineCom.Api.Tests/Modules/Account/AccountProfileServiceTests.cs`

- [x] **Step 1: Write failing service tests**

Cover current profile reading with optional organization, profile update contact normalization, duplicate contact mapping to `auth.user_already_exists`, organization create/update normalization, and invalid organization name mapping to `validation.invalid_request`.

- [x] **Step 2: Run targeted tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter AccountProfileServiceTests
```

Expected: fail because account DTOs, service, and repository contracts do not exist yet.

### Task 2: Account Endpoint RED Tests

**Files:**
- Create: `tests/LineCom.Api.Tests/Modules/Account/AccountProfileEndpointTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Account/AccountModuleRegistrationTests.cs`
- Modify: `apps/api/Program.cs`

- [x] **Step 1: Write failing endpoint and DI tests**

Cover `GET /api/account/profile`, `PUT /api/account/profile`, `PUT /api/account/organization`, unauthenticated `401 auth.unauthorized`, inactive user `403 auth.user_inactive`, and scoped DI registration.

- [x] **Step 2: Run targeted tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "AccountProfileServiceTests|AccountProfileEndpointTests|AccountModuleRegistrationTests"
```

Expected: fail because account controller and module registration are not implemented.

### Task 3: Implement Account Module

**Files:**
- Create: `apps/api/Modules/Account/DTOs/AccountDtos.cs`
- Create: `apps/api/Modules/Account/Repositories/IAccountProfileRepository.cs`
- Create: `apps/api/Modules/Account/Repositories/DapperAccountProfileRepository.cs`
- Create: `apps/api/Modules/Account/Services/IAccountProfileService.cs`
- Create: `apps/api/Modules/Account/Services/AccountProfileService.cs`
- Create: `apps/api/Modules/Account/AccountServiceCollectionExtensions.cs`
- Create: `apps/api/Modules/Account/Controllers/AccountProfileController.cs`
- Modify: `apps/api/Program.cs`

- [x] **Step 1: Add DTOs and contracts**

Define profile, organization, profile update, organization update DTOs and repository records matching `Auth Request Core API.md`.

- [x] **Step 2: Add service behavior**

Normalize user contacts with auth normalization rules, validate required profile and organization fields, map duplicate user contacts to `auth.user_already_exists`, and require active current user.

- [x] **Step 3: Add Dapper repository**

Read profile by current user id; update only the current user row; upsert exactly one organization through `INSERT ... ON CONFLICT (user_id) DO UPDATE`; keep SQL parameterized.

- [x] **Step 4: Add controller and registration**

Add `[Authorize]` controller actions under `/api/account`, register account services in DI, and call `AddAccountModule()` from `Program.cs`.

- [x] **Step 5: Run targeted tests and verify GREEN**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "AccountProfileServiceTests|AccountProfileEndpointTests|AccountModuleRegistrationTests"
```

Expected: pass.

### Task 4: Verify and Close Iteration

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

Mark iteration 5 complete and set the recommended next continuation point to iteration 6.

- [x] **Step 4: Check debt markers**

Search changed project files for `TODO`, `TBD`, `заглуш`, `костыл` and resolve accidental markers before finishing.
