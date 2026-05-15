# 05-03 Summary: Admin Contract Drift Checks

## Result

Completed.

Phase 5 now has lightweight contract-drift coverage for critical admin catalog product and homepage API surfaces.

## Scope Completed

- Strengthened frontend admin catalog API-client tests for:
  - product list/detail fixture shape;
  - create/update product mutation paths;
  - product attributes mutation path;
  - cookie credentials and CSRF headers on unsafe methods.
- Strengthened frontend admin homepage API-client tests for:
  - section/item fixture shape;
  - section update, item add/update/delete, and order mutation paths;
  - cookie credentials and CSRF headers on unsafe methods.
- Added backend endpoint serialization assertions for:
  - admin product detail critical JSON fields;
  - admin product attribute value shape;
  - admin homepage section and item critical JSON fields.
- Preserved contract-related backend dirty baseline for:
  - clearing product attribute values when a product category changes;
  - returning an existing homepage section item for duplicate product/category targets.

## Files Changed

- `apps/front/src/lib/api/admin-catalog.test.ts`
- `apps/front/src/lib/api/admin-homepage.test.ts`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageEndpointTests.cs`
- `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs`
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`
- `apps/api/Modules/Catalog/Repositories/AdminHomepageRepositorySql.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductAttributeRepositoryDatabaseTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductSqlTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageRepositorySqlTests.cs`

## Verification

Passed:

```powershell
npm.cmd --prefix apps/front test -- src/lib/api/admin-catalog.test.ts src/lib/api/admin-homepage.test.ts
```

Result: 2 test files, 16 tests passed.

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~AdminCatalogProductsEndpointTests|FullyQualifiedName~AdminHomepageEndpointTests"
```

Result: 18 tests passed. The run emitted `NU1900` vulnerability-feed warnings because `https://api.nuget.org/v3/index.json` was not reachable, but restore used existing project assets and tests passed.

```powershell
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~AdminCatalogProductAttributeRepositoryDatabaseTests|FullyQualifiedName~AdminHomepageRepositorySqlTests|FullyQualifiedName~AdminCatalogProductSqlTests"
```

Result: 21 tests passed. The same `NU1900` feed warnings were present.

## Commits

- `47b2c68 test(05-03): add admin contract drift checks`

## Ownership Notes

- Executor-owned additions: frontend API-client contract fixture assertions and backend endpoint serialization assertions.
- Pre-existing user-owned backend dirty baseline included because it is directly contract-related and had focused regression coverage:
  - product category change clears old product attribute values;
  - duplicate homepage target insertion returns the existing item.
- Unrelated public pages/styles, public homepage resolver files, and `errors/` were not staged.

## Requirement Coverage

- `MAIN-03`: Covered.
- `MAIN-02`: Reinforced for backend behavior that affects admin contract stability.
