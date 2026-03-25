using System.Xml.Linq;
using Runner.Parsers.Module.Application.Interfaces;
using Runner.Submissions.Module.Domain.Entities;
using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Parsers.Module.Infrastructure.NUnit;

/// <summary>
/// Парсер NUnit 3 XML (TestResult.xml).
/// Структура: &lt;test-run&gt; → &lt;test-suite type="TestFixture"&gt; → &lt;test-case&gt;
/// </summary>
internal sealed class NUnitXmlParser : INUnitXmlParser
{
    public IReadOnlyList<TestGroupResult> Parse(Guid checkResultId, string rawXml)
    {
        var doc = XDocument.Parse(rawXml);
        var results = new List<TestGroupResult>();

        // Обходим все TestFixture-наборы как группы
        var fixtures = doc.Descendants("test-suite")
            .Where(e => (string?)e.Attribute("type") == "TestFixture");

        foreach (var fixture in fixtures)
        {
            var groupName = (string?)fixture.Attribute("fullname")
                         ?? (string?)fixture.Attribute("name")
                         ?? "Unknown";

            var testCases = fixture.Descendants("test-case").ToList();
            int passed = testCases.Count(tc => (string?)tc.Attribute("result") == "Passed");
            int failed = testCases.Count(tc => (string?)tc.Attribute("result") is "Failed" or "Error");

            // Берём первую ошибку для классификации
            var firstFailure = fixture.Descendants("failure").FirstOrDefault();
            var firstError   = fixture.Descendants("test-case")
                .FirstOrDefault(tc => (string?)tc.Attribute("result") is "Failed" or "Error");

            ErrorType? errorType = null;
            string? errorMessage = null;

            if (firstFailure is not null)
            {
                errorMessage = (string?)firstFailure.Element("message") ?? string.Empty;
                var exType   = (string?)firstError?.Attribute("classname") ?? string.Empty;
                errorType = MapErrorType(exType, errorMessage);
            }

            results.Add(TestGroupResult.Create(
                checkResultId, groupName, passed, failed, errorType, errorMessage));
        }

        return results;
    }

    private static ErrorType MapErrorType(string exceptionType, string? message)
    {
        if (exceptionType.Contains("CompilationError", StringComparison.OrdinalIgnoreCase))
            return ErrorType.CompilationError;

        if (exceptionType.Contains("NotImplementedException", StringComparison.OrdinalIgnoreCase))
            return ErrorType.InterfaceNotFound;

        if (exceptionType.Contains("TaskCanceledException", StringComparison.OrdinalIgnoreCase)
         || exceptionType.Contains("TimeoutException", StringComparison.OrdinalIgnoreCase))
            return ErrorType.Timeout;

        if (message is not null
         && (message.Contains("coverage", StringComparison.OrdinalIgnoreCase)
          || message.Contains("CoverageBelow", StringComparison.OrdinalIgnoreCase)))
            return ErrorType.CoverageBelow;

        return ErrorType.AssertionFailed;
    }
}

