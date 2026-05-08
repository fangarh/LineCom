# Frontend Auth + Request Flow iterations

## Назначение

Этот план разбивает frontend-срез `Frontend Auth + Request Flow` на маленькие проверяемые итерации.

Цель этапа: получить первый рабочий пользовательский путь LineCom на frontend: публичный каталог ведет к карточке товара, товар добавляется в черновик заявки, пользователь проходит вход или регистрацию, отправляет заявку и видит ее в личном кабинете.

Основа этапа:

- дизайн: `docs/superpowers/specs/2026-05-07-frontend-auth-request-flow-design.md`;
- детальный implementation plan: `docs/superpowers/plans/2026-05-07-frontend-auth-request-flow.md`;
- public catalog API: `Public Catalog API.md`;
- auth/request API: `Auth Request Core API.md`.

## Общие правила этапа

- Frontend использует Next.js App Router, React и TypeScript.
- Публичные страницы каталога и товаров должны оставаться SEO/GEO-совместимыми: slug URL, server-rendered content, metadata.
- Browser API-вызовы идут через Next.js rewrite `/api/:path*` к backend, чтобы сохранить same-origin cookie-auth.
- Auth строится на существующей HTTP-only cookie backend-модели; frontend не читает cookie.
- Изменяющие защищенные запросы используют `X-CSRF-Token`.
- Черновик заявки хранится на frontend и восстанавливается после перезагрузки.
- Backend остается источником истины для проверки товара, номера заявки и snapshots.
- UI использует язык заявки: `Добавить в заявку`, `Отправить заявку`, `Цена по запросу`.
- В UI не должно быть публичных цен, онлайн-оплаты, счетов, доставки или текста `Оформить заказ`.
- Backend-контракты не меняются без отдельного решения.
- После каждой кодовой итерации выполняются релевантные frontend-проверки; после интеграционных итераций дополнительно выполняются .NET checks.
- До отдельного исправления окружения .NET checks запускаются с `-m:1`.
- Перед закрытием каждой итерации проверяется отсутствие намеренного технического долга.

## Итерация 1: Frontend tooling и API proxy

Статус: завершена.

Подготовить frontend к безопасной разработке и same-origin интеграции с backend.

Результат:

- Vitest + jsdom + React Testing Library;
- `npm test` и `npm run lint`;
- `LINECOM_API_ORIGIN`;
- Next.js rewrite `/api/:path* -> LINECOM_API_ORIGIN/api/:path*`;
- `.env.example` для frontend.

Критерии завершения:

- `npm.cmd run lint` проходит;
- `npm.cmd test` проходит;
- frontend может обращаться к backend через `/api/...`;
- CORS не требуется для браузерного frontend flow.

## Итерация 2: Typed API clients и обработка ошибок

Статус: завершена.

Добавить тонкий frontend API-слой без бизнес-логики в компонентах.

Результат:

- типы public catalog DTO;
- типы auth/account/request DTO;
- общий `apiJson`;
- `ApiErrorResponse` parsing;
- route builders;
- format helpers.

Критерии завершения:

- есть тесты для error parsing;
- API clients используют documented backend endpoints;
- account/auth/request reads используют `no-store`;
- backend errors не превращаются в сырые exception messages в UI.

## Итерация 3: Черновик заявки

Статус: выполнена 2026-05-07.

Реализовать клиентскую модель черновика заявки.

Результат:

- draft types;
- reducer;
- localStorage persistence;
- selectors;
- тесты reducer/storage.

Критерии завершения:

- товар добавляется в заявку одной единицей продажи;
- повторное добавление увеличивает количество, а не дублирует позицию;
- количество не падает ниже `1`;
- черновик восстанавливается после перезагрузки;
- frontend snapshot не считается источником истины для backend.

Изменена: добавлена клиентская модель черновика заявки с reducer, selectors, localStorage persistence и тестами reducer/storage.

## Итерация 4: App shell и базовая визуальная система

Статус: выполнена 2026-05-07.

Заменить стартовый Next.js-шаблон рабочим каркасом LineCom.

Результат:

- Russian locale в root layout;
- базовые metadata LineCom;
- `AuthProvider`;
- `RequestDraftProvider`;
- общий site header;
- общий page shell;
- restrained B2B catalog styling.

Критерии завершения:

- стартовый Next.js контент удален;
- навигация содержит каталог, заявку, личный кабинет и вход;
- layout не является маркетинговой landing page;
- UI не использует декоративные gradient/orb решения;
- `npm.cmd run lint`, `npm.cmd test`, `npm.cmd run build` проходят.

Изменена: стартовый Next.js-шаблон заменен на русский LineCom shell с AuthProvider, RequestDraftProvider, общей навигацией и restrained B2B styling.

## Итерация 5: Публичный каталог на frontend

Статус: в работе, реализация frontend выполнена 2026-05-07; полный browser QA заблокирован backend `500 internal_error`.

Показать публичные категории, товары и карточку товара на существующем public catalog API.

Результат:

- `/`;
- `/catalog`;
- `/catalog/[categorySlug]`;
- `/products/[slug]`;
- category navigation;
- product card;
- product detail;
- кнопка `Добавить в заявку`.

Критерии завершения:

- публичные страницы используют slug URL;
- category/product pages формируют metadata из backend SEO DTO;
- товары показывают `Цена по запросу`;
- карточка товара показывает характеристики, наличие, единицу продажи и количество в единице;
- в UI нет `Купить` и `Оформить заказ`;
- `npm.cmd run lint` и `npm.cmd run build` проходят.

Изменена: добавлены `/`, `/catalog`, `/catalog/[categorySlug]`, `/products/[slug]`, category navigation,
product card, product detail и кнопка `Добавить в заявку`. `npm.cmd run lint`, `npm.cmd test`,
`npm.cmd run build` проходят. Browser QA fallback-состояний пройден; happy path с реальными категориями
и товарами требует исправить локальный backend API, который сейчас возвращает `500 internal_error`.

## Итерация 6: Страница черновика заявки

Статус: выполнена 2026-05-07.

Дать пользователю рабочий экран подготовки заявки.

Результат:

- `/request`;
- список позиций;
- изменение количества;
- комментарий к позиции;
- общий комментарий;
- удаление позиции;
- пустое состояние;
- переход на login при попытке отправки без auth.

Критерии завершения:

- пустая заявка не отправляется;
- текст действия - `Отправить заявку`;
- пользовательский путь без auth ведет к входу, а не к ошибке;
- черновик не теряется при переходе на auth pages;
- компонент покрыт тестами.

Изменена: добавлены `/request`, `RequestDraftView` и тесты черновика заявки. Экран показывает пустое состояние,
позиции черновика, изменение количества, комментарии, удаление позиции и действие `Отправить заявку`; без auth
отправка ведет на login с возвратом к заявке. Проверки `npm.cmd test -- src/components/request/request-draft-view.test.tsx`,
`npm.cmd test`, `npm.cmd run lint`, `npm.cmd run build` прошли. Browser Use открыл `/request` и подтвердил отсутствие
`Купить` / `Оформить заказ`.

## Итерация 7: Auth и профиль клиента

Статус: выполнена 2026-05-07.

Реализовать вход, регистрацию, профиль и организацию на существующем backend API.

Результат:

- `/auth/login`;
- `/auth/register`;
- `/account/profile`;
- login/register forms;
- profile form;
- organization form;
- runtime auth state с `CurrentUserDto` и `csrfToken`.

Критерии завершения:

- успешный login/register сохраняет user + csrfToken в runtime state;
- register не создает организацию;
- protected profile page отправляет пользователя на login при `auth.unauthorized`;
- изменяющие account requests используют `X-CSRF-Token`;
- backend validation errors показываются контролируемо.

Изменена: добавлены страницы `/auth/login`, `/auth/register`, `/account/profile`, формы входа, регистрации,
профиля и организации. Login/register сохраняют `user` и `csrfToken` в runtime auth state и возвращают пользователя
к `returnTo`; регистрация не создает организацию. Профиль загружает `GET /api/auth/me` и `GET /api/account/profile`,
при `auth.unauthorized` ведет на login, а controlled backend errors показывает в UI. Проверки `npm.cmd run lint`,
`npm.cmd test`, `npm.cmd run build` прошли. Browser Use открыл auth/profile страницы и подтвердил отсутствие blank pages.

## Итерация 8: Отправка заявки

Статус: выполнена 2026-05-07.

Соединить черновик, auth и backend endpoint создания заявки.

Результат:

- `POST /api/account/requests` из frontend;
- payload с `source = cart`;
- retry один раз при `auth.forbidden` через refresh `GET /api/auth/me`;
- success redirect на карточку заявки;
- обработка `request.product_not_available`.

Критерии завершения:

- после login/register заявка не отправляется автоматически, пользователь явно нажимает `Отправить заявку`;
- backend генерирует номер заявки;
- после успеха черновик очищается или пользователь явно видит, что заявка создана;
- недоступный товар остается в черновике и подсвечивается контролируемой ошибкой;
- `npm.cmd run lint`, `npm.cmd run build`, `dotnet test LineCom.sln -m:1` проходят.

Изменена: `/request` подключен к `POST /api/account/requests` с payload `source = cart`, явным submit после auth,
очисткой черновика после успеха, редиректом на карточку созданной заявки, retry после `auth.forbidden` через `GET /api/auth/me`
и контролируемой ошибкой для недоступного товара. Проверки `npm.cmd test -- src/app/request/page.test.tsx`,
`npm.cmd test`, `npm.cmd run lint`, `npm.cmd run build`, `dotnet test LineCom.sln -m:1` прошли. Browser Use обновил `/request`
и подтвердил отсутствие blank page; полный happy-path с реальной заявкой остается ограничен локальной БД/секретом подключения.

## Итерация 9: Заявки в личном кабинете

Статус: выполнена 2026-05-07.

Показать пользователю список и карточку своих заявок.

Результат:

- `/account/requests`;
- `/account/requests/[number]`;
- request list;
- status filter;
- request detail;
- snapshots клиента, организации и товаров;
- история создания.

Критерии завершения:

- список ограничен текущим пользователем backend-контрактом;
- карточка использует публичный номер заявки;
- `request.not_found` показывается контролируемо;
- публичные цены не отображаются;
- даты форматируются для `ru-RU`.

Изменена: добавлены `/account/requests`, `/account/requests/[number]`, список заявок, фильтр статуса,
карточка заявки со снимками клиента, организации, позиций и историей. Protected pages загружают `GET /api/auth/me`
и request endpoints, при `auth.unauthorized` ведут на login с `returnTo`, а `request.not_found` показывают
контролируемо. Проверки `npm.cmd test -- src/components/account/request-list.test.tsx src/components/account/request-detail.test.tsx src/app/account/requests/requests-page-client.test.tsx src/app/account/requests/[number]/request-detail-page-client.test.tsx`,
`npm.cmd run lint`, `npm.cmd run build` прошли. Полный `npm.cmd test` в текущем окружении был прерван из-за OOM
Vitest workers после прохождения 13 из 14 test files; новые тесты итерации проверены отдельно. Browser Use открыл
`/account/requests` и `/account/requests/ЗК26-0001`: 404 нет, при недоступном backend показывается контролируемая ошибка.

## Итерация 10: Browser QA и закрытие этапа

Статус: QA выполнена 2026-05-07; полный happy path создания заявки заблокирован схемой подключенной БД.

Проверить весь путь в браузере и закрыть frontend-срез без намеренного технического долга.

Результат:

- happy path browser QA;
- responsive QA;
- проверка отсутствия запрещенной коммерческой лексики;
- проверка отсутствия TODO/TBD/FIXME в changed implementation files;
- финальные команды проверки.

Критерии завершения:

- пользователь проходит путь `каталог -> товар -> заявка -> регистрация/вход -> отправка -> карточка заявки -> список заявок`;
- нет blank pages;
- нет горизонтального overflow на мобильной ширине;
- длинные названия товаров корректно переносятся;
- `npm.cmd run lint` проходит;
- `npm.cmd test` проходит;
- `npm.cmd run build` проходит;
- `dotnet build LineCom.sln -m:1` проходит;
- `dotnet test LineCom.sln -m:1` проходит.

Изменена: выполнен финальный Playwright QA публичных, auth и account страниц на desktop и mobile ширинах.
Исправлен найденный QA-дефект с изображениями товаров: frontend теперь проксирует `/storage/:path*`, backend
публикует локальное файловое хранилище `/storage`, а локальный seed-файл `storage/products/cable.jpg` отдается
без 404. Playwright подтвердил отсутствие blank pages, горизонтального overflow и видимых `Купить` /
`Оформить заказ`; публичные страницы показывают `Цена по запросу`. Без auth protected pages ведут на login.

Проверки выполнены:

- `dotnet build LineCom.sln -m:1` - прошел, только `NU1900` warnings из-за недоступности NuGet vulnerability feed.
- `dotnet test LineCom.sln -m:1` - 285 passed.
- `npm.cmd run lint` - прошел.
- `npm.cmd test` - 16 files, 41 tests passed.
- `npm.cmd run build` - прошел.
- Playwright desktop/mobile routes: `/`, `/catalog`, `/catalog/vitaya-para`, `/products/u-utp-cat-5e`, `/request`,
  `/auth/login`, `/auth/register`, `/account/profile`, `/account/requests`, `/account/requests/ЗК26-0001`.
- `rg -n "Купить|Оформить заказ|оплат|цена \\d|TODO|TBD|FIXME|заглуш|костыл" apps/front docs/superpowers/specs docs/superpowers/plans` -
  запрещенная коммерческая лексика в `apps/front` не найдена; совпадения относятся к правилам в планах/спеках и
  случайному фрагменту integrity в `package-lock.json`.

Блокер полного happy path: `POST /api/auth/register` возвращает `500 internal_error`, потому что подключенная
локальная/QA БД не содержит таблицу `users` (`PostgreSQL 42P01 relation "users" does not exist`). Миграции на
подключенную БД не запускались в рамках QA без отдельного решения.

## Порядок работы после очистки контекста

1. Открыть эту заметку.
2. Открыть детальный план `docs/superpowers/plans/2026-05-07-frontend-auth-request-flow.md`.
3. Начать с первой итерации со статусом `запланирована`.
4. Перед реализацией конкретной итерации сверить файлы и команды из детального плана.
5. После завершения итерации обновить статус в этой заметке:
   - `запланирована`;
   - `в работе`;
   - `выполнена YYYY-MM-DD`;
   - `изменена: краткая причина`.
6. Если во время работы появляется более правильное решение, сначала обновить эту заметку или детальный план, затем продолжать реализацию.

## Текущая точка продолжения

Следующий шаг: для полного end-to-end happy path применить миграции auth/request к QA-БД или подключить БД,
где уже есть полный набор миграций, затем повторить сценарий `каталог -> товар -> заявка -> регистрация/вход ->
отправка -> карточка заявки -> список заявок`.

Отдельный QA-блокер остается для полного happy-path каталога с реальной БД: локальному backend нужен настроенный
`ConnectionStrings__Default`, потому что пароль development-песочницы не хранится в файлах проекта.

## Итерация 11: QA-БД и полный Playwright happy path

Статус: выполнена 2026-05-07.

Цель: снять блокер полного e2e-сценария `каталог -> товар -> заявка -> регистрация/вход -> отправка -> карточка заявки -> список заявок`.

План итерации: `docs/superpowers/plans/2026-05-07-qa-db-playwright-happy-path.md`.

Изменения:

- применены DbUp-миграции `003_auth_users_organizations.sql` и `004_requests.sql` к подключенной QA-БД;
- исправлен backend mapping в `DapperCustomerRequestRepository`: row-типы, читающие PostgreSQL `timestamptz`, используют `DateTime`, затем значения явно приводятся к доменным `DateTimeOffset`;
- добавлен regression-тест `DapperCustomerRequestRepositoryMappingTests`;
- исправлен `Location` header при создании заявки: кириллический номер заявки URL-encode'ится перед `Created(...)`;
- обновлен endpoint-тест создания заявки, чтобы проверять encoded `Location`.

Playwright QA:

- frontend проверялся на `http://127.0.0.1:3010`;
- backend работал на `http://127.0.0.1:8080`;
- создан QA-пользователь `qa-1778178414535@example.com`;
- создана заявка `ЗК26-0002`;
- проверены маршруты `/`, `/catalog`, `/catalog/vitaya-para`, `/products/u-utp-cat-5e`, `/request`, `/auth/login?returnTo=%2Frequest`, `/auth/register?returnTo=%2Frequest`, `/account/requests/%D0%97%D0%9A26-0002`, `/account/requests`;
- mobile responsive pass на ширине `390px` пройден для `/catalog`, `/catalog/vitaya-para`, `/products/u-utp-cat-5e`, `/request`, `/account/requests`, `/account/requests/%D0%97%D0%9A26-0002`;
- horizontal overflow не найден, browser console errors не найдены.

Проверки:

- `dotnet build LineCom.sln -m:1` - прошел, только `NU1900` warnings из-за недоступности NuGet vulnerability feed;
- `dotnet test LineCom.sln -m:1` - 289 passed;
- `npm.cmd run lint` - прошел;
- `npm.cmd test` - 17 files, 42 tests passed;
- `npm.cmd run build` - прошел;
- поиск `Купить|Оформить заказ|оплат|цена \d|TODO|TBD|FIXME|заглуш|костыл` не нашел запрещенную коммерческую лексику в `apps/front`; совпадения относятся к правилам/историческим заметкам в документации, команде поиска и случайному фрагменту integrity в `package-lock.json`.

Оставшиеся наблюдения:

- при старте backend в текущем пользовательском профиле ASP.NET DataProtection логирует DPAPI warnings по старым ключам, но приложение стартует и сценарий работает;
- во время Playwright-переходов были два `GET /api/auth/me net::ERR_ABORTED`, связанные с навигацией между routes; сценарий они не блокируют.

## Текущая точка продолжения после итерации 11

Frontend Auth + Request Flow имеет подтвержденный полный happy path на QA-БД. Следующая итерация может переходить к следующему продуктовому срезу: админская обработка заявок, импорт/привязка изображений каталога или дальнейшая каталоговая полнота.
