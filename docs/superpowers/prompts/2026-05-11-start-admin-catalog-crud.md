# Start Prompt: Admin Catalog CRUD

Продолжаем LineCom в `D:\Projects\FL\LineCom`.

Нужно выполнить следующий backend CRUD/API срез админки каталога по принципу Subagent-Driven.

Обязательные файлы:

- Спецификация: `docs/superpowers/specs/2026-05-11-admin-catalog-homepage-design.md`
- Foundation-план, уже выполнен: `docs/superpowers/plans/2026-05-11-admin-catalog-foundation.md`
- Новый план реализации: `docs/superpowers/plans/2026-05-11-admin-catalog-crud.md`

Текущий контекст:

- Foundation-срез завершён коммитами:
  - `f4946c1` test: cover admin catalog foundation migration
  - `88ced6b` feat: add admin catalog foundation migration
  - `152a0eb` test: cover admin catalog foundation database behavior
  - `5b5e10d` fix: filter inactive products from public catalog
  - `9e3d5b8` fix: reject inactive products in new requests
  - `a2eeb63` feat: add admin homepage read model
  - `22f0f4a` feat: add product duplicate candidate query
- Foundation verification прошёл:
  - focused suite: `79/79`
  - `dotnet test .\LineCom.sln`: `449/449`
  - `dotnet build .\LineCom.sln`: `0 errors`
  - `git diff --check`: clean
- `admin-catalog-homepage-slice.png` остаётся untracked и вне задачи. Не трогать, не stage, не commit.

Обязательные правила:

1. Используй skill `superpowers:subagent-driven-development`.
2. Выполняй задачи из `docs/superpowers/plans/2026-05-11-admin-catalog-crud.md` строго по порядку.
3. На каждую задачу запускай отдельного worker/subagent, если это уместно.
4. После каждой задачи:
   - проверь изменения;
   - запусти указанные тесты;
   - сделай spec compliance review;
   - сделай code quality review;
   - исправь все Critical/Important замечания;
   - сохрани коммит согласно плану.
5. Соблюдай AGENTS.md:
   - все ответы на русском;
   - Context7 для вопросов по библиотекам/SDK/API/CLI;
   - backend через PostgreSQL/Npgsql/Dapper;
   - миграции через DbUp;
   - Entity Framework не использовать;
   - Local FileStorage учитывать, но upload endpoints не делать в CRUD-плане;
   - SEO/GEO учитывать при категориях, товарах, slug, metadata.
6. Не реализуй:
   - image upload endpoints;
   - brand logo upload;
   - homepage mutation endpoints;
   - frontend UI;
   - import/export;
   - audit log;
   - LLM duplicate checking.

Начни с:

```powershell
git status --short --branch
Get-Content -Raw docs\superpowers\plans\2026-05-11-admin-catalog-crud.md
```

Затем приступай к Task 1 из плана.
