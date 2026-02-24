# Unity MCP Integration - Agent Guide

## Project Overview

Unity implementation of the Model Context Protocol (MCP) server, enabling AI assistants to control Unity Editor and runtime applications. Built on Unity 2021.3.45f1, C# 9.0, .NET Standard 2.1.

## Build Commands

### Compile Check (Unity Batch Mode)
```bash
/Applications/Unity/Hub/Editor/2021.3.45f1c2/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath . -quit -logFile -
```

### Solution File
- `UnityMCPIntergrate.sln` - Main solution (regenerate via Assets > Open C# Project)

## Test Commands

### Run All Tests
```bash
/Applications/Unity/Hub/Editor/2021.3.45f1c2/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults ./TestResults.xml \
  -quit -logFile -
```

### Run Single Test (Filter by Name)
```bash
/Applications/Unity/Hub/Editor/2021.3.45f1c2/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath . \
  -runTests -testPlatform EditMode \
  -testFilter "YourTestClass.YourTestMethod" \
  -testResults ./TestResults.xml \
  -quit -logFile -
```

### Editor Test Runner
- Window > General > Test Runner > EditMode/PlayMode

## Code Style Guidelines

### Imports Order
1. `System.*` namespaces
2. Third-party libraries (Newtonsoft.Json, etc.)
3. `UnityEngine.*` / `UnityEditor.*`
4. Project namespaces (`ModelContextProtocol.*`)

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ModelContextProtocol.Protocol;
```

### Naming Conventions
- **Classes/Methods/Properties:** PascalCase (`McpServer`, `StartAsync`)
- **Private fields:** `_underscorePrefix` (`_server`, `_isRunning`)
- **Constants:** PascalCase or ALL_CAPS
- **Interfaces:** `I` prefix (`ITransport`, `ILogger`)
- **Parameters:** camelCase

### Formatting
- **Braces:** Allman style (opening brace on new line)
- **Indentation:** 4 spaces
- **Max line length:** ~120 characters

### Async/Await Pattern
- Use `CancellationToken` for all async operations
- Return `Task<T>` for async methods
- Use `IAsyncDisposable` for resource cleanup
- `async void` ONLY for Unity event handlers (`Start`, `OnDestroy`)

```csharp
public async Task<CallToolResult> HandleAsync(CallToolRequestParams params, CancellationToken ct)
{
    await _server.ProcessAsync(ct);
    return result;
}
```

### Error Handling
- Use `McpException` with `McpErrorCode` for protocol errors
- Use `Throw` helper class for argument validation
- Return `CallToolResult { IsError = true }` for tool errors

```csharp
Throw.IfNull(arg);
Throw.IfNullOrWhiteSpace(name);

throw new McpException(McpErrorCode.InvalidParams, "Parameter required");

return new CallToolResult { IsError = true, Content = ... };
```

### Unity-Specific Patterns
- Use `[SerializeField]` for private fields exposed in Inspector
- Inherit from `MonoBehaviour` for scene components
- Use `Debug.Log*` for logging (or `UnityLogger` wrapper)
- Prefer `Object.Destroy()` over `DestroyImmediate()` in runtime

### JSON Serialization
- Use `Newtonsoft.Json` (already included via `com.unity.nuget.newtonsoft-json`)
- Annotate properties with `[JsonProperty("name")]`

```csharp
[JsonProperty("name")]
public string Name { get; set; }

[JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
public string Description { get; set; }
```

## Vector Type Support

### Supported Types
| Type | JSON Schema | Protocol Format |
|------|-------------|-----------------|
| `Vector2` | `paramName_x`, `paramName_y` (number) | Individual properties |
| `Vector3` | `paramName_x`, `paramName_y`, `paramName_z` (number) | Individual properties |
| `Vector4` | `paramName_x`, `paramName_y`, `paramName_z`, `paramName_w` (number) | Individual properties |
| `Quaternion` | `paramName_x`, `paramName_y`, `paramName_z`, `paramName_w` (number) | Individual properties |
| `Vector2[]` / `List<Vector2>` | `{ "type": "array", "items": { "type": "number" } }` | Flat float array `[x1,y1, x2,y2, ...]` |
| `Vector3[]` / `List<Vector3>` | `{ "type": "array", "items": { "type": "number" } }` | Flat float array `[x1,y1,z1, x2,y2,z2, ...]` |
| `Vector4[]` / `List<Vector4>` | `{ "type": "array", "items": { "type": "number" } }` | Flat float array `[x1,y1,z1,w1, x2,y2,z2,w2, ...]` |
| `Quaternion[]` / `List<Quaternion>` | `{ "type": "array", "items": { "type": "number" } }` | Flat float array `[x1,y1,z1,w1, x2,y2,z2,w2, ...]` |

### Example Tool with Vector Array
```csharp
[McpServerTool("set_path_points", Description = "Set path points from position array")]
public static CallToolResult SetPathPoints(
    [McpArgument(Description = "Flat array of positions [x1,y1,z1, x2,y2,z2, ...]", Required = true)]
    Vector3[] points)
{
    foreach (var point in points)
        Debug.Log($"Point: {point}");
    return new CallToolResult 
    { 
        Content = new List<ContentBlock> { new TextContentBlock { Text = $"Set {points.Length} points" } } 
    };
}
```

### Protocol Example
```json
{
  "name": "set_path_points",
  "arguments": {
    "points": [0, 0, 0, 1, 2, 3, 4, 5, 6]
  }
}
```
This represents 3 Vector3 points: (0,0,0), (1,2,3), (4,5,6)

## Project Structure

```
Assets/Scripts/MCPForUnity/
├── Core/
│   ├── McpException.cs       # Custom exception types
│   ├── McpErrorCode.cs       # Error codes enum
│   ├── Throw.cs              # Argument validation helpers
│   └── Protocol/             # JSON-RPC and MCP protocol types
├── Server/
│   ├── McpServer.cs          # Main server implementation
│   ├── McpServerOptions.cs   # Configuration
│   ├── McpSessionHandler.cs  # Session management
│   └── Tools/
│       └── Attributes.cs     # [McpServerTool], [McpArgument]
├── Transport/
│   ├── ITransport.cs         # Transport interface
│   └── HttpListenerServerTransport.cs
├── Utilities/
│   ├── UnityLogger.cs        # Logging abstraction
│   └── MainThreadDispatcher.cs
└── Samples/
    └── MCPExampleUsage.cs    # Usage examples
```

## Key Types

| Type | Purpose |
|------|---------|
| `CallToolResult` | Tool execution result with `Content` list and `IsError` flag |
| `ContentBlock` | Base for `TextContentBlock`, `ImageContentBlock` |
| `Tool` | MCP tool definition with `Name`, `Description`, `InputSchema` |
| `McpServer` | Main server class, use `AddTool()`, `RegisterToolsFromClass<T>()` |

## Adding New Tools

### Method 1: Lambda Handler
```csharp
_server.AddTool("tool_name", "Description", async (args, ct) =>
{
    return new CallToolResult { Content = ... };
});
```

### Method 2: Attribute-based
```csharp
public static class MyTools
{
    [McpServerTool("tool_name", Description = "...")]
    public static CallToolResult MyTool(
        [McpArgument(Description = "...", Required = true)] string param)
    {
        return new CallToolResult { Content = ... };
    }
}
// Register: server.RegisterToolsFromClass(typeof(MyTools));
```

## Testing

- Test assembly: `ModelContextProtocol.Unity.Tests`
- Use Unity Test Framework (`[Test]`, `[UnityTest]`)
- `InternalsVisibleTo` configured for internal member access
