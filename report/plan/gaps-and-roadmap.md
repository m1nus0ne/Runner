# Анализ пробелов и план доработки системы Runner

> Дата анализа: 08.04.2026
> Основание: ревью всех файлов проекта после рефакторинга на 4-модульную архитектуру

---

## 1. Общая сводка состояния проекта

| Компонент | Статус | Полнота |
|---|---|---|
| Runner.Api (Host) | ✅ Реализован | ~85 % |
| Runner.SharedKernel | ✅ Реализован | ~70 % |
| Runner.Auth.Module | ✅ Реализован | ~90 % |
| Runner.Parsers.Module | ✅ Реализован | ~95 % |
| Runner.Runner.Module | ✅ Реализован | ~90 % |
| Runner.Submissions.Module | ✅ Реализован | ~80 % |
| Runner.ProTech.Module | ❌ Не создан (планируемый) | 0 % |
| Регистрация Runner + Parsers в Program.cs | ⚠️ Не подключены | 0 % |
| Миграция для TemplateRepoUrl | ❌ Отсутствует | 0 % |
| Автоматизированные тесты | ❌ Отсутствуют | 0 % |

---

## 2. Текущая модульная архитектура

После рефакторинга система разделена на 4 бизнес-модуля:

```
src/
├── Runner.Api/                          ← Host (Program.cs, Middleware)
├── Runner.SharedKernel/                 ← Entity, Exceptions, PipelineTriggerPayload
└── Modules/
    ├── Auth/
    │   └── Runner.Auth.Module/          ← OAuth, Cookie Auth, Endpoints (/auth/*)
    │       ├── Api/AuthEndpoints.cs
    │       └── AuthModuleExtensions.cs
    ├── Parsers/
    │   └── Runner.Parsers.Module/       ← Парсинг NUnit XML, классификация ошибок
    │       ├── Application/Interfaces/INUnitXmlParser.cs
    │       ├── Infrastructure/NUnit/NUnitXmlParser.cs
    │       └── ParsersModuleExtensions.cs
    ├── Runner/
    │   └── Runner.Runner.Module/        ← OutboxWorker, GitLab, Webhooks
    │       ├── Api/WebhookEndpoints.cs
    │       ├── Application/
    │       │   ├── Interfaces/IGitLabClient.cs
    │       │   ├── UseCases/ProcessGitLabWebhook/
    │       │   └── Workers/OutboxWorker.cs
    │       ├── Infrastructure/GitLab/
    │       └── RunnerModuleExtensions.cs
    └── Submissions/
        └── Runner.Submissions.Module/   ← Домен, CRUD, БД
            ├── Api/                     ← Assignments, Submissions endpoints
            ├── Application/UseCases/    ← CreateAssignment, CreateSubmission, Get*
            ├── Domain/                  ← Entities, Enums
            └── Infrastructure/Database/ ← EF Core, Migrations
```

### Межмодульные зависимости

```
Runner.Runner.Module → Runner.Submissions.Module  (ISubmissionsDbContext, Domain Entities)
Runner.Runner.Module → Runner.Parsers.Module       (INUnitXmlParser)
Все модули           → Runner.SharedKernel
```

---

## 3. Критические пробелы

### 3.1 Runner и Parsers модули не подключены в Program.cs

**Проблема:** `Program.cs` регистрирует только Auth и Submissions модули:
```csharp
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddSubmissionsModule(builder.Configuration);
```

Модули Runner и Parsers (`AddRunnerModule()`, `AddParsersModule()`) **не зарегистрированы**, а их эндпоинты (`MapRunnerModuleEndpoints()`) не подключены. Это означает, что OutboxWorker не запускается, GitLabClient не регистрируется, вебхуки не принимаются, а парсер не доступен через DI.

**Действие:** добавить регистрацию в Program.cs:
```csharp
builder.Services.AddParsersModule();
builder.Services.AddRunnerModule(builder.Configuration);
// ...
app.MapRunnerModuleEndpoints();
```

### 3.2 Миграция для TemplateRepoUrl

Поле `TemplateRepoUrl` добавлено в доменную сущность, конфигурацию EF Core и API, но **миграция не создана**.

**Действие:**
```bash
dotnet ef migrations add AddTemplateRepoUrl \
  --project src/Modules/Submissions/Runner.Submissions.Module \
  --startup-project src/Runner.Api
```

### 3.3 Submissions.Infrastructure содержит пустые папки GitLab/ и NUnit/

После извлечения логики в модули Runner и Parsers в `Submissions/Infrastructure/` остались пустые папки `GitLab/` и `NUnit/`. Также `Submissions/Application/Workers/` пуста.

**Действие:** удалить пустые папки для чистоты структуры.

---

## 4. Незавершённые части

### 4.1 Отсутствует эндпоинт GET /assignments

Сейчас есть **только** `POST /assignments` (создание). Нет возможности:
- Получить список всех заданий (`GET /assignments`)
- Получить одно задание по ID (`GET /assignments/{id}`)

Студенту негде узнать доступные задания и URL шаблонного репозитория.

### 4.2 Отсутствует эндпоинт GET /submissions (список)

Есть только `GET /submissions/{id}` (конкретная отправка). Нет:
- `GET /submissions` — список всех отправок текущего студента
- `GET /submissions?assignmentId=...` — фильтрация по заданию

### 4.3 UserProfile не интегрирован в auth-процесс

Сущность `UserProfile` определена и имеет конфигурацию БД, но:
- При OAuth-логине через GitHub **профиль не создаётся/не обновляется** в БД
- Роль берётся из hard-coded claim в `dev-login`, а не из `UserProfile` в БД
- Нет эндпоинта для управления ролями пользователей

### 4.4 Entity базовый класс не используется

`Runner.SharedKernel.Entity` определён, но **ни одна доменная сущность его не наследует**. Стоит либо унаследовать, либо удалить.

### 4.5 Нет валидации URL репозитория

При создании Submission принимается произвольная строка `GitHubUrl`. Нет проверки формата URL.

### 4.6 Двойной парсинг в ProcessGitLabWebhookHandler

В `HandleSuccessAsync` NUnit XML парсится **дважды** — сначала с временным `tempId`, потом с реальным `checkResult.Id`. Можно оптимизировать.

---

## 5. Пробелы в инфраструктуре

| # | Пробел | Критичность |
|---|---|---|
| 1 | Нет .dockerignore | Средняя |
| 2 | Нет health-check для runner-api | Низкая |
| 3 | HTTPS / reverse proxy не настроен | Низкая (для MVP) |
| 4 | CI/CD для самого проекта Runner | Низкая |

---

## 6. Пробелы в SharedKernel

Для полноценного модульного монолита не хватает:
- Событий домена (domain events) для слабой связи между модулями
- Общих value objects (например, `GitHubUrl`, `BranchName`)

---

## 7. Приоритетный план доработки

### Фаза 1 — Критичные (чтобы система работала)

| # | Задача | Сложность | Файлы |
|---|---|---|---|
| 1 | **Подключить Runner и Parsers модули в Program.cs** | Низкая | `Program.cs` |
| 2 | Создать миграцию для `TemplateRepoUrl` | Низкая | `dotnet ef migrations add` |
| 3 | Удалить пустые папки в Submissions/Infrastructure | Низкая | Папки GitLab/, NUnit/, Workers/ |
| 4 | Добавить .dockerignore | Низкая | Файл в корне |

### Фаза 2 — Важные (функциональность)

| # | Задача | Сложность | Файлы |
|---|---|---|---|
| 5 | Реализовать `GET /assignments` и `GET /assignments/{id}` | Низкая | UseCase + Endpoint |
| 6 | Реализовать `GET /submissions` (список для студента) | Низкая | UseCase + Endpoint |
| 7 | Интегрировать `UserProfile` в OAuth-процесс | Средняя | Auth module + UseCase |
| 8 | Добавить валидацию GitHub URL | Низкая | `Submission.Create()` |
| 9 | Исправить двойной парсинг NUnit XML | Низкая | `ProcessGitLabWebhookHandler` |
| 10 | Наследовать сущности от `Entity` или удалить класс | Низкая | SharedKernel + Domain |

### Фаза 3 — Автоматизированное тестирование

| # | Задача | Сложность | Файлы |
|---|---|---|---|
| 11 | Unit-тесты NUnitXmlParser (модуль Parsers) | Низкая | Новый тестовый проект |
| 12 | Unit-тесты MapErrorType | Низкая | Тестовый проект |
| 13 | Integration-тесты CreateSubmission + Outbox | Средняя | TestContainers |
| 14 | Integration-тесты Webhook processing | Средняя | Тестовый проект |
| 15 | Тесты авторизации (роли, доступ к данным) | Средняя | Тестовый проект |

### Фаза 4 — Развитие (ProTech, масштабирование)

| # | Задача | Сложность | Файлы |
|---|---|---|---|
| 16 | Создать модуль Runner.ProTech.Module | Средняя | Новый модуль |
| 17 | Добавить domain events в SharedKernel | Средняя | SharedKernel + модули |
| 18 | Эндпоинт управления ролями | Низкая | UseCase + Endpoint |
| 19 | CI/CD для проекта Runner | Средняя | .gitlab-ci.yml |
| 20 | Health-check + reverse proxy | Средняя | docker-compose |

---

## 8. Рекомендация

Текущая 4-модульная архитектура хорошо отражает разделение ответственности:
- **Auth** — только аутентификация и авторизация
- **Parsers** — только парсинг результатов (может быть расширен для других форматов)
- **Runner** — оркестрация: отправка заданий в GitLab, приём результатов
- **Submissions** — ядро домена: сущности, хранение, CRUD-операции

**Ближайшие действия:**
1. Подключить Runner и Parsers модули в `Program.cs` (Фаза 1, п. 1)
2. Создать миграцию (Фаза 1, п. 2)
3. Реализовать GET-эндпоинты (Фаза 2, пп. 5-6)
4. ProTech оставить как планируемый модуль
