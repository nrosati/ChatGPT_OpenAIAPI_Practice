using OpenAI.Chat;
using System.Text.Json;

namespace Official.Services;

#region [01] Interface for Tool Handler
public interface IToolHandler
{
    string Name { get; }
    string Description { get; }
    BinaryData GetParametersSchema();
    Task<string> InvokeAsync(JsonElement parameters);
}
#endregion

#region [02] Tool Registry 
public class ToolRegistry
{
    private readonly Dictionary<string, IToolHandler> _handlers;

    public ToolRegistry(IEnumerable<IToolHandler> handlers)
        => _handlers = handlers.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ChatTool> GetAllTools() =>
        _handlers.Values.Select(h =>
            ChatTool.CreateFunctionTool(
                functionName: h.Name,
                functionDescription: h.Description,
                functionParameters: h.GetParametersSchema()
            ));

    public bool TryGetHandler(string name, out IToolHandler handler)
        => _handlers.TryGetValue(name, out handler);

    public Task<string> InvokeAsync(string toolName, JsonElement parameters)
        => TryGetHandler(toolName, out var handler)
            ? handler.InvokeAsync(parameters)
            : Task.FromResult($"Tool \"{toolName}\" is not registered.");
}

#endregion