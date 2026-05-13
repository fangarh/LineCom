# Production deployment line-com.ru

Дата первичной настройки: 2026-05-13

## Доступ

- Хост: `109.248.226.178`
- SSH-пользователь для временных работ: `croot`
- Пароль не хранить в git и Obsidian.
- Локальный файл для пароля SSH: `.codex-local/linecom-ssh.env`
- Переменные для автоматизации: `LC_SSH_HOST`, `LC_SSH_USER`, `LC_SSH_PASSWORD`

Локальный файл `.codex-local/linecom-ssh.env` добавлен в `.gitignore`. Перед SSH-операциями пароль нужно вписать в `LC_SSH_PASSWORD`.

## Домен и DNS

- Основной домен: `line-com.ru`
- WWW-домен: `www.line-com.ru`
- Публичный IP сайта: `109.248.226.178`
- DNS управляется через RU-CENTER DNS-master.
- A-записи:
  - `@ A 109.248.226.178`
  - `www A 109.248.226.178`

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

## Storage картинок

13 мая 2026 выяснилось, что сайт отдавал 404 на `/storage/products/...`, потому что production storage был пустой. В `/var/lib/linecom/storage` загружены файлы из локального `apps/api/storage/products`.

Проверка после загрузки:

- `https://line-com.ru/storage/products/cable.jpg` -> `200 image/jpeg`
- `https://line-com.ru/storage/products/catalog-import/...png` -> `200 image/png`
- Свежая загрузка главной страницы в браузере не показала ошибок по storage-файлам.

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
