# SEO/GEO Public Catalog — handoff 2026-05-13

## Текущее состояние

- Рабочий каталог: `D:\Projects\FL\LineCom`.
- Основная ветка: `main`.
- `main` объединен с worktree-веткой `feat/seo-geo-public-catalog`.
- `origin/main` обновлен и запушен до commit `9028249 docs: document public SEO metadata`.
- Корневой `git status`: `main...origin/main`, без tracked changes.
- Единственный ожидаемый untracked файл: `admin-catalog-homepage-slice.png`.
- Worktree еще существует: `.worktrees/seo-geo-public-catalog`, ветка `feat/seo-geo-public-catalog`, тот же HEAD `9028249`.

## Что было реализовано

- Site origin helpers: `apps/front/src/lib/seo/site.ts`.
- Shared metadata helpers: `apps/front/src/lib/seo/metadata.ts`.
- Root `metadataBase` и canonical metadata для публичных страниц.
- Canonical metadata для:
  - `/`
  - `/catalog`
  - `/about`
  - `/delivery`
  - `/catalog/{categorySlug}`
  - `/products/{slug}`
- `noindex, nofollow` metadata для внутренних страниц:
  - `/auth/*`
  - `/account/*`
  - `/admin/*`
- `app/robots.ts` и тесты.
- `app/sitemap.ts`, sitemap builder и тесты.
- Route-level sitemap test покрывает pagination с backend-valid `pageSize: 60` и fallback при падении product API branch.
- Документ постоянного контракта: `vault/Человекочитаемое/SEO GEO Public Catalog.md`.

## Коммиты slice

- `f1db6dd feat: add public SEO helpers`
- `95130a4 test: restore SEO origin env cleanly`
- `19f6a10 test: cover SEO metadata base origin`
- `2159ee0 feat: add public canonical metadata`
- `654af9f fix: keep public page copy unchanged`
  - Пустой commit, оставлен в истории, потому что reset не был разрешен.
- `8397c89 feat: noindex internal frontend pages`
- `4fb397c feat: add public catalog sitemap`
- `14a5263 fix: use valid sitemap product page size`
- `4e69a2d feat: add public robots route`
- `9028249 docs: document public SEO metadata`

## Проверки

В worktree перед merge:

- `npm.cmd test -- seo metadata sitemap robots`: passed, 16 tests.
- `npm.cmd test`: passed, 196 tests.
- `npm.cmd run lint`: exit 0, только существующие warnings `@next/next/no-img-element`.
- `npm.cmd run build`: passed.
- `dotnet test` без env сначала падал из-за `Connection string 'Default' is not configured`.
- `dotnet test` с dummy env `ConnectionStrings__Default=Host=localhost;Port=5432;Database=linecom_test;Username=linecom;Password=linecom`: passed, 691 tests.
- `dotnet build` с тем же dummy env: passed, только NU1900 warnings из-за недоступного NuGet vulnerability feed.
- `git diff --check`: clean.
- Marker scan по changed files: clean.

В корневом `main` после merge перед push:

- `npm.cmd test -- seo metadata sitemap robots`: passed, 16 tests.
- `npm.cmd run build`: passed.
- `git diff --check`: clean.
- `git push origin main`: success, `6fad95c..9028249 main -> main`.

## Browser QA

QA server поднимался на `http://127.0.0.1:4182`.

Проверено:

- `/catalog`:
  - title `Каталог кабеля и компонентов LineCom`;
  - canonical `http://127.0.0.1:4182/catalog`;
  - robots `index, follow`.
- `/about`:
  - canonical `http://127.0.0.1:4182/about`;
  - robots `index, follow`;
  - console errors отсутствовали.
- `/auth/login`:
  - title `Вход в LineCom`;
  - robots `noindex, nofollow`.
- `/admin/catalog`:
  - title `Админка каталога LineCom`;
  - robots `noindex, nofollow`;
  - console error был только из-за недоступного QA API `/api/auth/me`, не из-за SEO metadata.
- `/robots.txt`:
  - `Disallow: /admin/`
  - `Disallow: /account/`
  - `Disallow: /auth/`
  - `Sitemap: http://127.0.0.1:4182/sitemap.xml`
- `/sitemap.xml` без API содержит fallback static entries:
  - `/`
  - `/catalog`
  - `/about`
  - `/delivery`
- `/catalog/test-category` и `/products/test-product` при недоступном API уходят в unavailable fallback с `noindex, nofollow`.

Не проверено в браузере:

- API-backed canonical для реальных `/catalog/{categorySlug}` и `/products/{slug}`, потому что локальный API/data во время QA не поднимались.
- В тестах это покрывается helper/route behavior и использованием API `canonicalPath`.

## Хвосты для следующей сессии

- Решить, нужно ли удалить worktree:
  - `.worktrees/seo-geo-public-catalog`
  - branch `feat/seo-geo-public-catalog`
- Если удалять:
  - сначала убедиться, что `main` чистый и `origin/main` на `9028249` или новее;
  - затем удалить worktree и локальную feature-ветку.
- Не трогать `admin-catalog-homepage-slice.png`, если пользователь отдельно не попросит.
- Если нужно финально перепроверить production SEO с данными, поднять API с seed/catalog data и проверить реальные category/product canonical через браузер.

## Resume prompt

Продолжаем LineCom в `D:\Projects\FL\LineCom`.

Правила:

- Все ответы пользователю на русском.
- Соблюдать AGENTS.md.
- `vault/Человекочитаемое` — source of truth.
- Backend: PostgreSQL + Npgsql + Dapper, без Entity Framework.
- Миграции только SQL через DbUp.
- Local FileStorage — целевой file-storage подход.
- Context7 использовать для вопросов по библиотекам/framework/API/CLI.
- Не трогать untracked `admin-catalog-homepage-slice.png`.

Текущее состояние:

- `main` fast-forward merged и pushed в `origin/main` до `9028249`.
- SEO/GEO public catalog slice завершен.
- Worktree `.worktrees/seo-geo-public-catalog` еще существует и содержит ту же финальную версию.
- Следующий практичный шаг: по желанию пользователя cleanup worktree/feature branch или проверка SEO на локальном API с данными.
