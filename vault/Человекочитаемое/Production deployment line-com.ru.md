# Production deployment line-com.ru

Дата первичной настройки: 2026-05-13

## Доступ

- Хост: `188.130.138.146`
- SSH-пользователь для временных работ: `croot`
- Пароль не хранить в git и Obsidian.
- Локальный файл для пароля SSH: `.codex-local/linecom-ssh.env`
- Переменные для автоматизации production: `PROD_SSH_HOST`, `PROD_SSH_USER`, `PROD_SSH_PASSWORD`
- Переменные старой площадки/миграционных работ: `LC_SSH_HOST`, `LC_SSH_USER`, `LC_SSH_PASSWORD`

Локальный файл `.codex-local/linecom-ssh.env` добавлен в `.gitignore`. Перед production SSH-операциями пароль нужно вписать в `PROD_SSH_PASSWORD`.

## Домен и DNS

- Основной домен: `line-com.ru`
- WWW-домен: `www.line-com.ru`
- Публичный IP сайта: `188.130.138.146`
- DNS управляется через RU-CENTER DNS-master.
- A-записи:
  - `@ A 188.130.138.146`
  - `www A 188.130.138.146`

## Почта

Почта остается на Nicmail/RU-CENTER. При изменении DNS сайта эти записи нужно сохранять.

- MX:
  - `@ MX 5 mx02.nicmail.ru.`
  - `@ MX 10 mx01.nicmail.ru.`
  - `@ MX 20 mx03.nicmail.ru.`
- TXT:
  - `@ TXT "v=spf1 redirect=nicmail.ru"`
  - `@ TXT "_globalsign-domain-verification=xWzviafy-TvT1I6vfMid6UPEbxThKMjBjt15trRBgP"`
- A:
  - `mail A 91.189.116.40`
  - `mail A 91.189.116.41`
  - `mail A 91.189.116.42`
  - `mail A 91.189.116.43`

Важная деталь DNS-master: MX-значения должны быть абсолютными именами с точкой на конце. Без точки DNS-master может опубликовать `mx02.nicmail.ru.line-com.ru`, что ломает входящую почту.

Проверенные настройки почтового клиента:

- IMAP: `mail.nic.ru`, порт `993`, `SSL/TLS`, пользователь полный email.
- SMTP: `mail.nic.ru`, порт `587`, `STARTTLS/TLS`, пользователь полный email.
- `smtp.nic.ru` и `imap.nic.ru` не использовать: эти имена не резолвятся.

## Сервер

- ОС: Ubuntu 24.04 LTS.
- Web server: nginx.
- Backend runtime: ASP.NET Core Runtime 8.
- Frontend runtime: Node.js.
- Database: PostgreSQL 16.
- TLS: Let's Encrypt через certbot.

Системные пользователи и каталоги:

- Runtime-пользователь приложения: `linecom`
- Релизы: `/opt/linecom/releases/`
- Текущий API: `/opt/linecom/api/current`
- Текущий frontend: `/opt/linecom/front/current`
- Текущий dbmigrator: `/opt/linecom/dbmigrator/current`
- Файловое хранилище: `/var/lib/linecom/storage`
- Конфиг API: `/etc/linecom/api.env`
- Конфиг frontend: `/etc/linecom/front.env`

Systemd:

- API: `linecom-api.service`, слушает `127.0.0.1:8080`
- Frontend: `linecom-front.service`, слушает `127.0.0.1:3000`

Nginx:

- `/` проксируется на Next.js frontend.
- `/api/` проксируется на ASP.NET API.
- `/storage/` проксируется на ASP.NET API, который отдает файлы из `/var/lib/linecom/storage`.

TLS:

- Сертификат выпущен для `line-com.ru` и `www.line-com.ru`.
- Путь сертификата: `/etc/letsencrypt/live/line-com.ru/fullchain.pem`
- Certbot auto-renew включен.

## Актуальная production-публикация

3 июня 2026 production перенесён на сервер `188.130.138.146`.

4 июня 2026 опубликован текущий dirty-релиз с исправлением контраста названий товаров в темной теме и текущими frontend-изменениями.

- Hostname сервера: `line-com.ru`.
- Release id: `20260604-061124-a07976f-dirty`.
- API current: `/opt/linecom/releases/api-20260604-061124-a07976f-dirty`.
- Frontend current: `/opt/linecom/releases/front-20260604-061124-a07976f-dirty`.
- DbMigrator current: `/opt/linecom/releases/dbmigrator-20260604-061124-a07976f-dirty`.
- PostgreSQL и Local FileStorage восстановлены из согласованного backup point старой площадки: `/var/backups/linecom/20260603T170001Z-prepublish-20260603-a07976f-dirty`.
- Storage после восстановления: `325` файлов.
- DbMigrator на новом production: новых SQL-скриптов не было, журнал миграций актуален.
- Smoke после публикации `20260604-061124-a07976f-dirty`: `https://line-com.ru/` -> `200`, `https://line-com.ru/cookies` -> `200`, `GET https://line-com.ru/api/public/system/health` -> `200`; CSS главной содержит `.home-hero-product.is-active strong{color:#20262b}` и `.home-hero-product.is-active small{color:#62686f}`.
- HTTPS включён через Let's Encrypt для `line-com.ru` и `www.line-com.ru`.
- Сертификат на момент выпуска действителен до `2026-09-01`.
- Старая площадка `109.248.226.178` после переноса оставлена только как временный nginx proxy на `188.130.138.146` для защиты от остаточного DNS-cache; старые `linecom-api.service` и `linecom-front.service` остановлены.
- Контрольная сверка старой и новой площадок после переноса: counts всех public-таблиц совпадают, `schema_versions` = `7`, latest = `LineCom.DbMigrator.Migrations.007_admin_catalog_foundation.sql`, storage = `325` файлов, storage manifest SHA256 = `ceb000c191c304c89eb289f822206f1564753fb4c9a00d05d9f8c7b233c083b9`.

## Storage картинок

13 мая 2026 выяснилось, что сайт отдавал 404 на `/storage/products/...`, потому что production storage был пустой. В `/var/lib/linecom/storage` загружены файлы из локального `apps/api/storage/products`.

Проверка после загрузки:

- `https://line-com.ru/storage/products/cable.jpg` -> `200 image/jpeg`
- `https://line-com.ru/storage/products/catalog-import/...png` -> `200 image/png`
- Свежая загрузка главной страницы в браузере не показала ошибок по storage-файлам.

## Release runbook

Этот раздел является рабочим чеклистом production-релиза. Секреты в документ не вносить: здесь фиксируются только имена переменных, пути и команды.

### 1. Предрелизная проверка

Перед сборкой релиза:

```powershell
dotnet test LineCom.sln
npm.cmd --prefix apps/front test
$env:LINECOM_PUBLIC_SITE_ORIGIN='https://line-com.ru'; npm.cmd --prefix apps/front run build
npm.cmd --prefix apps/front audit --json
dotnet list LineCom.sln package --vulnerable --include-transitive
```

Проверить, что:

- нет failed tests;
- `npm audit` не содержит `critical` или `high` findings;
- NuGet vulnerable audit не содержит `critical` или `high` findings;
- production origin для frontend равен `https://line-com.ru`;
- миграции DbUp не содержат ручных правок в production базе.

### 2. Сборка артефактов

API:

```powershell
dotnet publish apps/api/LineCom.Api.csproj -c Release -o artifacts/api
```

DbUp migrator:

```powershell
dotnet publish apps/dbmigrator/LineCom.DbMigrator.csproj -c Release -o artifacts/dbmigrator
```

Frontend:

```powershell
$env:LINECOM_PUBLIC_SITE_ORIGIN='https://line-com.ru'
npm.cmd --prefix apps/front run build
```

Production frontend использует Next.js standalone output. В релизный каталог frontend переносить standalone output, `.next/static`, `public`, `package.json` и lockfile в составе выбранной схемы деплоя.

### 3. Конфигурация production

API env-файл: `/etc/linecom/api.env`.

Обязательные значения проверить по именам, не раскрывая секреты:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://127.0.0.1:8080`
- `ConnectionStrings__Default`
- `Storage__RootPath=/var/lib/linecom/storage`
- production origin / CORS / cookie параметры, если они вынесены в env.

Frontend env-файл: `/etc/linecom/front.env`.

Обязательные значения проверить по именам:

- `NODE_ENV=production`
- `PORT=3000`
- `HOSTNAME=127.0.0.1`
- `LINECOM_API_ORIGIN=http://127.0.0.1:8080`
- `LINECOM_PUBLIC_SITE_ORIGIN=https://line-com.ru`

Local FileStorage остается целевым storage-подходом проекта. Production root: `/var/lib/linecom/storage`. Не заменять его на S3/MinIO в рамках v1 release gate.

### 4. Миграции DbUp

Миграции выполняются отдельным шагом до переключения runtime-сервисов на новый релиз.

На сервере:

```bash
cd /opt/linecom/dbmigrator/current
LINECOM_CONNECTION_STRING="$LINECOM_CONNECTION_STRING" ./LineCom.DbMigrator
```

Остановить релиз и не переключать сервисы, если:

- migrator завершился с ненулевым кодом;
- есть ошибка подключения к PostgreSQL;
- есть ошибка применения SQL migration;
- журнал `public.schema_versions` не отражает ожидаемые миграции.

Откат SQL migration не выполнять вручную без отдельного решения: сначала сохранить логи migrator, дамп базы и текущий release id.

### 5. Переключение сервисов

После загрузки артефактов и успешных миграций:

```bash
sudo systemctl daemon-reload
sudo systemctl restart linecom-api.service
sudo systemctl restart linecom-front.service
sudo systemctl status linecom-api.service --no-pager
sudo systemctl status linecom-front.service --no-pager
```

Проверить nginx:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

## Coordinated backup and restore

Backup должен фиксировать одну согласованную точку: PostgreSQL dump и архив Local FileStorage относятся к одному release/backup id.

## Именованные backup points

### release1

- Alias для восстановления: `release1`.
- Тип: coordinated backup point, PostgreSQL dump + Local FileStorage archive.
- Создано UTC: `2026-05-15T15:13:47Z`.
- Storage добавлен UTC: `2026-05-15T15:21:09Z`.
- Сервер: `line-com.ru`.
- Каталог на сервере: `/var/backups/linecom/release1`.
- Dump: `/var/backups/linecom/release1/linecom.pgcustom`.
- Формат dump: `pg_dump --format=custom --no-owner --no-acl`.
- Restore list: `/var/backups/linecom/release1/linecom.pgcustom.list`.
- SHA256 file: `/var/backups/linecom/release1/linecom.pgcustom.sha256`.
- Размер dump: `85467` bytes.
- `pg_restore --list`: `185` lines.
- Storage archive: `/var/backups/linecom/release1/storage.tgz`.
- Storage list: `/var/backups/linecom/release1/storage.tgz.list`.
- Storage SHA256 file: `/var/backups/linecom/release1/storage.tgz.sha256`.
- Storage archive size: `25621275` bytes.
- Storage archive list: `84` lines.
- Storage files in archive: `79`.
- Frontend current на момент backup: `/opt/linecom/releases/front-20260515175433`.
- API current на момент backup: `/opt/linecom/releases/api-20260514170348`.
- DbMigrator current на момент backup: `/opt/linecom/releases/20260514130305/dbmigrator`.

Если пользователь попросит восстановить `release1`, использовать dump и storage archive из этого каталога:

```bash
BACKUP_DIR="/var/backups/linecom/release1"
pg_restore --clean --if-exists --no-owner --dbname "$LINECOM_CONNECTION_STRING" "$BACKUP_DIR/linecom.pgcustom"
sudo tar -C /var/lib/linecom -xzf "$BACKUP_DIR/storage.tgz"
sudo chown -R linecom:linecom /var/lib/linecom/storage
```

Перед restore production database и storage остановить runtime-сервисы, сохранить свежие логи и подтвердить, что требуется восстановить именно `release1`.

### 1. Создание backup point

Выбрать идентификатор backup point:

```bash
BACKUP_ID="$(date -u +%Y%m%dT%H%M%SZ)-linecom"
BACKUP_DIR="/var/backups/linecom/$BACKUP_ID"
sudo mkdir -p "$BACKUP_DIR"
```

Зафиксировать metadata:

```bash
sudo sh -c "cat > '$BACKUP_DIR/metadata.txt'" <<'EOF'
backup_id=
created_utc=
release_id=
database=PostgreSQL
storage_root=/var/lib/linecom/storage
api_current=/opt/linecom/api/current
front_current=/opt/linecom/front/current
dbmigrator_current=/opt/linecom/dbmigrator/current
EOF
```

Заполнить значения без секретов.

### 2. PostgreSQL dump

```bash
pg_dump --format=custom --no-owner --no-acl --file "$BACKUP_DIR/linecom.pgcustom" "$LINECOM_CONNECTION_STRING"
```

Проверка dump:

```bash
pg_restore --list "$BACKUP_DIR/linecom.pgcustom" > "$BACKUP_DIR/linecom.pgcustom.list"
test -s "$BACKUP_DIR/linecom.pgcustom.list"
```

### 3. Local FileStorage archive

```bash
sudo tar --xattrs --acls -C /var/lib/linecom -czf "$BACKUP_DIR/storage.tgz" storage
sudo tar -tzf "$BACKUP_DIR/storage.tgz" > "$BACKUP_DIR/storage.tgz.list"
test -s "$BACKUP_DIR/storage.tgz.list"
```

Backup считается пригодным только если есть оба файла:

- `$BACKUP_DIR/linecom.pgcustom`
- `$BACKUP_DIR/storage.tgz`

Однослойный restore только базы или только файлов считать risk scenario: БД и storage могут разойтись по ссылкам `stored_files` и физическим файлам.

### 4. Dry-run restore на отдельную цель

Dry-run restore выполняется только на отдельном host/database/storage path. Production database и `/var/lib/linecom/storage` не использовать как цель dry-run.

Пример целевых значений:

- database: `linecom_restore_drill`
- storage root: `/var/lib/linecom-restore-drill/storage`
- API port: `18080`
- frontend port: `13000`

Восстановить базу:

```bash
createdb linecom_restore_drill
pg_restore --clean --if-exists --no-owner --dbname "$RESTORE_CONNECTION_STRING" "$BACKUP_DIR/linecom.pgcustom"
```

Восстановить storage:

```bash
sudo mkdir -p /var/lib/linecom-restore-drill
sudo tar -C /var/lib/linecom-restore-drill -xzf "$BACKUP_DIR/storage.tgz"
sudo chown -R linecom:linecom /var/lib/linecom-restore-drill/storage
```

Запустить API dry-run с отдельной конфигурацией:

```bash
ASPNETCORE_ENVIRONMENT=Production \
ASPNETCORE_URLS=http://127.0.0.1:18080 \
ConnectionStrings__Default="$RESTORE_CONNECTION_STRING" \
Storage__RootPath=/var/lib/linecom-restore-drill/storage \
/opt/linecom/api/current/LineCom.Api
```

Frontend dry-run должен указывать на dry-run API:

```bash
LINECOM_API_ORIGIN=http://127.0.0.1:18080 \
LINECOM_PUBLIC_SITE_ORIGIN=https://line-com.ru \
PORT=13000 \
HOSTNAME=127.0.0.1 \
node /opt/linecom/front/current/server.js
```

### 5. Post-restore smoke checks

Для dry-run API/frontend:

```bash
curl.exe -I http://127.0.0.1:18080/api/public/health
curl.exe -I http://127.0.0.1:13000/
curl.exe -I http://127.0.0.1:13000/robots.txt
curl.exe -I http://127.0.0.1:13000/sitemap.xml
curl.exe -I http://127.0.0.1:13000/storage/products/cable.jpg
```

Проверить вручную:

- публичный каталог открывается;
- карточка товара с изображением открывается;
- `/storage/products/...` возвращает файл из restored Local FileStorage;
- заявки не создаются в production базе;
- dry-run сервисы остановлены после проверки.

## Быстрые проверки

DNS сайта:

```powershell
nslookup line-com.ru 1.1.1.1
nslookup www.line-com.ru 1.1.1.1
```

DNS почты:

```powershell
nslookup -type=mx line-com.ru 1.1.1.1
nslookup -type=txt line-com.ru 1.1.1.1
nslookup mail.line-com.ru 1.1.1.1
```

HTTP/HTTPS:

```powershell
curl.exe -I https://line-com.ru/
curl.exe -I https://www.line-com.ru/
curl.exe -I https://line-com.ru/storage/products/cable.jpg
```

SSH через локальный env-файл:

```powershell
Get-Content .codex-local/linecom-ssh.env | ForEach-Object {
  if ($_ -match '^([^#=]+)=(.*)$') {
    [Environment]::SetEnvironmentVariable($matches[1], $matches[2], 'Process')
  }
}
```
