# Инфографика и схемы системы Runner

> Все диаграммы выполнены в формате Mermaid.
> Для рендеринга используйте любой Mermaid-совместимый просмотрщик
> (GitHub, GitLab, плагины IDE, https://mermaid.live и др.).
>
> Нумерация рисунков соответствует ссылкам в файле `report.md`.

---

## Рисунок 1. Общая архитектура системы

```mermaid
graph TB
    subgraph Пользователи
        S[Студент]
        A[Администратор]
    end

    subgraph "Runner API (Docker)"
        API["ASP.NET Core 10<br/>Modular Monolith"]
        subgraph "Модуль Auth"
            AUTH_EP["API: /auth/*"]
            AUTH_CFG["GitHub OAuth 2.0<br/>Cookie Auth<br/>Политики авторизации"]
        end
        subgraph "Модуль Submissions"
            EP_A["API: /assignments"]
            EP_S["API: /submissions"]
            SUB_UC["Use Cases<br/>(CRUD)"]
            SUB_DB["SubmissionsDbContext<br/>(EF Core)"]
        end
        subgraph "Модуль Runner"
            EP_W["API: /webhooks/gitlab"]
            OW["OutboxWorker<br/>(BackgroundService)"]
            GLC["GitLabClient"]
            R_UC["ProcessGitLabWebhook"]
        end
        subgraph "Модуль Parsers"
            NUP["NUnitXmlParser<br/>Парсинг TestResult.xml"]
        end
        subgraph "SharedKernel"
            SK["Базовые типы,<br/>исключения, контракты"]
        end
    end

    subgraph "PostgreSQL 17 (Docker)"
        DB[("База данных runner<br/>Схема: submissions")]
    end

    subgraph "GitHub"
        GH_OAuth["GitHub OAuth 2.0"]
        GH_Repo["Репозиторий<br/>студента"]
        GH_Template["Шаблонный<br/>репозиторий<br/>(публичный)"]
    end

    subgraph "Self-hosted GitLab (Proxmox)"
        GL_API["GitLab API"]
        GL_CI["GitLab CI/CD"]
        GL_Repo["Приватный репозиторий<br/>с тестами"]
        GL_Runner["GitLab Runner<br/>(Docker Executor)"]
    end

    S -->|"Форк шаблона"| GH_Template
    S -->|"HTTP API"| EP_S
    A -->|"HTTP API"| EP_A
    S -->|"OAuth 2.0"| GH_OAuth
    A -->|"OAuth 2.0"| GH_OAuth
    GH_OAuth -->|"Аутентификация"| AUTH_CFG

    SUB_UC -->|"EF Core / Npgsql"| DB
    OW -->|"Чтение Outbox"| DB
    OW -->|"POST /pipeline"| GL_API
    R_UC -->|"GET /artifacts"| GL_API
    R_UC -->|"Парсинг XML"| NUP

    GL_API -->|"Запуск пайплайна"| GL_CI
    GL_CI -->|"Docker Executor"| GL_Runner
    GL_Runner -->|"git clone"| GH_Repo
    GL_Runner -->|"Скрытые тесты"| GL_Repo
    GL_CI -->|"Webhook (POST)"| EP_W
```

---

## Рисунок 2. Схема базы данных (ER-диаграмма)

```mermaid
erDiagram
    assignments {
        uuid Id PK
        varchar Title
        bigint GitLabProjectId
        int Type "Algorithm=0, Endpoint=1, Coverage=2"
        int CoverageThreshold "nullable"
        varchar TemplateRepoUrl "nullable, публичный шаблон"
    }

    submissions {
        uuid Id PK
        varchar StudentId
        uuid AssignmentId FK
        varchar GitHubUrl
        varchar Branch
        bigint GitLabPipelineId "nullable"
        int Status "Pending...Timeout"
        timestamp CreatedAt
    }

    check_results {
        uuid Id PK
        uuid SubmissionId FK "unique"
        int TotalTests
        int PassedTests
        int FailedTests
        text RawNUnitXml
    }

    test_group_results {
        uuid Id PK
        uuid CheckResultId FK
        varchar GroupName
        int Passed
        int Failed
        int ErrorType "nullable"
        text ErrorMessage "nullable"
    }

    outbox_messages {
        uuid Id PK
        text Payload "JSON: PipelineTriggerPayload"
        timestamp CreatedAt
        timestamp ProcessedAt "nullable"
        int RetryCount
        text Error "nullable"
    }

    user_profiles {
        uuid Id PK
        varchar GitHubLogin
        bigint GitHubId
        int Role "Admin=0, Student=1"
    }

    assignments ||--o{ submissions : "1 → *"
    submissions ||--o| check_results : "1 → 0..1"
    check_results ||--o{ test_group_results : "1 → *"
```

---

## Рисунок 3. Жизненный цикл отправки (Sequence Diagram)

```mermaid
sequenceDiagram
    actor Студент
    participant AuthMod as Auth Module
    participant SubAPI as Submissions API
    participant DB as PostgreSQL
    participant OW as Runner: OutboxWorker
    participant GLC as Runner: GitLabClient
    participant GL as GitLab API
    participant CI as GitLab CI/Runner
    participant Repo as GitHub Repo
    participant WH as Runner: WebhookEndpoints
    participant Parser as Parsers: NUnitXmlParser

    Студент->>AuthMod: GET /auth/login (GitHub OAuth)
    AuthMod-->>Студент: Cookie сессия

    Студент->>SubAPI: POST /submissions<br/>{assignmentId, gitHubUrl, branch}
    SubAPI->>DB: BEGIN TRANSACTION
    SubAPI->>DB: INSERT Submission (status=Pending)
    SubAPI->>DB: INSERT OutboxMessage (payload JSON)
    SubAPI->>DB: COMMIT
    SubAPI-->>Студент: 202 Accepted {id}

    loop Каждые 5 секунд
        OW->>DB: SELECT необработанные OutboxMessages
        OW->>GLC: TriggerPipelineAsync()
        GLC->>GL: POST /api/v4/projects/{id}/pipeline<br/>variables: STUDENT_REPO, STUDENT_BRANCH, SUBMISSION_ID
        GL-->>GLC: {pipeline_id}
        GLC-->>OW: pipeline_id
        OW->>DB: UPDATE Submission (status=Triggered, pipelineId)
        OW->>DB: UPDATE OutboxMessage (processedAt=now)
    end

    CI->>Repo: git clone (ветка студента)
    CI->>CI: Сборка + запуск NUnit-тестов
    CI->>CI: Формирование TestResult.xml

    CI->>WH: POST /webhooks/gitlab<br/>X-Gitlab-Token: secret
    WH->>WH: Валидация токена + извлечение SUBMISSION_ID
    WH->>GLC: DownloadNUnitArtifactAsync()
    GLC->>GL: GET /api/v4/projects/{id}/jobs/{jobId}/artifacts/TestResult.xml
    GL-->>GLC: XML-файл
    GLC-->>WH: rawXml
    WH->>Parser: Parse(checkResultId, rawXml)
    Parser-->>WH: List«TestGroupResult»
    WH->>DB: INSERT CheckResult + TestGroupResults
    WH->>DB: UPDATE Submission (status=Passed|Failed)

    Студент->>SubAPI: GET /submissions/{id}/report
    SubAPI->>DB: SELECT CheckResult + TestGroupResults
    SubAPI-->>Студент: Детализированный отчёт
```

---

## Рисунок 4. Диаграмма состояний отправки (State Machine)

```mermaid
stateDiagram-v2
    [*] --> Pending : Студент отправляет решение

    Pending --> Triggered : OutboxWorker запустил пайплайн
    Pending --> Error : Исчерпаны попытки (5 retry)

    Triggered --> Running : Пайплайн начал выполнение

    Running --> Passed : Все тесты пройдены
    Running --> Failed : Есть проваленные тесты
    Running --> Error : Ошибка сборки / пайплайна
    Running --> Timeout : Пайплайн отменён (canceled)

    Triggered --> Passed : Webhook: success
    Triggered --> Failed : Webhook: success + failed tests
    Triggered --> Error : Webhook: failed
    Triggered --> Timeout : Webhook: canceled

    Passed --> [*]
    Failed --> [*]
    Error --> [*]
    Timeout --> [*]
```

---

## Рисунок 5. Структура модулей (Modular Monolith)

```mermaid
graph LR
    subgraph "Runner.Api (Host)"
        Program["Program.cs<br/>Регистрация модулей,<br/>Middleware, Endpoints"]
    end

    subgraph "Runner.SharedKernel"
        Entity["Entity (базовый тип)"]
        Exceptions["NotFoundException<br/>ForbiddenException"]
        Payload["PipelineTriggerPayload"]
    end

    subgraph "Runner.Auth.Module"
        direction TB
        subgraph "Auth / Api"
            AuthEP["AuthEndpoints<br/>/auth/login<br/>/auth/dev-login<br/>/auth/logout<br/>/auth/me"]
        end
        AuthExt["AuthModuleExtensions<br/>GitHub OAuth 2.0<br/>Cookie Auth<br/>Policies: AdminOnly, StudentOnly"]
    end

    subgraph "Runner.Parsers.Module"
        direction TB
        subgraph "Parsers / Application"
            INP["INUnitXmlParser"]
        end
        subgraph "Parsers / Infrastructure"
            NUP["NUnitXmlParser<br/>Парсинг NUnit 3 XML<br/>Классификация ошибок"]
        end
        ParsersExt["ParsersModuleExtensions"]
    end

    subgraph "Runner.Runner.Module"
        direction TB
        subgraph "Runner / Api"
            WE["WebhookEndpoints<br/>/webhooks/gitlab"]
        end
        subgraph "Runner / Application"
            direction TB
            subgraph "Runner / UseCases"
                PW["ProcessGitLabWebhook<br/>Handler + Command"]
            end
            subgraph "Runner / Interfaces"
                IGL["IGitLabClient"]
            end
            subgraph "Runner / Workers"
                OBW["OutboxWorker<br/>(BackgroundService)"]
                OBOpt["OutboxOptions"]
            end
        end
        subgraph "Runner / Infrastructure"
            GLC["GitLabClient"]
            GLM["GitLabModels<br/>(Webhook Payload,<br/>Pipeline Request/Response)"]
            GLO["GitLabOptions"]
        end
        RunnerExt["RunnerModuleExtensions"]
    end

    subgraph "Runner.Submissions.Module"
        direction TB
        subgraph "Submissions / Api"
            AE["AssignmentsEndpoints<br/>/assignments"]
            SE["SubmissionsEndpoints<br/>/submissions"]
        end
        subgraph "Submissions / Application"
            direction TB
            subgraph "Submissions / UseCases"
                CA["CreateAssignment"]
                CS["CreateSubmission"]
                GS["GetSubmission"]
                GSR["GetSubmissionReport"]
            end
            subgraph "Submissions / Interfaces"
                IDB["ISubmissionsDbContext"]
            end
        end
        subgraph "Submissions / Domain"
            subgraph "Entities"
                Asgn["Assignment"]
                Sub["Submission"]
                CR["CheckResult"]
                TGR["TestGroupResult"]
                OM["OutboxMessage"]
                UP["UserProfile"]
            end
            subgraph "Enums"
                AT["AssignmentType"]
                SS["SubmissionStatus"]
                ET["ErrorType"]
                UR["UserRole"]
            end
        end
        subgraph "Submissions / Infrastructure"
            DBC["SubmissionsDbContext"]
            CFG["EF Configurations"]
            MIG["Migrations"]
        end
        SubExt["SubmissionsModuleExtensions"]
    end

    %% Host → Modules
    Program --> AuthExt
    Program --> ParsersExt
    Program --> RunnerExt
    Program --> SubExt
    Program --> Entity
    Program --> Exceptions

    %% Auth — endpoints
    AuthExt --> AuthEP

    %% Submissions — endpoints → use cases
    AE --> CA
    SE --> CS
    SE --> GS
    SE --> GSR

    %% Submissions — use cases → interfaces
    CA --> IDB
    CS --> IDB
    GS --> IDB
    GSR --> IDB

    %% Runner — endpoints → use cases
    WE --> PW

    %% Runner — use cases → interfaces (cross-module)
    PW --> IGL
    PW --> INP
    PW --> IDB
    OBW --> IGL
    OBW --> IDB

    %% Infrastructure implementations
    IDB -.->|"реализация"| DBC
    IGL -.->|"реализация"| GLC
    INP -.->|"реализация"| NUP
```

---

## Рисунок 6. Инфраструктура развёртывания (Docker Compose + Proxmox)

```mermaid
graph TB
    subgraph "Кластер Proxmox"
        subgraph "VM 1 — Runner"
            subgraph "Docker Compose"
                RC["runner-api<br/>ASP.NET Core 10<br/>:8080"]
                PG["postgres:17-alpine<br/>PostgreSQL"]
            end
            RC -->|"EF Core"| PG
            NET["Docker Network: internal"]
        end

        subgraph "VM 2 — GitLab"
            GL["Self-hosted GitLab<br/>CE"]
        end

        subgraph "VM 3 — GitLab Runner"
            GLR["GitLab Runner<br/>Docker Executor"]
        end
    end

    LAN["Локальная сеть"] -->|"HTTP :8080"| RC
    RC -->|"HTTP API"| GL
    GL -->|"Webhook"| RC
    GL -->|"Задачи CI"| GLR
    GLR -->|"git clone"| GH["GitHub.com"]
```

---

## Рисунок 7. Алгоритм классификации ошибок (Parsers Module)

```mermaid
flowchart TD
    Start["Получен элемент<br/>&lt;failure&gt; из NUnit XML"] --> A{Тип исключения содержит<br/>'CompilationError'?}
    A -->|Да| CE["CompilationError<br/>(Ошибка компиляции)"]
    A -->|Нет| B{Тип исключения содержит<br/>'NotImplementedException'?}
    B -->|Да| INF["InterfaceNotFound<br/>(Отсутствие реализации)"]
    B -->|Нет| C{Тип исключения содержит<br/>'TaskCanceledException' или<br/>'TimeoutException'?}
    C -->|Да| TO["Timeout<br/>(Превышение времени)"]
    C -->|Нет| D{Сообщение содержит<br/>'coverage' или<br/>'CoverageBelow'?}
    D -->|Да| CB["CoverageBelow<br/>(Недостаточное покрытие)"]
    D -->|Нет| AF["AssertionFailed<br/>(Ошибка утверждения)"]
```

---

## Рисунок 8. Карта API-эндпоинтов

```mermaid
graph LR
    subgraph "Auth Module: /auth/*"
        L["GET /auth/login<br/>🔓 Anonymous<br/>GitHub OAuth"]
        DL["GET /auth/dev-login<br/>🔓 Anonymous<br/>(только dev)"]
        LO["GET /auth/logout<br/>🔒 Authorized"]
        ME["GET /auth/me<br/>🔒 Authorized<br/>+ profileUrl"]
        GR["GET /auth/github/repos<br/>🔒 Authorized<br/>Публичные репозитории"]
        GB["GET /auth/github/repos<br/>/{owner}/{repo}/branches<br/>🔒 Authorized<br/>Ветки репозитория"]
    end

    subgraph "Submissions Module: /assignments"
        CA2["POST /assignments<br/>🔑 AdminOnly"]
    end

    subgraph "Submissions Module: /submissions"
        CS2["POST /submissions<br/>🔑 StudentOnly"]
        GS2["GET /submissions/{id}<br/>🔒 Authorized"]
        GR2["GET /submissions/{id}/report<br/>🔒 Authorized"]
    end

    subgraph "Runner Module: /webhooks"
        WH["POST /webhooks/gitlab<br/>🔓 Anonymous<br/>(X-Gitlab-Token)"]
    end
```

---

## Рисунок 9. Межмодульные зависимости

```mermaid
graph TD
    SK["Runner.SharedKernel<br/>(Entity, Exceptions,<br/>PipelineTriggerPayload)"]

    AUTH["Runner.Auth.Module<br/>(OAuth, Endpoints)"]
    PARSERS["Runner.Parsers.Module<br/>(INUnitXmlParser,<br/>NUnitXmlParser)"]
    SUB["Runner.Submissions.Module<br/>(Domain, DbContext,<br/>CRUD Use Cases, API)"]
    RUN["Runner.Runner.Module<br/>(OutboxWorker, Webhook,<br/>GitLabClient)"]
    HOST["Runner.Api (Host)"]

    HOST -->|"AddAuthModule()"| AUTH
    HOST -->|"AddParsersModule()"| PARSERS
    HOST -->|"AddRunnerModule()"| RUN
    HOST -->|"AddSubmissionsModule()"| SUB

    AUTH -->|"SharedKernel"| SK
    SUB -->|"SharedKernel"| SK
    RUN -->|"SharedKernel"| SK
    PARSERS -->|"SharedKernel"| SK

    RUN -->|"ISubmissionsDbContext"| SUB
    RUN -->|"Domain Entities"| SUB
    RUN -->|"INUnitXmlParser"| PARSERS

    style HOST fill:#4a90d9,color:#fff
    style SK fill:#e8e8e8,color:#333
    style AUTH fill:#f5a623,color:#fff
    style PARSERS fill:#7ed321,color:#fff
    style SUB fill:#bd10e0,color:#fff
    style RUN fill:#d0021b,color:#fff
```

---

## Рисунок 10. Схема взаимодействия репозиториев (шаблонный и тестовый)

```mermaid
graph TB
    subgraph "Администратор (составитель задания)"
        A["Создаёт задание<br/>в Runner API"]
    end

    subgraph "GitHub (публичный)"
        TR["Шаблонный репозиторий<br/>(Template Repository)<br/>🔓 Открытый"]
        SR["Репозиторий студента<br/>(Fork / Clone шаблона)<br/>🔓 Открытый"]
    end

    subgraph "Self-hosted GitLab (приватный)"
        TEST["Репозиторий с тестами<br/>+ .gitlab-ci.yml<br/>🔒 Приватный"]
    end

    subgraph "Runner API"
        ASGN["Assignment<br/>GitLabProjectId → TEST<br/>TemplateRepoUrl → TR"]
    end

    subgraph "GitLab CI/Runner"
        PIPE["Пайплайн тестирования"]
    end

    A -->|"1. Создаёт шаблон"| TR
    A -->|"2. Создаёт тесты + CI"| TEST
    A -->|"3. POST /assignments<br/>{gitLabProjectId, templateRepoUrl}"| ASGN

    TR -->|"4. Студент создаёт<br/>свой репозиторий"| SR

    SR -->|"5. POST /submissions<br/>{gitHubUrl, branch}"| ASGN

    ASGN -->|"6. OutboxWorker<br/>запускает пайплайн"| PIPE
    TEST -->|"Скрытые тесты"| PIPE
    SR -->|"git clone"| PIPE

    PIPE -->|"7. Webhook →<br/>Runner Module"| ASGN
```

---

## Рисунок 11. Схема интеграции с платформой ProTech (планируемая)

```mermaid
sequenceDiagram
    participant PT as ProTech (Moodle)
    participant API as Runner API
    participant DB as PostgreSQL
    participant GL as GitLab CI

    Note over PT,GL: Планируемая интеграция

    PT->>API: POST /submissions<br/>(от имени студента через API-ключ)
    API->>DB: Создание отправки + Outbox
    API-->>PT: 202 Accepted {submissionId}

    Note over API,GL: Цикл проверки (существующий)

    API->>GL: Запуск пайплайна
    GL-->>API: Webhook + артефакты
    API->>DB: Сохранение результатов

    PT->>API: GET /submissions/{id}/report
    API-->>PT: Результаты проверки

    PT->>PT: Проставка оценки<br/>через Moodle Gradebook API
```
