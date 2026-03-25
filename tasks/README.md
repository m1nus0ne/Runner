# Тестовые репозитории для проверки пайплайна

## Обзор

Два репозитория для end-to-end тестирования системы автопроверки:

| # | Репозиторий | Платформа | Доступ | Содержимое |
|---|---|---|---|---|
| 1 | `calculator-assignment` | **GitLab** | 🔒 **Private** | `.gitlab-ci.yml` + скрытые NUnit-тесты |
| 2 | `calculator-solution` | **GitHub** | 🌐 **Public** | Решение студента (класс `Calculator`) |

> ⚠️ **Ключевой принцип:** `.gitlab-ci.yml` и `tests/` находятся **только** в приватном GitLab-репозитории.
> Студент **не имеет доступа** к пайплайну и тестам. В его GitHub-репозитории нет ни того, ни другого.

## Задание: Calculator

Студент реализует класс `Calculator` с 4 методами: `Add`, `Subtract`, `Multiply`, `Divide`.
5 скрытых тестов проверяют корректность каждого метода + деление на ноль.

## Разделение ответственности

```
┌─────────────────────────────────────────────────────┐
│  GitLab (приватный)  calculator-assignment           │
│  ┌───────────────┐  ┌────────────────────────────┐  │
│  │ .gitlab-ci.yml│  │ tests/Calculator.Tests/    │  │
│  │  (пайплайн)   │  │  CalculatorTests.cs        │  │
│  │               │  │  Calculator.Tests.csproj   │  │
│  └───────────────┘  └────────────────────────────┘  │
│         ↓ Runner API запускает пайплайн              │
│         ↓ передаёт STUDENT_REPO, BRANCH, ID         │
└─────────────────────────────────────────────────────┘
                         │
                         │ git clone + cp -r tests/
                         ▼
┌─────────────────────────────────────────────────────┐
│  GitHub (публичный)  calculator-solution             │
│  ┌────────────────────────────────────────────────┐ │
│  │ Calculator.slnx                                │ │
│  │ src/Calculator/Calculator.cs   ← код студента  │ │
│  │ src/Calculator/Calculator.csproj               │ │
│  │                                                │ │
│  │ (БЕЗ .gitlab-ci.yml)                          │ │
│  │ (БЕЗ tests/)                                   │ │
│  └────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

## Как это работает в пайплайне

```
1. Runner API вызывает GitLab Pipelines API:
   POST /projects/{calculator-assignment-id}/pipeline
   variables: { STUDENT_REPO, STUDENT_BRANCH, SUBMISSION_ID }

2. GitLab CI запускает .gitlab-ci.yml из приватного репо:
   build stage:
     git clone $STUDENT_REPO student/     ← клонирует код студента с GitHub
     cp -r tests/ student/tests/          ← копирует скрытые тесты из приватного репо
     dotnet restore student/              ← восстанавливает зависимости

3. GitLab CI (test stage):
   dotnet test student/ --logger "nunit;..." ← запускает NUnit тесты
   → TestResult.xml                          ← артефакт с результатами

4. Runner API получает webhook + скачивает TestResult.xml
   → парсит NUnit XML → сохраняет CheckResult
```

## Структура файлов после сборки в CI

```
student/                          ← корень (склонированный GitHub-репо студента)
├── Calculator.slnx               ← солюшен (ссылается на оба проекта)
├── src/
│   └── Calculator/
│       ├── Calculator.csproj
│       └── Calculator.cs         ← код студента
└── tests/                        ← скопировано из приватного GitLab-репозитория
    └── Calculator.Tests/
        ├── Calculator.Tests.csproj  (→ ../../src/Calculator/Calculator.csproj)
        └── CalculatorTests.cs       ← 5 скрытых NUnit-тестов
```

## Задания для агентов

- [01 — GitLab-репозиторий задания (приватный, `.gitlab-ci.yml` + скрытые тесты)](./01-gitlab-calculator-assignment.md)
- [02 — GitHub-репозиторий студента (публичный, только код)](./02-github-calculator-solution.md)


