namespace Re.Application.Interfaces;

public record McpToolDefinition(string Name, string Description, string JsonSchemaInput);
public record McpExecutionResult(bool Success, string OutputJson, string LogSummary, DateTime ExecutedAt);

public interface ISalesforceMcpServerService
{
    Task<IReadOnlyCollection<McpToolDefinition>> GetRegisteredMcpToolsAsync(CancellationToken cancellationToken = default);
    Task<McpExecutionResult> GenerateMetadataWithGeminiAiAsync(string prompt, string targetObjectType = "CustomObject", CancellationToken cancellationToken = default);
    Task<McpExecutionResult> ExecuteMcpToolAsync(string toolName, string inputJson, CancellationToken cancellationToken = default);
}
