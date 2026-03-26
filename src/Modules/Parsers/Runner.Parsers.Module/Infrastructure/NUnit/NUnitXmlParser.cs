using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Runner.Parsers.Module.Application.Interfaces;
using Runner.Submissions.Module.Domain.Entities;
using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Parsers.Module.Infrastructure.NUnit;

/// <summary>
/// Парсер NUnit 3 XML (TestResult.xml).
/// Структура: &lt;test-run&gt; → &lt;test-suite type="TestFixture"&gt; → &lt;test-case&gt;
/// </summary>
internal sealed partial class NUnitXmlParser : INUnitXmlParser
{
    public IReadOnlyList<TestGroupResult> Parse(Guid checkResultId, string rawXml)
    {
        var doc = XDocument.Parse(rawXml);
        var results = new List<TestGroupResult>();

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

            var failedCases = testCases
                .Where(tc => (string?)tc.Attribute("result") is "Failed" or "Error")
                .ToList();

            ErrorType? errorType = null;
            string? errorMessage = null;

            if (failedCases.Count > 0)
            {
                // Структурированные данные по каждому упавшему тесту → JSON
                var failedTestDetails = new List<FailedTestDetail>();

                foreach (var tc in failedCases)
                {
                    var testName = (string?)tc.Attribute("name") ?? "?";
                    var failure = tc.Element("failure");
                    var rawMessage = (string?)failure?.Element("message") ?? string.Empty;

                    var expectedMatch = ExpectedRegex().Match(rawMessage);
                    var actualMatch   = ButWasRegex().Match(rawMessage);

                    failedTestDetails.Add(new FailedTestDetail
                    {
                        TestName = testName,
                        Message  = rawMessage.Trim(),
                        Expected = expectedMatch.Success ? expectedMatch.Groups[1].Value.Trim() : null,
                        Actual   = actualMatch.Success ? actualMatch.Groups[1].Value.Trim() : null
                    });
                }

                errorMessage = JsonSerializer.Serialize(failedTestDetails,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                // Классификация по первой ошибке
                var firstMessage   = (string?)failedCases[0].Element("failure")?.Element("message") ?? string.Empty;
                var firstClassName = (string?)failedCases[0].Attribute("classname") ?? string.Empty;
                errorType = MapErrorType(firstClassName, firstMessage);
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

    [GeneratedRegex(@"Expected:\s*(.+?)(?:\r?\n|$)", RegexOptions.Singleline)]
    private static partial Regex ExpectedRegex();

    [GeneratedRegex(@"But was:\s*(.+?)(?:\r?\n|$)", RegexOptions.Singleline)]
    private static partial Regex ButWasRegex();

    /// <summary>Структура одного упавшего теста для сериализации в JSON.</summary>
    private sealed class FailedTestDetail
    {
        public string TestName { get; init; } = "";
        public string Message  { get; init; } = "";
        public string? Expected { get; init; }
        public string? Actual   { get; init; }
    }
}

