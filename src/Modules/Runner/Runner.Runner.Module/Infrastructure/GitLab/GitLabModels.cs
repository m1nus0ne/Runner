using System.Text.Json.Serialization;

namespace Runner.Runner.Module.Infrastructure.GitLab;

internal sealed record TriggerPipelineRequest(
    [property: JsonPropertyName("ref")] string Ref,
    [property: JsonPropertyName("variables")] List<PipelineVariable> Variables);

internal sealed record PipelineVariable(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("value")] string Value);

internal sealed record TriggerPipelineResponse(
    [property: JsonPropertyName("id")] long Id);

internal sealed record GitLabWebhookPayload(
    [property: JsonPropertyName("object_kind")] string ObjectKind,
    [property: JsonPropertyName("object_attributes")] PipelineAttributes ObjectAttributes,
    [property: JsonPropertyName("project")] GitLabProject? Project,
    [property: JsonPropertyName("builds")] List<BuildInfo>? Builds);

internal sealed record GitLabProject(
    [property: JsonPropertyName("id")] long Id);

internal sealed record PipelineAttributes(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("variables")] List<PipelineVariable>? Variables);

internal sealed record BuildInfo(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("stage")] string Stage);

