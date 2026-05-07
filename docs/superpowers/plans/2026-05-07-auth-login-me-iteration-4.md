# Auth Login And Me Iteration 4 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the minimal auth loop with `POST /api/auth/login` and `GET /api/auth/me`.

**Architecture:** Extend the existing `Auth` module without adding new persistence technology. Login validation and password verification live in a focused service, user lookup lives in the Dapper repository, cookie creation continues through `IAuthSessionService`, and `GET /api/auth/me` reads only the authenticated claims/user record path required by the public contract.

**Tech Stack:** .NET 8, ASP.NET Core controllers and cookie authentication, Dapper, Npgsql, xUnit, WebApplicationFactory.

---

### Task 1: Login Service RED Tests

**Files:**
- Modify: `tests/LineCom.Api.Tests/Modules/Auth/CustomerRegistrationServiceTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Auth/CustomerLoginServiceTests.cs`

- [x] **Step 1: Write failing service tests**

Cover successful login by normalized email and phone, invalid password mapping to `auth.invalid_credentials`, missing user mapping to `auth.invalid_credentials`, inactive user mapping to `auth.user_inactive`, and malformed login/password mapping to public validation errors.

- [x] **Step 2: Run targeted tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter CustomerLoginServiceTests
```

Expected: fail because login service/repository contracts do not exist yet.

### Task 2: Login And Me Endpoint RED Tests

**Files:**
- Modify: `tests/LineCom.Api.Tests/Modules/Auth/AuthRegisterEndpointTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Auth/AuthModuleRegistrationTests.cs`

- [x] **Step 1: Write failing endpoint and DI tests**

Cover `POST /api/auth/login` returning `200 OK`, camelCase `AuthSessionDto`, `linecom_auth` cookie, invalid credentials `401`, inactive user `403`, `GET /api/auth/me` returning the authenticated user, and unauthenticated `GET /api/auth/me` returning `auth.unauthorized`.

- [x] **Step 2: Run targeted tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "CustomerLoginServiceTests|AuthLoginEndpointTests|AuthModuleRegistrationTests"
```

Expected: fail because controller actions and service registrations are not implemented.

### Task 3: Implement Login And Current User

**Files:**
- Modify: `apps/api/Modules/Auth/DTOs/AuthDtos.cs`
- Modify: `apps/api/Modules/Auth/Services/AuthErrors.cs`
- Modify: `apps/api/Modules/Auth/Services/IPasswordHasher.cs`
- Modify: `apps/api/Modules/Auth/Services/Pbkdf2PasswordHasher.cs`
- Create: `apps/api/Modules/Auth/Services/ICustomerLoginService.cs`
- Create: `apps/api/Modules/Auth/Services/CustomerLoginService.cs`
- Create: `apps/api/Modules/Auth/Services/IAuthCurrentUserService.cs`
- Create: `apps/api/Modules/Auth/Services/AuthCurrentUserService.cs`
- Create: `apps/api/Modules/Auth/Repositories/IUserLoginRepository.cs`
- Create: `apps/api/Modules/Auth/Repositories/DapperUserLoginRepository.cs`
- Modify: `apps/api/Modules/Auth/Controllers/AuthController.cs`
- Modify: `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`

- [x] **Step 1: Add DTOs and contracts**

Add `LoginRequest`, `ICustomerLoginService`, and `IAuthCurrentUserService`.

- [x] **Step 2: Add password verification**

Extend `IPasswordHasher` with `VerifyPassword` and implement PBKDF2 verification against the existing `pbkdf2-sha256$iterations$salt$hash` format using constant-time comparison.

- [x] **Step 3: Add user lookup repository**

Lookup by normalized email or normalized phone through parameterized SQL. Return password hash and active status for the login service; do not expose password hash in API DTOs.

- [x] **Step 4: Add login service behavior**

Normalize login as email when it parses as email, otherwise normalize as phone. Use the same public `auth.invalid_credentials` for missing user and wrong password. Reject inactive users with `auth.user_inactive`.

- [x] **Step 5: Add current user service and controller actions**

`POST /api/auth/login` signs in through the existing cookie session service. `GET /api/auth/me` requires cookie auth, returns `AuthSessionDto`, and never accepts user id from request body or URL.

- [x] **Step 6: Register services and run targeted tests**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "CustomerLoginServiceTests|AuthLoginEndpointTests|AuthModuleRegistrationTests"
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

Mark iteration 4 complete and set the recommended next continuation point to iteration 5.

- [x] **Step 4: Check debt markers**

Search changed project files for `TODO`, `TBD`, `заглуш`, `костыл` and resolve accidental markers before finishing.
