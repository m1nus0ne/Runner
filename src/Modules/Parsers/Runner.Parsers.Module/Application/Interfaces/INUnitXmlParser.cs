using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Parsers.Module.Application.Interfaces;

/// <summary>Парсер результатов тестирования.</summary>
public interface INUnitXmlParser
{
    /// <summary>Парсит NUnit XML (TestResult.xml) и возвращает результаты по группам тестов.</summary>
    IReadOnlyList<TestGroupResult> Parse(Guid checkResultId, string rawXml);
}

