# Auth Request Core Iteration 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the release database foundation for users and optional one-per-user organizations.

**Architecture:** The iteration is schema-only. A new DbUp SQL migration creates `users` and `organizations`; tests validate table creation, role/contact constraints, uniqueness, password storage shape, one-organization cardinality, and update timestamp triggers.

**Tech Stack:** .NET 8, xUnit, PostgreSQL SQL, DbUp, Npgsql/Dapper project conventions.

---

### Task 1: Add Auth/Organization Migration Tests

**Files:**
- Create: `tests/LineCom.Api.Tests/Infrastructure/Database/AuthRequestCoreMigrationTests.cs`

- [x] **Step 1: Write failing tests**

Create tests that read `apps/dbmigrator/Migrations/003_auth_users_organizations.sql` and assert:

- `users` and `organizations` tables exist;
- `users.role` is constrained to `customer`, `seller`, `admin`;
- at least one of `email` or `phone` is required;
- non-empty `email` and `phone` are unique;
- users store `password_hash`, not plaintext password;
- organizations are one-per-user through a unique `user_id` index;
- organization belongs to a user through a foreign key;
- both tables use `set_updated_at` triggers.

- [x] **Step 2: Run targeted tests and verify RED**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter AuthRequestCoreMigrationTests
```

Expected: fail because `003_auth_users_organizations.sql` does not exist yet.

### Task 2: Add SQL Migration

**Files:**
- Create: `apps/dbmigrator/Migrations/003_auth_users_organizations.sql`

- [x] **Step 1: Add minimal release schema**

Create `users` with contact, role, password hash, activity and timestamps. Create `organizations` with a required owner user, optional business/contact fields and timestamps.

- [x] **Step 2: Add constraints and indexes**

Add check constraints for non-blank required fields, at-least-one contact, allowed roles, and non-blank optional fields. Add partial unique indexes for non-empty `email` and `phone`, and unique `organizations.user_id`.

- [x] **Step 3: Add update timestamp triggers**

Use the existing `set_updated_at()` function from the catalog migration.

- [x] **Step 4: Run targeted tests and verify GREEN**

Run:

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter AuthRequestCoreMigrationTests
```

Expected: pass.

### Task 3: Verify and Close Iteration

**Files:**
- Modify: `vault/Человекочитаемое/Auth Request Core iterations.md`
- Modify: `C:\Users\Fangarh\.codex\memories\linecom.md`

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

Mark iteration 2 complete and set the recommended next continuation point to iteration 3.

- [x] **Step 4: Check debt markers**

Run a search for `TODO`, `TBD`, `заглуш`, `костыл` in changed project files and resolve any accidental markers.
