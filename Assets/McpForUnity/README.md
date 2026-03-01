# MCP for Unity

Model Context Protocol (MCP) SDK for Unity - 允许 AI 助手（如 Claude、ChatGPT）通过 MCP 协议控制 Unity。

## 功能特性

- 基于 HttpListener 的 HTTP 服务器
- 支持 SSE (Server-Sent Events) 长连接
- 支持完整 MCP 协议（Tools、Prompts、Resources）
- 支持 Tasks 异步任务系统
- 预置 Unity 场景操作工具
- 支持属性标注自动注册工具
- 纯 C# 实现，不依赖 MonoBehaviour

## 安装要求

- Unity 2021.3+
- Newtonsoft.Json（通过 Package Manager 安装）

### 安装 Newtonsoft.Json

1. 打开 **Window > Package Manager**
2. 点击 **+ > Add package by name...**
3. 输入 `com.unity.nuget.newtonsoft-json`
4. 点击 **Add**

或者编辑 `Packages/manifest.json`，添加：
```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

## 快速开始

### 1. 创建服务器实例

```csharp
using ModelContextProtocol.Unity;
using UnityEngine;

public class MCPServerSetup : MonoBehaviour
{
    private McpServerHost _host;

    async void Start()
    {
        var options = new McpServerHostOptions
        {
            Port = 3000,
            ServerName = "UnityMCP",
            EnableSceneTools = true,
            EnableConsoleTools = true,
            EnableTimeTools = true
        };

        _host = new McpServerHost(options);
        await _host.StartAsync();
    }

    private async void OnDestroy()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }
}
```

### 2. 配置选项

| 选项 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `Port` | int | 3000 | 服务器端口 |
| `ServerName` | string | "UnityMCP" | 服务器名称 |
| `ServerVersion` | string | "1.0.0" | 版本号 |
| `Instructions` | string | - | 服务器说明 |
| `LogLevel` | LogLevel | Information | 日志级别 |
| `EnableSceneTools` | bool | true | 启用场景操作工具 |
| `EnableConsoleTools` | bool | true | 启用控制台工具 |
| `EnableTimeTools` | bool | true | 启用时间控制工具 |

### 3. 连接 MCP 客户端

MCP 客户端（如 Claude Desktop、VS Code MCP 插件）配置：

```json
{
  "mcpServers": {
    "unity": {
      "url": "http://localhost:3000/mcp",
      "transport": "sse"
    }
  }
}
```

## 自定义工具

### 方式 1: 使用 AddCustomTool

```csharp
_host.AddCustomTool("create_sphere", "Create a sphere", async (args, ct) =>
{
    float x = args?["x"]?.Value<float>() ?? 0f;
    float y = args?["y"]?.Value<float>() ?? 0f;
    float z = args?["z"]?.Value<float>() ?? 0f;

    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    sphere.transform.position = new Vector3(x, y, z);

    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = "Sphere created!" }
        }
    };
});
```

### 方式 2: 使用属性标注

```csharp
public static class MyTools
{
    [McpServerTool("my_tool", Description = "My custom tool")]
    public static CallToolResult MyTool(string param1, int param2 = 0)
    {
        // 工具逻辑
        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = "Result" }
            }
        };
    }
}

// 注册
_host.Server.RegisterToolsFromClass<MyTools>();
```

## 预置工具

### 场景工具 (Scene Tools)

| 工具名 | 描述 |
|--------|------|
| `get_scene_info` | 获取当前场景信息 |
| `find_gameobject` | 按名称查找 GameObject |
| `get_gameobject_children` | 获取子对象列表 |
| `set_gameobject_active` | 设置激活状态 |

### 控制台工具 (Console Tools)

| 工具名 | 描述 |
|--------|------|
| `log_message` | 输出日志到 Unity Console |

### 时间工具 (Time Tools)

| 工具名 | 描述 |
|--------|------|
| `get_time_info` | 获取时间信息 |
| `set_time_scale` | 设置时间缩放 |

## API 参考

### IMcpServerHost 接口

```csharp
public interface IMcpServerHost : IAsyncDisposable
{
    McpServer Server { get; }
    bool IsRunning { get; }
    int Port { get; }
    int ConnectedClients { get; }

    event Action OnServerStarted;
    event Action OnServerStopped;
    event Action<string> OnServerError;

    Task StartAsync();
    Task StopAsync();

    void AddCustomTool(string name, string description,
        Func<JObject, CancellationToken, Task<CallToolResult>> handler,
        JObject inputSchema = null);

    void AddCustomTool<T>(string name, string description,
        Func<T, CancellationToken, Task<CallToolResult>> handler);
}
```

### McpServer

主要服务器类：

```csharp
// 添加工具
void AddTool(string name, string description, 
    Func<JObject, CancellationToken, Task<CallToolResult>> handler,
    JObject inputSchema = null)

// 添加 Prompt
void AddPrompt(Prompt prompt, 
    Func<GetPromptRequestParams, CancellationToken, Task<GetPromptResult>> handler)

// 添加 Resource
void AddResource(Resource resource,
    Func<ReadResourceRequestParams, CancellationToken, Task<ReadResourceResult>> handler)

// 发送通知
Task SendNotificationAsync(string method, object @params = null, 
    CancellationToken cancellationToken = default)
```

### Tasks 系统

```csharp
// 创建后台任务
var task = await server.TaskStore.RunAsTaskAsync(async ct =>
{
    // 长时间运行的任务
    await Task.Delay(5000, ct);
    return "Completed";
}, "my_task_type");
```

## 生命周期管理

`McpServerHost` 会自动监听 `Application.quitting` 事件，在应用退出时自动取消运行中的任务。

你也可以手动管理：

```csharp
// 启动
await _host.StartAsync();

// 停止
await _host.StopAsync();

// 释放资源（推荐在 OnDestroy 中调用）
await _host.DisposeAsync();
```

## 注意事项

1. **线程安全**: Unity API 必须在主线程调用。工具处理器在后台线程运行，如需操作 Unity 对象，请使用 `MainThreadDispatcher` 或类似机制。

2. **安全性**: 默认监听 `localhost`，不接受外部连接。如需外部访问，需修改 `HttpListener` 前缀。

3. **性能**: 大量工具调用可能影响性能，建议限制调用频率。

## 故障排除

### 端口被占用

```
Failed to start HTTP listener: Address in use
```

更改端口号或释放占用的端口。

### Newtonsoft.Json 未找到

确保已安装 `com.unity.nuget.newtonsoft-json` 包。

## License

MIT License
