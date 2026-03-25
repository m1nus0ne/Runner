# Задание для агента: GitLab-репозиторий задания «Calculator»

## Цель

Создать **приватный** GitLab-репозиторий задания. Он содержит:
- `.gitlab-ci.yml` — пайплайн автопроверки (находится **только** здесь, студент его не видит)
- `tests/` — скрытые модульные NUnit-тесты (студент **не имеет доступа** к этому репозиторию)

Этот репозиторий **не содержит** код студента. Пайплайн сам клонирует код студента по URL и подставляет скрытые тесты.

> ⚠️ **Приватность:** репозиторий должен быть `Private` в GitLab. Студент не должен иметь к нему доступа.
> Runner API запускает пайплайн через GitLab Pipelines API с сервисным токеном.

---

## Структура репозитория

```
calculator-assignment/              ← приватный GitLab-репозиторий
├── .gitlab-ci.yml                  ← пайплайн (ТОЛЬКО здесь, не у студента)
└── tests/                          ← скрытые тесты (копируются в CI в код студента)
    └── Calculator.Tests/
        ├── Calculator.Tests.csproj
        └── CalculatorTests.cs
```

---

## Настройка репозитория в GitLab

1. Создать проект: `Settings → General → Visibility = Private`
2. Запомнить **Project ID** (число) — его нужно указать при создании `Assignment` в Runner API
3. CI/CD переменные (`Settings → CI/CD → Variables`) задавать **не нужно** — Runner API передаёт
   `STUDENT_REPO`, `STUDENT_BRANCH`, `SUBMISSION_ID` при запуске пайплайна через API
4. Убедиться, что для проекта зарегистрирован GitLab Runner (shared или specific)

---

## Файлы

### `.gitlab-ci.yml`

> Этот файл находится **только** в приватном GitLab-репозитории. Студент его не видит и не может изменить.

Скопировать дословно (совместим с Runner API):

```yaml
image: mcr.microsoft.com/dotnet/sdk:10.0

variables:
  STUDENT_REPO:   ""
  STUDENT_BRANCH: "main"
  SUBMISSION_ID:  ""
  DOTNET_NOLOGO:  "true"
  DOTNET_CLI_TELEMETRY_OPTOUT: "true"

stages:
  - build
  - test

build:
  stage: build
  script:
    - git clone --branch "$STUDENT_BRANCH" --depth 1 "$STUDENT_REPO" student
    - cp -r tests/ student/tests/
    - dotnet restore student/
  artifacts:
    paths:
      - student/
    expire_in: 1 hour
  timeout: 3 minutes

test:
  stage: test
  needs: [build]
  script:
    - dotnet test student/tests/Calculator.Tests/Calculator.Tests.csproj --verbosity normal --logger "nunit;LogFilePath=$CI_PROJECT_DIR/TestResult.xml" -- NUnit.DefaultTestNamePattern="{m}"
  artifacts:
    when: always
    reports:
      junit: TestResult.xml
    paths:
      - TestResult.xml
    expire_in: 7 days
  timeout: 5 minutes
  variables:
    GIT_STRATEGY: none
```

---

### `tests/Calculator.Tests/Calculator.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="4.3.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
    <PackageReference Include="NunitXml.TestLogger" Version="4.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Calculator/Calculator.csproj" />
  </ItemGroup>

</Project>
```

> **Важно:** `ProjectReference` ссылается на `../../src/Calculator/Calculator.csproj` — это путь относительно `student/tests/Calculator.Tests/` до `student/src/Calculator/Calculator.csproj` после того, как пайплайн склонирует код студента и скопирует тесты.

---

### `tests/Calculator.Tests/CalculatorTests.cs`

5 минимальных тестов, проверяющих все 4 операции + деление на ноль:

```csharp
using NUnit.Framework;
using Calc = global::Calculator.Calculator;

namespace Calculator.Tests;

[TestFixture]
public class CalculatorTests
{
    private Calc _calc = null!;

    [SetUp]
    public void SetUp() => _calc = new Calc();

    [Test]
    public void Add_ReturnsSumOfTwoNumbers()
    {
        Assert.That(_calc.Add(2, 3), Is.EqualTo(5));
    }

    [Test]
    public void Subtract_ReturnsDifference()
    {
        Assert.That(_calc.Subtract(10, 4), Is.EqualTo(6));
    }

    [Test]
    public void Multiply_ReturnsProduct()
    {
        Assert.That(_calc.Multiply(3, 7), Is.EqualTo(21));
    }

    [Test]
    public void Divide_ReturnsQuotient()
    {
        Assert.That(_calc.Divide(20, 4), Is.EqualTo(5));
    }

    [Test]
    public void Divide_ByZero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => _calc.Divide(1, 0));
    }
}
```

---

## Безопасность

- Репозиторий **приватный** — студент не имеет к нему доступа ни в каком виде
- `.gitlab-ci.yml` **не дублируется** в репозиторий студента — он живёт только здесь
- Тесты из `tests/` **не раскрываются** студенту — он видит только результаты (passed/failed)
- Runner API запускает пайплайн сервисным токеном (`GitLab:ServiceToken`) через `POST /api/v4/projects/{id}/pipeline`

---

## Проверка

После создания репозитория убедиться:
1. Репозиторий имеет visibility = **Private** в GitLab
2. В корне есть `.gitlab-ci.yml` и папка `tests/`
3. Тест-проект компилируется (если подставить любой `Calculator.csproj` с классом `Calculator`)
4. Путь `ProjectReference` из тестов ведёт на `../../src/Calculator/Calculator.csproj`
5. NUnit-логгер (`NunitXml.TestLogger`) указан в зависимостях — он нужен для `--logger "nunit;..."` в пайплайне
6. Студент **не имеет доступа** к репозиторию (ни как Reporter, ни как Guest)








