# Start Prompt: Admin Full Catalog UX

Продолжаем LineCom в `D:\Projects\FL\LineCom`.

Нужно начать работу после очистки контекста по согласованному срезу `Admin Full Catalog UX` в мультиагентном режиме.

## Обязательный контекст

Прочитай перед планированием:

- `AGENTS.md`
- `vault/Человекочитаемое/README.md`
- `vault/Человекочитаемое/Сквозные требования.md`
- `vault/Человекочитаемое/Продуктовая модель.md`
- `vault/Человекочитаемое/Архитектура backend и БД.md`
- `vault/Человекочитаемое/Admin Homepage Management API.md`
- `vault/Человекочитаемое/Admin Request Processing API.md`
- `vault/Человекочитаемое/SEO GEO Public Catalog.md`
- `docs/superpowers/specs/2026-05-13-admin-full-catalog-ux-design.md`
- `docs/superpowers/specs/2026-05-13-account-navigation-request-quick-view-design.md`

Главная спецификация для этой работы:

- `docs/superpowers/specs/2026-05-13-admin-full-catalog-ux-design.md`

Предыдущий релевантный коммит:

- `92337b8 docs: design admin full catalog ux`

## Цель среза

Реализовать согласованный вариант `B`: полный рабочий каталог и UX администрирования.

В срез входят:

- выпадающее меню `Администрирование` для `seller` и `admin`;
- компактная таблица товаров с явной пагинацией и признаками активности, публикации, готовности и категории;
- дерево категорий в списке и при выборе родителя;
- добавление товаров/категорий на `/admin/homepage` через поиск, без ручного UUID;
- автоматическая генерация slug с ручным переопределением;
- смена пароля только текущего пользователя через защищённый endpoint.

В срез не входят:

- управление пользователями;
- админская смена пароля другого пользователя;
- восстановление пароля;
- одноразовые коды;
- audit log;
- массовые операции;
- Excel-импорт/экспорт;
- изменение публичных SEO/GEO URL, sitemap, robots, canonical или metadata;
- перенос товаров при перемещении категории.

## Обязательные проектные правила

- Все ответы пользователю на русском.
- `vault/Человекочитаемое` считать источником истины.
- Backend: PostgreSQL, Npgsql, Dapper. Entity Framework не использовать.
- Миграции: SQL-скрипты через DbUp.
- FileStorage: локальный.
- SEO/GEO учитывать при slug, категориях, товарах, metadata и публичных маршрутах.
- Не оставлять намеренный технический долг.
- Не трогать чужие untracked-файлы.

Текущие известные untracked-файлы, которые не относятся к задаче и их нельзя stage/commit без отдельного указания:

- `admin-catalog-homepage-slice.png`
- `dns-master-current.png`
- `old_cite.png`

## Как стартовать

1. Проверь состояние:

```powershell
git status --short --branch
git log -5 --oneline
```

2. Если нет свежего implementation plan для этой спецификации, используй skill `superpowers:writing-plans` и создай план в:

```text
docs/superpowers/plans/2026-05-13-admin-full-catalog-ux.md
```

План должен быть разбит на проверяемые задачи с коммитами после каждого законченного шага.

3. После утверждения/готовности плана переходи к `superpowers:subagent-driven-development`.

## Мультиагентное разбиение

Используй subagents/workers только для независимых задач с непересекающимися зонами записи. Каждому worker явно указывай, что он не один в кодовой базе и не должен откатывать чужие изменения.

Рекомендуемые независимые направления:

- `Navigation/Auth worker`: `SiteHeader`, `AuthProvider`, role-aware admin menu, связанные тесты.
- `Catalog Products worker`: компактная таблица товаров, пагинация, фильтры, status badges, тесты.
- `Category Tree worker`: helpers дерева, category tree UI, выбор родителя, тесты.
- `Homepage Search worker`: поиск товаров/категорий для секций главной, отказ от ручного UUID в UI, тесты.
- `Slug worker`: shared slug helper, автогенерация, ручное переопределение, frontend tests, backend validation review.
- `Password worker`: `PUT /api/account/password`, сервисы/репозитории, frontend форма, backend/frontend tests.

Не запускай workers на один и тот же файл одновременно. Если файл уже слишком крупный, сначала сделай узкую декомпозицию и только потом параллельные изменения.

## Важные файлы для первичного анализа

Frontend:

- `apps/front/src/components/layout/site-header.tsx`
- `apps/front/src/components/auth/auth-provider.tsx`
- `apps/front/src/lib/routes.ts`
- `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- `apps/front/src/components/admin/catalog/admin-product-list-panel.tsx`
- `apps/front/src/components/admin/catalog/admin-category-manager.tsx`
- `apps/front/src/components/admin/homepage/admin-homepage-manager.tsx`
- `apps/front/src/components/account/profile-form.tsx`
- `apps/front/src/app/account/profile/profile-page-client.tsx`
- `apps/front/src/lib/api/admin-catalog.ts`
- `apps/front/src/lib/api/admin-homepage.ts`
- `apps/front/src/lib/api/account.ts`
- `apps/front/src/app/globals.css`

Backend:

- `apps/api/Modules/Auth`
- `apps/api/Modules/Account`
- `apps/api/Modules/Catalog`
- `apps/api/Modules/Catalog/Services/AdminCatalogInput.cs`
- `apps/dbmigrator/Migrations`
- `tests/LineCom.Api.Tests/Modules/Auth`
- `tests/LineCom.Api.Tests/Modules/Account`
- `tests/LineCom.Api.Tests/Modules/Catalog`

## Ожидаемые проверки

Для каждого шага запускай focused tests по изменённой зоне.

Перед завершением всего среза:

```powershell
npm.cmd test
npm.cmd run build
dotnet test .\LineCom.sln
dotnet build .\LineCom.sln
git diff --check
```

Если sandbox/network мешает важной команде, запроси escalation по правилам окружения.

## Browser QA

После frontend-изменений обязательно открыть локальное приложение в браузере и проверить:

- desktop/mobile header с `Администрирование`;
- `/admin/catalog` товары: фильтры, пагинация, выбор товара;
- `/admin/catalog` категории: дерево, редактирование, выбор родителя;
- `/admin/homepage`: поиск и добавление товара/категории;
- `/account/profile`: смена пароля;
- отсутствие горизонтального overflow;
- отсутствие перекрытия текста в кнопках, таблицах и компактных строках.

## Первый шаг

Начни с чтения спецификации и создания implementation plan, если он ещё не существует:

```powershell
Get-Content -Raw docs\superpowers\specs\2026-05-13-admin-full-catalog-ux-design.md
Get-ChildItem docs\superpowers\plans
```

Затем предложи или сразу запиши план реализации по правилам `superpowers:writing-plans`, после чего переходи к мультиагентному выполнению.
