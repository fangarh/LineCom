# Admin Catalog Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the database and backend foundation required by the approved admin catalog and homepage design.

**Architecture:** This plan adds the durable catalog foundation first: database migration, public visibility rules, request creation visibility rules, and repository-level query services for homepage sections and duplicate candidates. It does not build the full admin UI or full CRUD controllers; those are separate follow-up plans after this foundation is verified.

**Tech Stack:** ASP.NET Core Web API, PostgreSQL, Npgsql, Dapper, DbUp SQL migrations, xUnit.

---

## Scope Split

The approved design covers several large subsystems. Implement them as separate plans:

1. `admin-catalog-foundation` - this plan: migration, public visibility, request visibility, homepage read model, duplicate candidates.
2. `admin-catalog-crud` - products, categories, attributes, brands admin API and validation.
3. `admin-catalog-images` - Local FileStorage upload endpoints for product images and brand logos.
4. `admin-homepage-ui` - frontend admin screens for homepage card sections.
5. `admin-product-ui` - frontend product/category/brand editors and publication checklist.

This first plan is complete when backend tests confirm:

- `products.is_active` exists and affects public visibility;
- `publish_status` only allows `draft` and `published`;
- `homepage_sections` and `homepage_section_items` exist with constraints;
- request creation cannot use inactive products;
- duplicate candidates can be queried with `pg_trgm`.

## File Structure

Create:

- `apps/dbmigrator/Migrations/007_admin_catalog_foundation.sql` - schema changes for `is_active`, homepage sections, trigram duplicate support.
- `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationMigrationTests.cs` - SQL text tests for migration content.
- `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationDatabaseBehaviorTests.cs` - opt-in PostgreSQL tests for constraints and visibility-critical schema behavior.
- `apps/api/Modules/Catalog/DTOs/AdminHomepageDtos.cs` - DTOs for admin homepage section read model.
- `apps/api/Modules/Catalog/DTOs/AdminDuplicateCandidateDtos.cs` - DTOs for duplicate candidate results.
- `apps/api/Modules/Catalog/Queries/IAdminHomepageQuery.cs` - read interface for admin homepage sections.
- `apps/api/Modules/Catalog/Queries/DapperAdminHomepageQuery.cs` - Dapper query for homepage sections and selected items.
- `apps/api/Modules/Catalog/Queries/AdminHomepageRows.cs` - internal rows for homepage query.
- `apps/api/Modules/Catalog/Queries/AdminHomepageSql.cs` - SQL text for homepage query.
- `apps/api/Modules/Catalog/Queries/IAdminProductDuplicateQuery.cs` - duplicate candidate query interface.
- `apps/api/Modules/Catalog/Queries/DapperAdminProductDuplicateQuery.cs` - Dapper query implementation using trigram similarity.
- `apps/api/Modules/Catalog/Queries/AdminProductDuplicateSql.cs` - SQL text for duplicate candidates.
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageQueryTests.cs` - read-model builder/query tests for homepage rows.
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminProductDuplicateSqlTests.cs` - SQL contract tests for duplicate candidate query.

Modify:

- `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs` - register new query services.
- `apps/api/Modules/Catalog/Queries/PublicProductSql.cs` - add `product.is_active = TRUE` to all public product read paths.
- `apps/api/Modules/Requests/Repositories/CustomerRequestSql.cs` - add `product.is_active = TRUE` to product snapshot lookup for new requests.
- `tests/LineCom.Api.Tests/Modules/Catalog/PublicProductSqlTests.cs` - assert public SQL filters active products.
- `tests/LineCom.Api.Tests/Modules/Requests/CustomerRequestSqlTests.cs` - assert request creation filters active products.
- `vault/Человекочитаемое/...` or a new iteration note only if the implementation materially changes the product record after the plan is executed.

Do not modify:

- frontend admin pages;
- import/export tooling;
- image upload endpoints;
- product/category CRUD endpoints.

### Task 1: Migration SQL Text Tests

**Files:**
- Create: `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationMigrationTests.cs`
- Read: `apps/dbmigrator/Migrations/007_admin_catalog_foundation.sql`

- [ ] **Step 1: Write failing migration text tests**

Create `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationMigrationTests.cs`:

```csharp
namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class AdminCatalogFoundationMigrationTests
{
    private static readonly string MigrationSql = ReadMigration("007_admin_catalog_foundation.sql");

    [Fact]
    public void AdminCatalogFoundation_EnablesPgTrgmForDuplicateSearch()
    {
        Assert.Contains("CREATE EXTENSION IF NOT EXISTS pg_trgm;", MigrationSql);
    }

    [Fact]
    public void AdminCatalogFoundation_AddsProductActiveFlag()
    {
        Assert.Contains(
            "ALTER TABLE products ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;",
            MigrationSql);
    }

    [Fact]
    public void AdminCatalogFoundation_RemovesArchivedPublishStatus()
    {
        Assert.Contains("ALTER TABLE products DROP CONSTRAINT IF EXISTS ck_products_publish_status;", MigrationSql);
        Assert.Contains(
            "ADD CONSTRAINT ck_products_publish_status CHECK (publish_status IN ('draft', 'published'))",
            MigrationSql);
        Assert.DoesNotContain("'archived'", MigrationSql);
    }

    [Theory]
    [InlineData("CREATE TABLE homepage_sections (")]
    [InlineData("CREATE TABLE homepage_section_items (")]
    [InlineData("CONSTRAINT ck_homepage_sections_type CHECK (type IN ('product_list', 'category_list'))")]
    [InlineData("CONSTRAINT ck_homepage_sections_item_limit_positive CHECK (item_limit > 0)")]
    [InlineData("CONSTRAINT ck_homepage_section_items_single_target CHECK (num_nonnulls(product_id, category_id) = 1)")]
    [InlineData("CREATE UNIQUE INDEX ux_homepage_sections_code ON homepage_sections (code);")]
    [InlineData("CREATE UNIQUE INDEX ux_homepage_section_items_section_product")]
    [InlineData("CREATE UNIQUE INDEX ux_homepage_section_items_section_category")]
    public void AdminCatalogFoundation_DefinesHomepageSectionSchema(string expectedSql)
    {
        Assert.Contains(expectedSql, MigrationSql);
    }

    [Theory]
    [InlineData("('hero_products', 'Hero: ходовые позиции', 'product_list', 3, 10, TRUE)")]
    [InlineData("('featured_products', 'Популярные позиции', 'product_list', 8, 20, TRUE)")]
    [InlineData("('direction_categories', 'Направления', 'category_list', 4, 30, TRUE)")]
    public void AdminCatalogFoundation_SeedsKnownHomepageSections(string expectedSql)
    {
        Assert.Contains(expectedSql, MigrationSql);
    }

    [Theory]
    [InlineData("CREATE INDEX IF NOT EXISTS ix_products_name_trgm ON products USING gin (name gin_trgm_ops);")]
    [InlineData("CREATE INDEX IF NOT EXISTS ix_products_slug_trgm ON products USING gin (slug gin_trgm_ops);")]
    public void AdminCatalogFoundation_AddsTrigramIndexesForDuplicateCandidates(string expectedSql)
    {
        Assert.Contains(expectedSql, MigrationSql);
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

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogFoundationMigrationTests
```

Expected: FAIL because `007_admin_catalog_foundation.sql` does not exist.

- [ ] **Step 3: Commit failing tests**

```powershell
git add tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationMigrationTests.cs
git commit -m "test: cover admin catalog foundation migration"
```

### Task 2: Admin Catalog Foundation Migration

**Files:**
- Create: `apps/dbmigrator/Migrations/007_admin_catalog_foundation.sql`
- Test: `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationMigrationTests.cs`

- [ ] **Step 1: Create migration SQL**

Create `apps/dbmigrator/Migrations/007_admin_catalog_foundation.sql`:

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;

ALTER TABLE products ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;

UPDATE products
SET publish_status = 'draft'
WHERE publish_status NOT IN ('draft', 'published');

ALTER TABLE products DROP CONSTRAINT IF EXISTS ck_products_publish_status;
ALTER TABLE products
    ADD CONSTRAINT ck_products_publish_status CHECK (publish_status IN ('draft', 'published'));

DROP INDEX IF EXISTS ix_products_public_listing;
CREATE INDEX IF NOT EXISTS ix_products_public_listing
    ON products (is_active, publish_status, availability_status, primary_category_id, sort_order);

CREATE INDEX IF NOT EXISTS ix_products_active_category
    ON products (is_active, primary_category_id, sort_order);

CREATE INDEX IF NOT EXISTS ix_products_name_trgm ON products USING gin (name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_products_slug_trgm ON products USING gin (slug gin_trgm_ops);

CREATE TABLE homepage_sections (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code text NOT NULL,
    title text NOT NULL,
    type text NOT NULL,
    item_limit integer NOT NULL,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_homepage_sections_code_not_blank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_homepage_sections_title_not_blank CHECK (btrim(title) <> ''),
    CONSTRAINT ck_homepage_sections_type CHECK (type IN ('product_list', 'category_list')),
    CONSTRAINT ck_homepage_sections_item_limit_positive CHECK (item_limit > 0)
);

CREATE UNIQUE INDEX ux_homepage_sections_code ON homepage_sections (code);
CREATE INDEX ix_homepage_sections_active_sort_order ON homepage_sections (is_active, sort_order, code);

CREATE TABLE homepage_section_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    section_id uuid NOT NULL REFERENCES homepage_sections (id) ON DELETE CASCADE,
    product_id uuid NULL REFERENCES products (id) ON DELETE RESTRICT,
    category_id uuid NULL REFERENCES categories (id) ON DELETE RESTRICT,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_homepage_section_items_single_target CHECK (num_nonnulls(product_id, category_id) = 1)
);

CREATE UNIQUE INDEX ux_homepage_section_items_section_product
    ON homepage_section_items (section_id, product_id)
    WHERE product_id IS NOT NULL;

CREATE UNIQUE INDEX ux_homepage_section_items_section_category
    ON homepage_section_items (section_id, category_id)
    WHERE category_id IS NOT NULL;

CREATE INDEX ix_homepage_section_items_section_sort_order
    ON homepage_section_items (section_id, is_active, sort_order, id);

CREATE OR REPLACE FUNCTION validate_homepage_section_item_target()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    section_type text;
BEGIN
    SELECT type INTO section_type
    FROM homepage_sections
    WHERE id = NEW.section_id;

    IF section_type = 'product_list' AND NEW.product_id IS NULL THEN
        RAISE EXCEPTION 'Homepage section % requires product item.', NEW.section_id;
    END IF;

    IF section_type = 'category_list' AND NEW.category_id IS NULL THEN
        RAISE EXCEPTION 'Homepage section % requires category item.', NEW.section_id;
    END IF;

    IF section_type = 'product_list' AND NEW.category_id IS NOT NULL THEN
        RAISE EXCEPTION 'Homepage product section % cannot use category item.', NEW.section_id;
    END IF;

    IF section_type = 'category_list' AND NEW.product_id IS NOT NULL THEN
        RAISE EXCEPTION 'Homepage category section % cannot use product item.', NEW.section_id;
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_homepage_section_items_validate_target
BEFORE INSERT OR UPDATE OF section_id, product_id, category_id ON homepage_section_items
FOR EACH ROW EXECUTE FUNCTION validate_homepage_section_item_target();

CREATE TRIGGER trg_homepage_sections_set_updated_at
BEFORE UPDATE ON homepage_sections
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_homepage_section_items_set_updated_at
BEFORE UPDATE ON homepage_section_items
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

INSERT INTO homepage_sections (code, title, type, item_limit, sort_order, is_active)
VALUES
    ('hero_products', 'Hero: ходовые позиции', 'product_list', 3, 10, TRUE),
    ('featured_products', 'Популярные позиции', 'product_list', 8, 20, TRUE),
    ('direction_categories', 'Направления', 'category_list', 4, 30, TRUE)
ON CONFLICT (code) DO UPDATE
SET
    title = EXCLUDED.title,
    type = EXCLUDED.type,
    item_limit = EXCLUDED.item_limit,
    sort_order = EXCLUDED.sort_order,
    is_active = EXCLUDED.is_active;
```

- [ ] **Step 2: Run migration text tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogFoundationMigrationTests
```

Expected: PASS.

- [ ] **Step 3: Commit migration**

```powershell
git add apps/dbmigrator/Migrations/007_admin_catalog_foundation.sql
git commit -m "feat: add admin catalog foundation migration"
```

### Task 3: PostgreSQL Migration Behavior Tests

**Files:**
- Create: `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationDatabaseBehaviorTests.cs`
- Read: `tests/LineCom.Api.Tests/Infrastructure/Database/PostgresMigrationFixture.cs`

- [ ] **Step 1: Write opt-in PostgreSQL behavior tests**

Create `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationDatabaseBehaviorTests.cs`:

```csharp
using Dapper;
using Npgsql;

namespace LineCom.Api.Tests.Infrastructure.Database;

[Collection(PostgresMigrationCollection.Name)]
public sealed class AdminCatalogFoundationDatabaseBehaviorTests
{
    private readonly PostgresMigrationFixture _fixture;

    public AdminCatalogFoundationDatabaseBehaviorTests(PostgresMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Migration_AddsProductActiveFlagAndHomepageSections()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL integration tests require LINECOM_TEST_CONNECTION_STRING.");

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);

        var productIsActiveCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM information_schema.columns
            WHERE table_name = 'products'
                AND column_name = 'is_active'
                AND data_type = 'boolean';
            """);

        var sectionCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM homepage_sections;");

        Assert.Equal(1, productIsActiveCount);
        Assert.Equal(3, sectionCount);
    }

    [SkippableFact]
    public async Task HomepageSectionItem_RejectsCategoryInsideProductSection()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL integration tests require LINECOM_TEST_CONNECTION_STRING.");

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        var sectionId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT id FROM homepage_sections WHERE code = 'hero_products';");

        var categoryId = await connection.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO categories (name, slug)
            VALUES ('Тестовая категория', 'test-category')
            RETURNING id;
            """);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO homepage_section_items (section_id, category_id)
            VALUES (@SectionId, @CategoryId);
            """,
            new { SectionId = sectionId, CategoryId = categoryId }));

        Assert.Contains("cannot use category item", exception.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Products_PublishStatusRejectsArchived()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL integration tests require LINECOM_TEST_CONNECTION_STRING.");

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        var categoryId = await connection.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO categories (name, slug)
            VALUES ('Статусная категория', 'status-category')
            RETURNING id;
            """);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO products (
                primary_category_id,
                name,
                slug,
                availability_status,
                sale_unit,
                unit_quantity,
                publish_status
            )
            VALUES (
                @CategoryId,
                'Архивный товар',
                'archived-product',
                'in_stock',
                'piece',
                '1 шт',
                'archived'
            );
            """,
            new { CategoryId = categoryId }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }
}
```

- [ ] **Step 2: Add xUnit skip support if missing**

Check whether `SkippableFact` is already available. If compilation fails because `SkippableFact` is undefined, replace each `[SkippableFact]` with `[Fact]` and replace each `Skip.IfNot(...)` with:

```csharp
if (!_fixture.IsConfigured)
{
    return;
}
```

Use the same replacement in all three test methods.

- [ ] **Step 3: Run behavior tests**

Run without PostgreSQL:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogFoundationDatabaseBehaviorTests
```

Expected without `LINECOM_TEST_CONNECTION_STRING`: PASS or SKIP, depending on skip implementation.

Run with PostgreSQL when a test database is available:

```powershell
$env:LINECOM_TEST_CONNECTION_STRING="Host=localhost;Port=5432;Database=linecom_test;Username=postgres;Password=postgres"
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogFoundationDatabaseBehaviorTests
```

Expected with configured database: PASS.

- [ ] **Step 4: Commit behavior tests**

```powershell
git add tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationDatabaseBehaviorTests.cs
git commit -m "test: cover admin catalog foundation database behavior"
```

### Task 4: Public Product Visibility Uses is_active

**Files:**
- Modify: `apps/api/Modules/Catalog/Queries/PublicProductSql.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/PublicProductSqlTests.cs`

- [ ] **Step 1: Write failing SQL assertions**

Update `tests/LineCom.Api.Tests/Modules/Catalog/PublicProductSqlTests.cs`:

```csharp
[Fact]
public void BuildProductListSql_SelectsOnlyActivePublishedProductsAndActiveCategories()
{
    var sql = PublicProductSql.BuildProductListSql(string.Empty, "ORDER BY product.sort_order, product.name, product.slug");

    Assert.Contains("product.is_active = TRUE", sql);
    Assert.Contains("product.publish_status = 'published'", sql);
    Assert.Contains("category.is_active = TRUE", sql);
    Assert.Contains("brand.is_active = TRUE", sql);
}

[Fact]
public void GetProductDetail_SelectsOnlyActivePublishedProductFromActiveCategory()
{
    Assert.Contains("product.slug = @Slug", PublicProductSql.GetProductDetail);
    Assert.Contains("product.is_active = TRUE", PublicProductSql.GetProductDetail);
    Assert.Contains("product.publish_status = 'published'", PublicProductSql.GetProductDetail);
    Assert.Contains("category.is_active = TRUE", PublicProductSql.GetProductDetail);
}
```

Remove the old versions named:

- `BuildProductListSql_SelectsOnlyPublishedProductsAndActiveCategories`;
- `GetProductDetail_SelectsOnlyPublishedProductFromActiveCategory`.

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter PublicProductSqlTests
```

Expected: FAIL because public SQL does not yet contain `product.is_active = TRUE`.

- [ ] **Step 3: Update public product SQL**

In `apps/api/Modules/Catalog/Queries/PublicProductSql.cs`, add `AND product.is_active = TRUE` anywhere public products are selected.

The relevant fragments should become:

```csharp
WHERE product.is_active = TRUE
    AND product.publish_status = 'published'
    {whereSql}
```

and:

```sql
WHERE product.slug = @Slug
    AND product.is_active = TRUE
    AND product.publish_status = 'published';
```

Also update product image, attribute, and breadcrumb subqueries inside `GetProductDetail` so they include:

```sql
AND product.is_active = TRUE
AND product.publish_status = 'published'
```

- [ ] **Step 4: Run public product SQL tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter PublicProductSqlTests
```

Expected: PASS.

- [ ] **Step 5: Commit public visibility change**

```powershell
git add apps/api/Modules/Catalog/Queries/PublicProductSql.cs tests/LineCom.Api.Tests/Modules/Catalog/PublicProductSqlTests.cs
git commit -m "fix: filter inactive products from public catalog"
```

### Task 5: Request Creation Rejects Inactive Products

**Files:**
- Modify: `apps/api/Modules/Requests/Repositories/CustomerRequestSql.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Requests/CustomerRequestSqlTests.cs`

- [ ] **Step 1: Write failing request SQL assertion**

Open `tests/LineCom.Api.Tests/Modules/Requests/CustomerRequestSqlTests.cs` and add:

```csharp
[Fact]
public void FindProductSnapshots_SelectsOnlyActivePublishedProducts()
{
    Assert.Contains("product.id = ANY(@ProductIds)", CustomerRequestSql.FindProductSnapshots);
    Assert.Contains("product.is_active = TRUE", CustomerRequestSql.FindProductSnapshots);
    Assert.Contains("product.publish_status = 'published'", CustomerRequestSql.FindProductSnapshots);
    Assert.Contains("category.is_active = TRUE", CustomerRequestSql.FindProductSnapshots);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter CustomerRequestSqlTests
```

Expected: FAIL because the request product snapshot SQL does not yet filter `product.is_active`.

- [ ] **Step 3: Update request product snapshot SQL**

In `apps/api/Modules/Requests/Repositories/CustomerRequestSql.cs`, change the end of `FindProductSnapshots` to:

```sql
WHERE product.id = ANY(@ProductIds)
    AND product.is_active = TRUE
    AND product.publish_status = 'published';
```

- [ ] **Step 4: Run request SQL tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter CustomerRequestSqlTests
```

Expected: PASS.

- [ ] **Step 5: Commit request visibility change**

```powershell
git add apps/api/Modules/Requests/Repositories/CustomerRequestSql.cs tests/LineCom.Api.Tests/Modules/Requests/CustomerRequestSqlTests.cs
git commit -m "fix: reject inactive products in new requests"
```

### Task 6: Admin Homepage Read Model

**Files:**
- Create: `apps/api/Modules/Catalog/DTOs/AdminHomepageDtos.cs`
- Create: `apps/api/Modules/Catalog/Queries/AdminHomepageRows.cs`
- Create: `apps/api/Modules/Catalog/Queries/AdminHomepageSql.cs`
- Create: `apps/api/Modules/Catalog/Queries/IAdminHomepageQuery.cs`
- Create: `apps/api/Modules/Catalog/Queries/DapperAdminHomepageQuery.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageQueryTests.cs`

- [ ] **Step 1: Create DTOs**

Create `apps/api/Modules/Catalog/DTOs/AdminHomepageDtos.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminHomepageSectionsResponse(
    IReadOnlyList<AdminHomepageSectionDto> Sections);

public sealed record AdminHomepageSectionDto(
    Guid Id,
    string Code,
    string Title,
    string Type,
    int ItemLimit,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<AdminHomepageSectionItemDto> Items);

public sealed record AdminHomepageSectionItemDto(
    Guid Id,
    Guid? ProductId,
    Guid? CategoryId,
    string Name,
    string? Slug,
    string? SecondaryText,
    int SortOrder,
    bool IsActive,
    string VisibilityStatus);
```

- [ ] **Step 2: Create row records**

Create `apps/api/Modules/Catalog/Queries/AdminHomepageRows.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.Queries;

internal sealed record AdminHomepageSectionRow(
    Guid Id,
    string Code,
    string Title,
    string Type,
    int ItemLimit,
    int SortOrder,
    bool IsActive);

internal sealed record AdminHomepageSectionItemRow(
    Guid Id,
    Guid SectionId,
    Guid? ProductId,
    Guid? CategoryId,
    string? ProductName,
    string? ProductSlug,
    string? ProductSku,
    bool? ProductIsActive,
    string? ProductPublishStatus,
    string? ProductCategoryName,
    bool? ProductCategoryIsActive,
    string? CategoryName,
    string? CategorySlug,
    bool? CategoryIsActive,
    int SortOrder,
    bool IsActive);
```

- [ ] **Step 3: Create SQL**

Create `apps/api/Modules/Catalog/Queries/AdminHomepageSql.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.Queries;

internal static class AdminHomepageSql
{
    public const string GetSections = """
        SELECT
            id AS "Id",
            code AS "Code",
            title AS "Title",
            type AS "Type",
            item_limit AS "ItemLimit",
            sort_order AS "SortOrder",
            is_active AS "IsActive"
        FROM homepage_sections
        ORDER BY sort_order, code;
        """;

    public const string GetSectionItems = """
        SELECT
            item.id AS "Id",
            item.section_id AS "SectionId",
            item.product_id AS "ProductId",
            item.category_id AS "CategoryId",
            product.name AS "ProductName",
            product.slug AS "ProductSlug",
            product.sku AS "ProductSku",
            product.is_active AS "ProductIsActive",
            product.publish_status AS "ProductPublishStatus",
            product_category.name AS "ProductCategoryName",
            product_category.is_active AS "ProductCategoryIsActive",
            category.name AS "CategoryName",
            category.slug AS "CategorySlug",
            category.is_active AS "CategoryIsActive",
            item.sort_order AS "SortOrder",
            item.is_active AS "IsActive"
        FROM homepage_section_items item
        LEFT JOIN products product ON product.id = item.product_id
        LEFT JOIN categories product_category ON product_category.id = product.primary_category_id
        LEFT JOIN categories category ON category.id = item.category_id
        ORDER BY item.section_id, item.sort_order, item.id;
        """;
}
```

- [ ] **Step 4: Create query interface and implementation**

Create `apps/api/Modules/Catalog/Queries/IAdminHomepageQuery.cs`:

```csharp
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public interface IAdminHomepageQuery
{
    Task<AdminHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default);
}
```

Create `apps/api/Modules/Catalog/Queries/DapperAdminHomepageQuery.cs`:

```csharp
using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed class DapperAdminHomepageQuery : IAdminHomepageQuery
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminHomepageQuery(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminHomepageSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var sections = (await connection.QueryAsync<AdminHomepageSectionRow>(
            new CommandDefinition(AdminHomepageSql.GetSections, cancellationToken: cancellationToken))).ToArray();
        var items = (await connection.QueryAsync<AdminHomepageSectionItemRow>(
            new CommandDefinition(AdminHomepageSql.GetSectionItems, cancellationToken: cancellationToken))).ToArray();

        var itemsBySection = items.ToLookup(item => item.SectionId);
        return new AdminHomepageSectionsResponse(sections
            .Select(section => new AdminHomepageSectionDto(
                section.Id,
                section.Code,
                section.Title,
                section.Type,
                section.ItemLimit,
                section.SortOrder,
                section.IsActive,
                itemsBySection[section.Id].Select(BuildItem).ToArray()))
            .ToArray());
    }

    private static AdminHomepageSectionItemDto BuildItem(AdminHomepageSectionItemRow row)
    {
        if (row.ProductId is not null)
        {
            return new AdminHomepageSectionItemDto(
                row.Id,
                row.ProductId,
                null,
                row.ProductName ?? "Товар не найден",
                row.ProductSlug,
                row.ProductSku ?? row.ProductCategoryName,
                row.SortOrder,
                row.IsActive,
                ResolveProductVisibilityStatus(row));
        }

        return new AdminHomepageSectionItemDto(
            row.Id,
            null,
            row.CategoryId,
            row.CategoryName ?? "Категория не найдена",
            row.CategorySlug,
            null,
            row.SortOrder,
            row.IsActive,
            ResolveCategoryVisibilityStatus(row));
    }

    private static string ResolveProductVisibilityStatus(AdminHomepageSectionItemRow row)
    {
        if (!row.IsActive)
        {
            return "item_inactive";
        }

        if (row.ProductIsActive != true)
        {
            return "product_inactive";
        }

        if (!string.Equals(row.ProductPublishStatus, "published", StringComparison.Ordinal))
        {
            return "product_unpublished";
        }

        if (row.ProductCategoryIsActive != true)
        {
            return "category_inactive";
        }

        return "visible";
    }

    private static string ResolveCategoryVisibilityStatus(AdminHomepageSectionItemRow row)
    {
        if (!row.IsActive)
        {
            return "item_inactive";
        }

        return row.CategoryIsActive == true ? "visible" : "category_inactive";
    }
}
```

- [ ] **Step 5: Register query service**

Modify `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAdminHomepageQuery, DapperAdminHomepageQuery>();
```

Place it next to the existing catalog query registrations.

- [ ] **Step 6: Add focused unit tests for status mapping**

Create `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageQueryTests.cs`:

```csharp
using LineCom.Api.Modules.Catalog.Queries;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminHomepageQueryTests
{
    [Fact]
    public void AdminHomepageSql_LoadsSectionsAndItemsWithVisibilityInputs()
    {
        Assert.Contains("FROM homepage_sections", AdminHomepageSql.GetSections);
        Assert.Contains("FROM homepage_section_items item", AdminHomepageSql.GetSectionItems);
        Assert.Contains("product.is_active AS \"ProductIsActive\"", AdminHomepageSql.GetSectionItems);
        Assert.Contains("product.publish_status AS \"ProductPublishStatus\"", AdminHomepageSql.GetSectionItems);
        Assert.Contains("product_category.is_active AS \"ProductCategoryIsActive\"", AdminHomepageSql.GetSectionItems);
        Assert.Contains("category.is_active AS \"CategoryIsActive\"", AdminHomepageSql.GetSectionItems);
    }
}
```

If `AdminHomepageSql` is internal and tests cannot access it, add this to `apps/api/Properties/AssemblyInfo.cs` if not already present:

```csharp
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("LineCom.Api.Tests")]
```

- [ ] **Step 7: Run tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminHomepageQueryTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 8: Commit homepage query foundation**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminHomepageDtos.cs apps/api/Modules/Catalog/Queries/AdminHomepageRows.cs apps/api/Modules/Catalog/Queries/AdminHomepageSql.cs apps/api/Modules/Catalog/Queries/IAdminHomepageQuery.cs apps/api/Modules/Catalog/Queries/DapperAdminHomepageQuery.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageQueryTests.cs apps/api/Properties/AssemblyInfo.cs
git commit -m "feat: add admin homepage read model"
```

### Task 7: Duplicate Candidate Query Foundation

**Files:**
- Create: `apps/api/Modules/Catalog/DTOs/AdminDuplicateCandidateDtos.cs`
- Create: `apps/api/Modules/Catalog/Queries/AdminProductDuplicateSql.cs`
- Create: `apps/api/Modules/Catalog/Queries/IAdminProductDuplicateQuery.cs`
- Create: `apps/api/Modules/Catalog/Queries/DapperAdminProductDuplicateQuery.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminProductDuplicateSqlTests.cs`

- [ ] **Step 1: Create DTOs**

Create `apps/api/Modules/Catalog/DTOs/AdminDuplicateCandidateDtos.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminProductDuplicateCandidatesResponse(
    IReadOnlyList<AdminProductDuplicateCandidateDto> Items);

public sealed record AdminProductDuplicateCandidateDto(
    Guid Id,
    string Name,
    string Slug,
    string? Sku,
    string? ExternalId,
    string CategoryName,
    string CategorySlug,
    string? BrandName,
    string PublishStatus,
    bool IsActive,
    decimal Similarity);
```

- [ ] **Step 2: Create SQL**

Create `apps/api/Modules/Catalog/Queries/AdminProductDuplicateSql.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.Queries;

internal static class AdminProductDuplicateSql
{
    public const string FindCandidates = """
        SELECT
            product.id AS "Id",
            product.name AS "Name",
            product.slug AS "Slug",
            product.sku AS "Sku",
            product.external_id AS "ExternalId",
            category.name AS "CategoryName",
            category.slug AS "CategorySlug",
            brand.name AS "BrandName",
            product.publish_status AS "PublishStatus",
            product.is_active AS "IsActive",
            GREATEST(
                similarity(product.name, @Name),
                similarity(product.slug, @Slug)
            )::numeric AS "Similarity"
        FROM products product
        INNER JOIN categories category ON category.id = product.primary_category_id
        LEFT JOIN brands brand ON brand.id = product.brand_id
        WHERE (@ExcludeProductId IS NULL OR product.id <> @ExcludeProductId)
            AND (
                (@Sku IS NOT NULL AND product.sku = @Sku)
                OR (@ExternalId IS NOT NULL AND product.external_id = @ExternalId)
                OR (@Slug IS NOT NULL AND product.slug = @Slug)
                OR (
                    @Name IS NOT NULL
                    AND product.primary_category_id = @CategoryId
                    AND similarity(product.name, @Name) >= @SimilarityThreshold
                )
                OR (
                    @Slug IS NOT NULL
                    AND similarity(product.slug, @Slug) >= @SimilarityThreshold
                )
            )
        ORDER BY
            CASE
                WHEN @Sku IS NOT NULL AND product.sku = @Sku THEN 0
                WHEN @ExternalId IS NOT NULL AND product.external_id = @ExternalId THEN 1
                WHEN @Slug IS NOT NULL AND product.slug = @Slug THEN 2
                ELSE 3
            END,
            GREATEST(similarity(product.name, @Name), similarity(product.slug, @Slug)) DESC,
            product.name,
            product.slug
        LIMIT @Limit;
        """;
}
```

- [ ] **Step 3: Create query interface and implementation**

Create `apps/api/Modules/Catalog/Queries/IAdminProductDuplicateQuery.cs`:

```csharp
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed record AdminProductDuplicateCandidateQuery(
    string? Name,
    Guid? CategoryId,
    Guid? BrandId,
    string? Sku,
    string? ExternalId,
    string? Slug,
    Guid? ExcludeProductId,
    int Limit = 10,
    decimal SimilarityThreshold = 0.35m);

public interface IAdminProductDuplicateQuery
{
    Task<AdminProductDuplicateCandidatesResponse> FindCandidatesAsync(
        AdminProductDuplicateCandidateQuery query,
        CancellationToken cancellationToken = default);
}
```

Create `apps/api/Modules/Catalog/Queries/DapperAdminProductDuplicateQuery.cs`:

```csharp
using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Queries;

public sealed class DapperAdminProductDuplicateQuery : IAdminProductDuplicateQuery
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminProductDuplicateQuery(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AdminProductDuplicateCandidatesResponse> FindCandidatesAsync(
        AdminProductDuplicateCandidateQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AdminProductDuplicateCandidateDto>(
            new CommandDefinition(
                AdminProductDuplicateSql.FindCandidates,
                new
                {
                    query.Name,
                    query.CategoryId,
                    query.BrandId,
                    query.Sku,
                    query.ExternalId,
                    query.Slug,
                    query.ExcludeProductId,
                    Limit = Math.Clamp(query.Limit, 1, 25),
                    query.SimilarityThreshold
                },
                cancellationToken: cancellationToken));

        return new AdminProductDuplicateCandidatesResponse(rows.ToArray());
    }
}
```

- [ ] **Step 4: Register duplicate query**

Modify `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAdminProductDuplicateQuery, DapperAdminProductDuplicateQuery>();
```

- [ ] **Step 5: Add SQL contract tests**

Create `tests/LineCom.Api.Tests/Modules/Catalog/AdminProductDuplicateSqlTests.cs`:

```csharp
using LineCom.Api.Modules.Catalog.Queries;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminProductDuplicateSqlTests
{
    [Fact]
    public void FindCandidates_UsesHardIdentityMatchesAndTrigramSimilarity()
    {
        Assert.Contains("product.sku = @Sku", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("product.external_id = @ExternalId", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("product.slug = @Slug", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("similarity(product.name, @Name)", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("similarity(product.slug, @Slug)", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("product.primary_category_id = @CategoryId", AdminProductDuplicateSql.FindCandidates);
        Assert.Contains("LIMIT @Limit", AdminProductDuplicateSql.FindCandidates);
    }
}
```

- [ ] **Step 6: Run duplicate tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminProductDuplicateSqlTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 7: Commit duplicate query foundation**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminDuplicateCandidateDtos.cs apps/api/Modules/Catalog/Queries/AdminProductDuplicateSql.cs apps/api/Modules/Catalog/Queries/IAdminProductDuplicateQuery.cs apps/api/Modules/Catalog/Queries/DapperAdminProductDuplicateQuery.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminProductDuplicateSqlTests.cs
git commit -m "feat: add product duplicate candidate query"
```

### Task 8: Full Foundation Verification

**Files:**
- Verify all files from Tasks 1-7.
- Modify docs only if verification reveals a documented behavior mismatch.

- [ ] **Step 1: Run focused test suite**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogFoundationMigrationTests|AdminCatalogFoundationDatabaseBehaviorTests|PublicProductSqlTests|CustomerRequestSqlTests|AdminHomepageQueryTests|AdminProductDuplicateSqlTests|CatalogModuleRegistrationTests"
```

Expected: PASS. PostgreSQL behavior tests pass or skip when `LINECOM_TEST_CONNECTION_STRING` is not configured.

- [ ] **Step 2: Run full backend test suite**

Run:

```powershell
dotnet test .\LineCom.sln
```

Expected: PASS.

- [ ] **Step 3: Run build**

Run:

```powershell
dotnet build .\LineCom.sln
```

Expected: PASS with 0 errors.

- [ ] **Step 4: Inspect git diff**

Run:

```powershell
git diff --check
git status --short
```

Expected:

- `git diff --check` reports no whitespace errors;
- only intended files are modified.

- [ ] **Step 5: Commit final verification note if docs changed**

If documentation was updated during verification:

```powershell
git add docs/superpowers/specs/2026-05-11-admin-catalog-homepage-design.md vault/Человекочитаемое
git commit -m "docs: update admin catalog foundation notes"
```

If no docs changed, do not create an empty commit.

## Handoff Notes

After this plan is complete, write the next implementation plan for `admin-catalog-crud`. That plan should start with admin DTOs and endpoints for categories, brands, attributes, and products, using the schema and read-model foundation from this plan.
