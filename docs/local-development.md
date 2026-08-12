# Локальный запуск StudioB2B

## Назначение и состав

StudioB2B — multi-tenant OMS на ASP.NET Core/Blazor Server. Решение `StudioB2B.sln` содержит веб-приложение `StudioB2B.Web`, слои `Domain`, `Infrastructure`, `Shared` и два тестовых проекта. Данные хранятся в MySQL: есть master-БД и отдельные БД тенантов. При успешном старте приложение автоматически применяет миграции master-БД, создаёт базовые роли/администратора и обновляет миграции существующих тенантов.

## Проверенное окружение

- macOS ARM64;
- .NET SDK `10.0.302` (`net10.0` — целевая платформа всех проектов);
- MySQL 8+;
- порт веб-приложения: `5184`.

SDK установлен через Homebrew:

```bash
brew install dotnet
```

Если в `PATH` раньше попадает старый SDK, для текущей сессии используйте SDK Homebrew:

```bash
export DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version
```

## Схема запуска

```
Браузер ── HTTP :5184 ──> StudioB2B.Web
                                  │
                                  ├──> master MySQL: пользователи, тенанты, роли,
                                  │    миграции и `MasterHangfire_*`
                                  │
                                  └──> tenant MySQL: данные одного тенанта,
                                       миграции и `Hangfire_*`
```

Master-домен (`MultiTenancy:MasterDomain`) обслуживает вход и управление тенантами. Запрос на активный поддомен ищет запись тенанта в master-БД; если записи нет, middleware перенаправляет браузер на master-домен. Для локальной разработки `localhost` и `<поддомен>.localhost` подходят без настройки DNS.

## Порты и внешние сервисы

| Порт | Кто использует | Когда нужен |
|---|---|---|
| `5184/tcp` | `StudioB2B.Web` (Kestrel, HTTP) | Всегда при запуске профиля `http`; в Docker это порт контейнера и его нужно опубликовать. |
| `7253/tcp` | Kestrel, HTTPS | Только при запуске launch profile `https` и наличии доверенного development-сертификата. |
| `3306/tcp` | MySQL в проверенном локальном окружении | Всегда: master-БД и БД тенантов. Порт может быть другим, но его надо одинаково указать в обеих строках подключения. |
| `3345/tcp` | MySQL из текущего `appsettings.Development.json` | Альтернативная локальная конфигурация; в проверенном окружении этот порт не слушается. |
| `5341/tcp` | Seq | Не поднимается приложением. Нужен, только если включён Serilog sink Seq. |
| `9000/tcp` | MinIO/S3-совместимое хранилище | Не поднимается приложением. Нужен только для функции резервных копий. |
| `443/tcp` (исходящий) | Ozon и OpenRouter | Нужен только для синхронизации маркетплейса и AI-функций. |
| `587/tcp` (исходящий) | SMTP | Нужен для отправки писем; адрес и TLS определяются секцией `Email`. |

В репозитории нет `docker-compose`: запуск Docker-образа не создаёт MySQL, MinIO, Seq или SMTP-сервер.

## Конфигурация

Не добавляйте пароли, JWT-ключи, ключ шифрования и ключи API в Git. Локальные значения можно передать переменными среды: в .NET вложенные секции разделяются `__`.

Минимальный набор для запуска:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__MasterDb='Server=127.0.0.1;Port=3306;Database=StudioB2B_Master_Dev;User=root;Password=<пароль>;AllowUserVariables=True;AllowPublicKeyRetrieval=True;'
export MultiTenancy__MasterDomain=localhost
export MultiTenancy__TenantDbConnectionTemplate='Server=127.0.0.1;Port=3306;Database={0};User=root;Password=<пароль>;AllowUserVariables=True;AllowPublicKeyRetrieval=True;'
export Jwt__Secret='<минимум 32 случайных символа>'
export Jwt__Issuer=StudioB2B
export Jwt__Audience=StudioB2B
export Encryption__Key='<base64-ключ>'
export Seed__AdminEmail='admin@example.test'
export Seed__AdminPassword='<надёжный пароль>'
```

`appsettings.Development.json` в текущей рабочей копии настроен на MySQL-порт `3345`, тогда как локальный сервис MySQL прослушивает `3306`. Поэтому для этого окружения требуется передать строку подключения с `Port=3306`, как в примере выше, либо изменить локальный конфигурационный файл, не коммитя секреты. У пользователя MySQL должны быть права на создание баз и таблиц: EF Core применяет миграции автоматически.

`Ozon`, `OpenRouter`, `Backup` (MinIO и `mysql`/`mysqldump`) и `Email` нужны только при использовании соответствующих возможностей. Ключи Ozon для marketplace-клиентов хранятся в tenant-БД. В production Docker-образ также требует задать `ConnectionStrings__MasterDb` и `MultiTenancy__TenantDbConnectionTemplate` извне.

## Установка пакетов, сборка и запуск

```bash
dotnet restore StudioB2B.Web/StudioB2B.Web.csproj
dotnet build StudioB2B.Web/StudioB2B.Web.csproj --no-restore
dotnet run --project StudioB2B.Web/StudioB2B.Web.csproj --launch-profile http
```

После запуска откройте `http://localhost:5184` и войдите под учётной записью из `Seed__AdminEmail` / `Seed__AdminPassword`.

## Что происходит при запуске

### Первый запуск с пустым MySQL

1. Kestrel открывает HTTP-порт `5184`; Blazor Server, API-контроллеры и SignalR hubs (`/hubs/sync`, `/hubs/taskboard`, `/hubs/ozon-push`) работают в том же процессе и на том же порту.
2. `DatabaseMigrationService` подключается к БД из `ConnectionStrings:MasterDb`. Если БД ещё нет, EF Core создаёт её; затем применяются миграции master-схемы.
3. В master-БД создаются таблицы `Users`, `Roles`, `UserRoles`, `Tenants`, история EF-миграций, таблицы резервных копий и служебные Hangfire-таблицы с префиксом `MasterHangfire_`.
4. Seed создаёт роли `Admin` и `User`, затем активного и подтверждённого master-администратора из `Seed:AdminEmail` / `Seed:AdminPassword`. Поэтому эти две переменные обязательны для первого запуска.
5. Запускаются master Hangfire worker и менеджер tenant workers. Если в `Tenants` нет записей, tenant worker и tenant-БД не создаются.

После этого получается рабочая master-часть: страница входа и возможность зарегистрировать первого тенанта. В проверенном запуске была создана master-БД `StudioB2B_Master_Dev`, применены 5 master-миграций, созданы две роли и один администратор; число тенантов осталось равным нулю.

### Создание тенанта

Регистрация тенанта доступна из master-части. Поддомен должен иметь 3–30 символов, состоять из строчных латинских букв, цифр и дефисов и не быть зарезервированным. Для поддомена `demo` приложение:

1. Добавляет запись в master-таблицу `Tenants`.
2. Формирует имя БД как `StudioB2B_Tenant_demo`: код подставляет `StudioB2B_Tenant_<поддомен>` вместо `{0}` в `MultiTenancy:TenantDbConnectionTemplate`.
3. Создаёт tenant-БД и применяет tenant-миграции; добавляет базовые страницы, колонки, права, ценовые типы, правила расчёта, операции и технического пользователя.
4. Создаёт администратора тенанта из данных регистрации.
5. Создаёт Hangfire-таблицы с префиксом `Hangfire_`, запускает отдельный worker для очереди тенанта и регистрирует периодическую синхронизацию коммуникаций раз в 5 минут.

Начальные задания синхронизации заказов, обновлений и возвратов добавляются только если в новой tenant-БД уже есть marketplace-клиент. На чистой базе таких клиентов нет, поэтому внешние запросы к Ozon сразу после регистрации не выполняются.

При ошибке регистрации приложение пытается удалить созданную tenant-БД и запись тенанта из master-БД. Это означает, что у MySQL-пользователя необходимы также права `DROP` для полного автоматического отката.

Для контейнерного запуска используется `Dockerfile`, но compose-файла в репозитории нет: MySQL и остальные внешние сервисы надо предоставить отдельно.

```bash
docker build -t studiob2b .
docker run --rm -p 5184:5184 \
  -e ConnectionStrings__MasterDb='<строка подключения>' \
  -e MultiTenancy__MasterDomain=localhost \
  -e MultiTenancy__TenantDbConnectionTemplate='<шаблон строки подключения>' \
  -e Jwt__Secret='<секрет>' \
  -e Jwt__Issuer=StudioB2B \
  -e Jwt__Audience=StudioB2B \
  studiob2b
```

## Результат проверки

Команды ниже выполнены после установки SDK 10.0.302:

```bash
dotnet restore StudioB2B.Web/StudioB2B.Web.csproj
dotnet build StudioB2B.Web/StudioB2B.Web.csproj --no-restore
```

Веб-проект и его production-зависимости собираются успешно, без предупреждений и ошибок.

Полная сборка `StudioB2B.sln` и тесты сейчас заблокированы не кодом приложения, а восстановлением пакетов тестовых проектов: wildcard-зависимость `Microsoft.Extensions.Logging.Abstractions` `10.*` выбирает версию, которой нужен несуществующий стабильный пакет `Microsoft.Extensions.DependencyInjection.Abstractions >= 10.0.11` (`NU1103`). До исправления зависимостей в `tests/StudioB2B.Tests.Unit/StudioB2B.Tests.Unit.csproj` и `tests/StudioB2B.Tests.Integration/StudioB2B.Tests.Integration.csproj` `dotnet test` не запустится. NuGet дополнительно сообщает об известных уязвимостях транзитивных пакетов интеграционных тестов (`OpenTelemetry` и `Scriban.Signed`).

## Полезные команды

```bash
# Проверить доступность MySQL
mysqladmin --protocol=tcp --host=127.0.0.1 --port=3306 --user=root ping

# Собрать только приложение
dotnet build StudioB2B.Web/StudioB2B.Web.csproj

# После исправления NuGet-зависимости тестов
dotnet test StudioB2B.sln
```
