# План: Раннер автопроверки .NET-заданий

Студент отправляет GitHub URL + ветку → раннер через Outbox запускает пайплайн в GitLab → GitLab Runner клонирует студенческий код, прогоняет скрытые тесты → результат возвращается webhook'ом → раннер парсит NUnit XML и сохраняет результат. Всё разворачивается через Docker Compose.

---

## Роли

| Роль | Что может |
|---|---|
| `Admin` | Создавать задания (указывает название + `GitLabProjectId` — id репозитория с тестами) |
| `Student` | Отправлять решения, смотреть свои результаты |

---

## Как устроен GitLab-репозиторий задания

В каждом приватном репозитории GitLab лежат:
- `.gitlab-ci.yml` — пайплайн, который умеет принять `STUDENT_REPO` / `STUDENT_BRANCH` / `SUBMISSION_ID` как переменные и сам знает что делать
- `tests/` — скрытые тесты
- Сам пайплайн клонирует код студента внутрь контейнера, билдит и тестирует рядом со скрытыми тестами

Раннер только **запускает** этот пайплайн с нужными переменными — шаблоны CI в коде раннера не нужны.

---

## Структура решения

```
Runner.sln
  src/
    Runner.Api/            ← точка входа (текущий WebApplication1)
    Runner.Application/    ← use cases, интерфейсы
    Runner.Domain/         ← сущности, enum'ы
    Runner.Infrastructure/ ← EF Core, GitLab HTTP-клиент, парсер XML
  docker-compose.yml
  docker-compose.override.yml   ← dev-секреты
```

---

## Шаги реализации

1. **Реструктурировать решение** — переименовать `WebApplication1` → `Runner.Api`, добавить проекты `Runner.Domain`, `Runner.Application`, `Runner.Infrastructure` в `Runner.sln`

2. **Доменная модель** в `Runner.Domain` — сущности:
   - `Assignment` — `Id`, `Title`, `GitLabProjectId` (long, id репозитория GitLab), `Type` (enum: `Algorithm` / `Endpoint` / `Coverage`), `CoverageThreshold?`
   - `Submission` — `Id`, `StudentId`, `AssignmentId`, `GitHubUrl`, `Branch`, `GitLabPipelineId?`, `Status` (enum: `Pending` → `Triggered` → `Running` → `Passed` / `Failed` / `Error` / `Timeout`)
   - `CheckResult` — `Id`, `SubmissionId`, `TotalTests`, `PassedTests`, `FailedTests`, `RawNUnitXml`
   - `TestGroupResult` — `Id`, `CheckResultId`, `GroupName`, `Passed`, `Failed`, `ErrorType?`, `ErrorMessage?`
   - `OutboxMessage` — `Id`, `Payload` (JSON), `CreatedAt`, `ProcessedAt?`, `RetryCount`, `Error?`
   - `UserProfile` — `Id`, `GitHubLogin`, `GitHubId`, `Role` (enum: `Admin` / `Student`)
   - `ErrorType` enum: `CompilationError`, `AssertionFailed`, `Timeout`, `CoverageBelow`, `InterfaceNotFound`

3. **PostgreSQL + EF Core** в `Runner.Infrastructure` — NuGet: `Npgsql.EntityFrameworkCore.PostgreSQL`; `RunnerDbContext` с таблицами для всех сущностей; миграции в `Runner.Infrastructure/Migrations/`; строка подключения в `appsettings.json`

4. **GitHub OAuth** в `Runner.Api` — NuGet: `AspNet.Security.OAuth.GitHub`; при первом входе — создать `UserProfile` со стандартной ролью `Student`; Admin назначается вручную в БД (или через отдельный endpoint защищённый флагом)

5. **`POST /submissions` + Outbox** в `Runner.Application` — валидировать GitHub URL, создать `Submission (Pending)` + `OutboxMessage` в одной EF-транзакции; `OutboxWorker : BackgroundService` каждые 10 сек достаёт необработанные записи и вызывает `POST /api/v4/projects/{GitLabProjectId}/pipeline` с переменными `STUDENT_REPO`, `STUDENT_BRANCH`, `SUBMISSION_ID`; при успехе — `ProcessedAt = now`, `Status = Triggered`; при ошибке 5 раз — `Status = Error`

6. **`POST /webhooks/gitlab`** в `Runner.Api` — валидация `X-Gitlab-Token`; при событии `pipeline` со статусом `success` — по `SUBMISSION_ID` из `variables` найти `Submission`, скачать `TestResult.xml` через `GET /api/v4/projects/{id}/jobs/{job_id}/artifacts/TestResult.xml`, распарсить, сохранить `CheckResult` + список `TestGroupResult`, обновить `Status = Passed/Failed`; при `failed` без артефакта — `Status = Error`; при `canceled` — `Status = Timeout`

7. **NUnit XML парсер** в `Runner.Infrastructure` — разобрать `<test-suite name>` → `GroupName`; считать `passed/failed` из атрибутов; `<failure>` → маппинг в `ErrorType`:
   - `"CompilationError"` → `CompilationError`
   - `"AssertionException"` / `"AssertionError"` / пусто → `AssertionFailed`
   - `"TaskCanceledException"` / `"TimeoutException"` → `Timeout`
   - `"NotImplementedException"` → `InterfaceNotFound`
   - Остальное → `AssertionFailed`

8. **REST API** в `Runner.Api`:
   - `POST /assignments` (только `Admin`) — создать задание, указав `Title` + `GitLabProjectId` + `Type`
   - `POST /submissions` (только `Student`) — отправить решение
   - `GET /submissions/{id}` — статус + `passed/total`
   - `GET /submissions/{id}/report` — полный отчёт по группам тестов

9. **Docker Compose** в корне решения — сервисы: `runner-api` (из `Dockerfile`), `postgres`, `gitlab-runner` (образ `gitlab/gitlab-runner`); переменные окружения и секреты через `docker-compose.override.yml` (в `.gitignore`)

---

## Дополнительные соображения

1. **Секреты** — `GitLab:ServiceToken` (для вызова API и скачивания артефактов), `GitLab:WebhookSecret`, `GitHub:ClientId/ClientSecret` — всё через `.env` файл для Docker Compose, User Secrets для локальной разработки

2. **Безопасность пайплайна** — пайплайн запускается от имени сервисного аккаунта с минимальными правами (`Reporter` на репозиторий + `trigger token`); код студента не попадает в GitLab и не имеет доступа к секретам CI

3. **Таймаут пайплайна** — установить `timeout` в `.gitlab-ci.yml` (например, 5 минут), чтобы зависший студенческий код не блокировал раннеры; `Timeout` как отдельный `ErrorType` в отчёте

4. **Moodle (поздний этап)** — в `Assignment` уже есть nullable `ExternalAssignmentId` + `ExternalPlatform` enum; интеграция добавляется позже без изменения схемы
