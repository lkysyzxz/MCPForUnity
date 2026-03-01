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

## Custom Type Parameter Support

### Overview
Support for custom types as tool parameters. Custom types are serialized as JSON objects with automatic schema generation.

### Validation Rules (Strict Mode)
- **Required**: Fields must have BOTH `[JsonProperty]` AND `[McpArgument]` attributes
- If a field uses only one attribute, the type is marked as **invalid**
- At least one valid field is required per custom type

### Required Field Determination

Required 状态由以下特性决定（优先级从高到低）：

| Priority | Attribute | Effect |
|----------|-----------|--------|
| 1 | `[JsonRequired]` | Required = true (highest priority) |
| 2 | `[McpArgument(Required = true)]` | Required = true (fallback) |
| 3 | None | Required = false |

Example:
```csharp
public class ExampleType
{
    // Using JsonRequired
    [JsonProperty("name")]
    [JsonRequired]
    [McpArgument(Description = "Name")]
    public string Name;  // Required = true (JsonRequired)
    
    // Using McpArgument.Required
    [JsonProperty("email")]
    [McpArgument(Description = "Email", Required = true)]
    public string Email;  // Required = true
    
    // Optional field
    [JsonProperty("phone")]
    [McpArgument(Description = "Phone")]
    public string Phone;  // Required = false
}
```

### Supported Type Forms
| Type | JSON Schema | Example |
|------|-------------|---------|
| Basic custom type | `{ "type": "object", "properties": {...} }` | `PersonInfo` |
| Nested custom type | Nested object schema | `PersonInfo.Address` |
| Custom type array | `{ "type": "array", "items": {...} }` | `PersonInfo[]`, `List<PersonInfo>` |
| Mixed with primitive arrays | `{ "type": "array", "items": { "type": "string" } }` | `List<string>` |

### Constraints
- Public fields only (not properties)
- Must be non-primitive, non-Unity, non-System types
- Circular references handled gracefully

### McpArgument.Name Limitation

**For custom type fields**, `McpArgument.Name` is ignored. Field names are determined by `JsonProperty.PropertyName`.

```csharp
public class ExampleType
{
    // ✓ Correct: Only use JsonProperty for name
    [JsonProperty("userName")]
    [McpArgument(Description = "User name", Required = true)]
    public string UserName;  // JSON field name: "userName"
    
    // ✗ Avoid: McpArgument.Name will be ignored
    [JsonProperty("email")]
    [McpArgument(Name = "userEmail", Description = "Email")]  // Name ignored
    public string Email;  // JSON field name still: "email"
}
```

**Note:** For Method parameters, `McpArgument.Name` is still effective.

| Context | McpArgument.Name | JsonProperty.PropertyName |
|---------|-----------------|--------------------------|
| Method Parameter | ✓ Effective | N/A |
| Custom Type Field | ✗ Ignored | ✓ Effective |

A warning will be logged if `McpArgument.Name` conflicts with `JsonProperty.PropertyName`.

### Example: Define Custom Types

```csharp
// Basic custom type
public class AddressInfo
{
    [JsonProperty("street")]
    [JsonRequired]  // Optional: mark as required
    [McpArgument(Description = "街道名称")]
    public string Street;

    [JsonProperty("city")]
    [McpArgument(Description = "城市名称", Required = true)]
    public string City;

    [JsonProperty("zipCode")]
    [McpArgument(Description = "邮政编码")]
    public string ZipCode;
}

// Nested custom type
public class PersonInfo
{
    [JsonProperty("name")]
    [McpArgument(Description = "姓名", Required = true)]
    public string Name;

    [JsonProperty("age")]
    [McpArgument(Description = "年龄")]
    public int Age;

    [JsonProperty("address")]
    [McpArgument(Description = "地址信息（嵌套对象）")]
    public AddressInfo Address;
}

// Array custom type
public class TeamInfo
{
    [JsonProperty("teamName")]
    [McpArgument(Description = "团队名称", Required = true)]
    public string TeamName;

    [JsonProperty("members")]
    [McpArgument(Description = "团队成员列表")]
    public PersonInfo[] Members;
}
```

### Example: Tool with Custom Type

```csharp
[McpServerTool("register_person", Description = "注册新用户")]
public static CallToolResult RegisterPerson(
    [McpArgument(Description = "用户信息", Required = true)]
    PersonInfo person)
{
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = $"Registered: {person.Name}" }
        }
    };
}

[McpServerTool("create_team", Description = "创建团队")]
public static CallToolResult CreateTeam(
    [McpArgument(Description = "团队信息", Required = true)]
    TeamInfo team)
{
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = $"Team '{team.TeamName}' with {team.Members?.Length ?? 0} members" }
        }
    };
}
```

### Protocol Example
```json
{
  "name": "register_person",
  "arguments": {
    "person": {
      "name": "张三",
      "age": 25,
      "address": {
        "city": "北京",
        "zipCode": "100000"
      }
    }
  }
}
```

## UTF-8 Encoding

### RFC 8259 Compliance
All JSON request/response encoding enforces UTF-8 per RFC 8259 specification.

### Chinese Character Handling
- Request body: Read with `Encoding.UTF8`
- Response body: Written with `Encoding.UTF8`
- Content-Type: `application/json; charset=utf-8` / `text/event-stream; charset=utf-8`

### Implementation Location
`HttpListenerServerTransport.cs`:
- `HandlePostRequestAsync()`: UTF-8 request reading (line 198)
- `HandleGetRequestAsync()`: SSE stream encoding (line 160)
- Response headers include `charset=utf-8`

## Editor Window Features

### Tool Status Display
| Status | Icon | Color | Condition |
|--------|------|-------|-----------|
| Valid | ✓ | Green | `IsValid == true && !IsDisabled` |
| Disabled | ○ | Gray | `IsDisabled == true` |
| Invalid | ✗ | Red | `IsValid == false` |

### Error Information Display
- Invalid tools show `ValidationError` in a HelpBox
- Red styling for tool name with "[Invalid]" suffix
- Description still displayed below error

### Window Access
- Menu: `Tools > MCP For Unity > Server Window`
- Shows all registered tools with status indicators
- Pagination support for large tool lists

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
    ├── MCPExampleUsage.cs    # Usage examples
    └── CustomTypes/          # Custom type examples
        ├── PersonInfo.cs     # Nested custom type example
        ├── TeamInfo.cs       # Array custom type example
        ├── AddressInfo.cs    # Basic custom type example
        └── InvalidCustomType.cs  # Invalid type for testing
```

## Key Types

| Type | Purpose |
|------|---------|
| `CallToolResult` | Tool execution result with `Content` list and `IsError` flag |
| `ContentBlock` | Base for `TextContentBlock`, `ImageContentBlock` |
| `Tool` | MCP tool definition with `Name`, `Description`, `InputSchema`, `IsValid`, `ValidationError` |
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

### Method 3: With Custom Type
```csharp
public static class PersonTools
{
    [McpServerTool("update_user", Description = "Update user information")]
    public static CallToolResult UpdateUser(
        [McpArgument(Description = "User data", Required = true)] PersonInfo user)
    {
        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = $"Updated: {user.Name}" }
            }
        };
    }
}
// Register: server.RegisterToolsFromClass(typeof(PersonTools));
```

## Testing

- Test assembly: `ModelContextProtocol.Unity.Tests`
- Use Unity Test Framework (`[Test]`, `[UnityTest]`)
- `InternalsVisibleTo` configured for internal member access
