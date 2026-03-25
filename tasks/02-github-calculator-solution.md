# Задание для агента: GitHub-репозиторий студента «Calculator» (решение)

## Цель

Создать **публичный** GitHub-репозиторий, содержащий **правильное решение** задания «Calculator».
Используется для проверки работоспособности пайплайна автопроверки.

Студент получает этот репозиторий как шаблон (но с заглушками вместо реализации).
Данный репозиторий содержит **полное решение** для end-to-end тестирования.

> ⚠️ **Важно:** в этом репозитории **нет** `.gitlab-ci.yml` и **нет** папки `tests/`.
> Пайплайн и скрытые тесты хранятся в **отдельном приватном GitLab-репозитории**
> (см. [01-gitlab-calculator-assignment.md](./01-gitlab-calculator-assignment.md)).
> Студент не имеет доступа ни к тестам, ни к пайплайну.

---

## Структура репозитория

```
calculator-solution/                ← публичный GitHub-репозиторий
├── Calculator.slnx                 ← солюшен (включает ссылку на тест-проект для CI)
├── README.md                       ← описание задания для студента
└── src/
    └── Calculator/
        ├── Calculator.csproj
        └── Calculator.cs           ← код, который реализует студент
```

> **Нет** `.gitlab-ci.yml` — он находится только в приватном GitLab-репозитории.
> **Нет** `tests/` — скрытые тесты копируются в CI из приватного GitLab-репозитория.
> Файл `.slnx` содержит ссылку на `tests/Calculator.Tests/Calculator.Tests.csproj`,
> который появится **только в CI** после `cp -r tests/ student/tests/`.

---

## Файлы

### `Calculator.slnx`

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Calculator/Calculator.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Calculator.Tests/Calculator.Tests.csproj" />
  </Folder>
</Solution>
```

> **Важно:** ссылка на `tests/Calculator.Tests/Calculator.Tests.csproj` обязательна,
> хотя локально у студента этой папки нет. Она появляется в CI после `cp -r tests/ student/tests/`.
> Без этой ссылки `dotnet test student/` не найдёт тестовый проект.

---

### `src/Calculator/Calculator.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

---

### `src/Calculator/Calculator.cs` (полное решение)

```csharp
namespace Calculator;

public class Calculator
{
    public double Add(double a, double b) => a + b;

    public double Subtract(double a, double b) => a - b;

    public double Multiply(double a, double b) => a * b;

    public double Divide(double a, double b)
    {
        if (b == 0)
            throw new DivideByZeroException("Cannot divide by zero.");
        return a / b;
    }
}
```

---

### `README.md`

```markdown
# Calculator

Реализуйте класс `Calculator` в файле `src/Calculator/Calculator.cs`.

## Требования

Класс `Calculator` должен содержать 4 метода:

| Метод | Сигнатура | Описание |
|---|---|---|
| `Add` | `double Add(double a, double b)` | Возвращает сумму `a + b` |
| `Subtract` | `double Subtract(double a, double b)` | Возвращает разность `a - b` |
| `Multiply` | `double Multiply(double a, double b)` | Возвращает произведение `a * b` |
| `Divide` | `double Divide(double a, double b)` | Возвращает частное `a / b`. При `b == 0` выбрасывает `DivideByZeroException` |

## Как работать

1. Откройте `src/Calculator/Calculator.cs`
2. Реализуйте все 4 метода
3. Для локальной проверки: `dotnet build src/Calculator/`
4. Отправьте ссылку на этот репозиторий через систему автопроверки

> **Примечание:** папка `tests/` отсутствует — скрытые тесты запускаются автоматически в CI.
> Не удаляйте и не переименовывайте файл `Calculator.slnx`.
```

---

## Дополнительно: шаблон-заглушка для студента

Если нужно выдать студенту шаблон **без решения**, замените `Calculator.cs` на:

```csharp
namespace Calculator;

public class Calculator
{
    public double Add(double a, double b) => throw new NotImplementedException();

    public double Subtract(double a, double b) => throw new NotImplementedException();

    public double Multiply(double a, double b) => throw new NotImplementedException();

    public double Divide(double a, double b) => throw new NotImplementedException();
}
```

При этом все 5 тестов упадут с `NotImplementedException` → в системе автопроверки статус будет `Failed`.

---

## Проверка

1. `dotnet build src/Calculator/` — собирается без ошибок
2. После подстановки тестов (`cp -r tests/ .` из GitLab-репозитория):
   - `dotnet restore` — восстанавливает оба проекта
   - `dotnet test` — все 5 тестов проходят ✅
3. Структура путей совпадает с ожиданиями `.gitlab-ci.yml`:
   - Код студента: `src/Calculator/Calculator.csproj`
   - Тесты CI: `tests/Calculator.Tests/Calculator.Tests.csproj`
   - Ссылка из тестов на код: `../../src/Calculator/Calculator.csproj`


