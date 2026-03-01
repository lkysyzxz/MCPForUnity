# 类实例工具注册功能开发计划

## 📌 需求概述

支持为类实例注册工具，允许同一类型的多个实例分别注册独立的工具。

### 确定的设计

- **工具名称格式**: `{instanceId}.{methodName}`
- **Schema 信息**: 自动在描述中添加实例ID信息
- **实例ID参数**: 不作为参数传入

---

## 📋 任务列表

### 任务1：新增 McpInstanceToolAttribute

**文件:** `Assets/Scripts/MCPForUnity/Server/Tools/Attributes.cs`

**修改内容:**
```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class McpInstanceToolAttribute : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }
}
```

---

### 任务2：在 McpServer 中添加实例管理字段

**文件:** `Assets/Scripts/MCPForUnity/Server/McpServer.cs`

**添加字段:**
```csharp
private readonly Dictionary<string, object> _instances = new Dictionary<string, object>();
private readonly Dictionary<string, List<string>> _instanceToolNames = new Dictionary<string, List<string>>();
```

---

### 任务3：实现 RegisterToolsFromInstance 方法

**文件:** `Assets/Scripts/MCPForUnity/Server/McpServer.cs`

**方法签名:**
```csharp
public void RegisterToolsFromInstance(object instance, string instanceId)
```

**实现逻辑:**
1. 验证 instanceId 唯一性
2. 获取实例类型的 McpInstanceToolAttribute
3. 遍历所有非静态公共方法
4. 为每个方法生成工具名称: `{instanceId}.{methodName}`
5. 在描述中添加实例ID信息
6. 注册工具并记录映射关系

---

### 任务4：实现 UnregisterInstanceTools 方法

**文件:** `Assets/Scripts/MCPForUnity/Server/McpServer.cs`

**方法签名:**
```csharp
public void UnregisterInstanceTools(string instanceId)
```

**实现逻辑:**
1. 根据 instanceId 查找所有关联的工具名称
2. 从 _tools 和 _allTools 中移除
3. 清理实例引用

---

### 任务5：修改 RegisterToolFromMethod 支持实例工具

**文件:** `Assets/Scripts/MCPForUnity/Server/McpServer.cs`

**添加重载:**
```csharp
private void RegisterToolFromMethod(MethodInfo method, McpServerToolAttribute attr, Type declaringType, 
    object instance = null, string instanceId = null, string instanceDescription = null)
```

**修改逻辑:**
- 如果 instanceId 不为空，工具名称格式化为 `{instanceId}.{methodName}`
- 描述中添加 `[Instance: {instanceId}]` 前缀
- 使用传入的 instance 而非创建新实例

---

### 任务6：创建测试用例

**文件:** `Assets/Scripts/MCPForUnity/Samples/InstanceTools/`

**示例类型:**
```csharp
[McpInstanceTool(Name = "Player", Description = "玩家实例工具")]
public class PlayerInstance
{
    public int Health { get; set; }
    public string Name { get; set; }

    [McpServerTool(Description = "获取玩家生命值")]
    public int GetHealth()
    {
        return Health;
    }

    [McpServerTool(Description = "设置玩家生命值")]
    public void SetHealth(int value)
    {
        Health = value;
    }

    [McpServerTool(Description = "获取玩家名称")]
    public string GetName()
    {
        return Name;
    }
}
```

---

### 任务7：更新 AGENTS.md

**添加章节: Instance Tool Support**

内容包括:
- McpInstanceToolAttribute 用法
- RegisterToolsFromInstance API
- UnregisterInstanceTools API
- 工具名称格式说明
- 使用示例

---

### 任务8：编译测试

- Unity 编译验证
- 功能测试

---

### 任务9：提交并推送

---

## 🔧 技术细节

### 工具名称生成

```
静态工具: methodName
实例工具: {instanceId}.{methodName}

示例:
- 静态: test_address
- 实例: player_123.GetHealth
```

### 描述格式化

```
原始描述: "获取玩家生命值"
实例工具描述: "[Instance: player_123] 获取玩家生命值"
```

### 实例存储

```csharp
// 实例ID -> 实例对象
_instances["player_123"] = playerInstance;

// 实例ID -> 工具名称列表
_instanceToolNames["player_123"] = ["player_123.GetHealth", "player_123.GetName"];
```

---

## ⚠️ 注意事项

1. **实例ID唯一性**: 必须确保全局唯一
2. **实例生命周期**: 需要手动注销实例工具
3. **线程安全**: 实例字典需要考虑并发访问
4. **内存管理**: 实例引用可能导致内存泄漏，考虑使用 WeakReference

---

## 📊 预估时间

- 编码实现: 1.5 小时
- 测试验证: 0.5 小时
- 文档更新: 0.5 小时
- **总计: 2.5 小时**
