# Unity Editor MCP 工具规划

## 概述

- **实现文件**: `Assets/McpForUnity/Editor/Tools/EditorToolsList.cs`
- **工具数量**: 80 个
- **命名规范**: 大驼峰 (PascalCase)
- **工具前缀**: 统一使用 `Editor` 前缀

---

## 一、场景管理 (10 个)

| 序号 | 工具名 | 描述 | 参数 |
|------|--------|------|------|
| 1 | `EditorGetScenesInBuild` | 获取 Build Settings 场景列表 | 无 |
| 2 | `EditorGetActiveScene` | 获取当前激活场景信息 | 无 |
| 3 | `EditorLoadScene` | 加载场景 | `scenePath`, `additive` (可选) |
| 4 | `EditorCreateScene` | 创建新场景 | `sceneName` |
| 5 | `EditorSaveScene` | 保存当前场景 | 无 |
| 6 | `EditorSaveSceneAs` | 另存场景 | `scenePath` |
| 7 | `EditorCloseScene` | 关闭场景 | `scenePath` |
| 8 | `EditorAddSceneToBuild` | 添加场景到 Build | `scenePath` |
| 9 | `EditorRemoveSceneFromBuild` | 从 Build 移除场景 | `scenePath` |
| 10 | `EditorGetSceneHierarchy` | 获取场景层级结构 | `rootPath` (可选), `maxDepth` (可选) |

---

## 二、GameObject 操作 (15 个)

| 序号 | 工具名 | 描述 | 参数 |
|------|--------|------|------|
| 11 | `EditorCreateGameObject` | 创建空 GameObject | `name`, `parentPath` (可选), `position` (可选) |
| 12 | `EditorCreatePrimitive` | 创建基础几何体 | `type`, `name` (可选), `position` (可选) |
| 13 | `EditorDuplicateGameObject` | 复制 GameObject | `path` |
| 14 | `EditorDeleteGameObject` | 删除 GameObject | `path` |
| 15 | `EditorFindGameObject` | 查找 GameObject | `path` |
| 16 | `EditorFindGameObjectsByName` | 按名称查找 | `name`, `includeInactive` (可选) |
| 17 | `EditorFindGameObjectsByTag` | 按标签查找 | `tag` |
| 18 | `EditorFindGameObjectsByLayer` | 按层查找 | `layer` |
| 19 | `EditorGetGameObjectInfo` | 获取详细信息 | `path` |
| 20 | `EditorGetChildren` | 获取子物体列表 | `path`, `recursive` (可选) |
| 21 | `EditorSetParent` | 设置父物体 | `childPath`, `parentPath` |
| 22 | `EditorSetTag` | 设置标签 | `path`, `tag` |
| 23 | `EditorSetLayer` | 设置层 | `path`, `layer` |
| 24 | `EditorSetStatic` | 设置静态标志 | `path`, `isStatic` |
| 25 | `EditorSetActive` | 设置激活状态 | `path`, `active` |

---

## 三、Transform 操作 (10 个)

| 序号 | 工具名 | 描述 | 参数 |
|------|--------|------|------|
| 26 | `EditorSetPosition` | 设置世界位置 | `path`, `position` (Vector3) |
| 27 | `EditorSetRotation` | 设置世界旋转 | `path`, `rotation` (Vector3) |
| 28 | `EditorSetScale` | 设置缩放 | `path`, `scale` (Vector3) |
| 29 | `EditorSetLocalPosition` | 设置本地位置 | `path`, `position` |
| 30 | `EditorSetLocalRotation` | 设置本地旋转 | `path`, `rotation` |
| 31 | `EditorSetLocalScale` | 设置本地缩放 | `path`, `scale` |
| 32 | `EditorSetPositionAndRotation` | 同时设置位置旋转 | `path`, `position`, `rotation` |
| 33 | `EditorTranslate` | 相对移动 | `path`, `translation`, `relativeTo` |
| 34 | `EditorRotate` | 相对旋转 | `path`, `eulerAngles`, `relativeTo` |
| 35 | `EditorResetTransform` | 重置变换 | `path` |

---

## 四、组件操作 (10 个)

| 序号 | 工具名 | 描述 | 参数 |
|------|--------|------|------|
| 36 | `EditorAddComponent` | 添加组件 | `path`, `componentType` |
| 37 | `EditorRemoveComponent` | 移除组件 | `path`, `componentType` |
| 38 | `EditorGetComponent` | 获取组件信息 | `path`, `componentType` |
| 39 | `EditorGetComponents` | 获取所有组件 | `path` |
| 40 | `EditorGetComponentsInChildren` | 获取子物体组件 | `path`, `componentType`, `includeInactive` |
| 41 | `EditorSetComponentProperty` | 设置组件属性 | `path`, `componentType`, `propertyName`, `value` |
| 42 | `EditorGetComponentProperty` | 获取组件属性 | `path`, `componentType`, `propertyName` |
| 43 | `EditorHasComponent` | 检查是否有组件 | `path`, `componentType` |
| 44 | `EditorGetRequiredComponent` | 获取或添加组件 | `path`, `componentType` |
| 45 | `EditorSendComponentMessage` | 发送消息 | `path`, `methodName`, `value` (可选) |

---

## 五、编译与构建 (10 个)

| 序号 | 工具名 | 描述 | 参数 |
|------|--------|------|------|
| 46 | `EditorGetCompilationStatus` | 获取编译状态 | 无 |
| 47 | `EditorIsCompiling` | 是否正在编译 | 无 |
| 48 | `EditorGetCompileErrors` | 获取编译错误 | 无 |
| 49 | `EditorGetCompileWarnings` | 获取编译警告 | 无 |
| 50 | `EditorGetBuildTarget` | 获取构建目标 | 无 |
| 51 | `EditorSetBuildTarget` | 设置构建目标 | `buildTarget` |
| 52 | `EditorGetScriptingBackend` | 获取脚本后端 | 无 |
| 53 | `EditorGetAssemblyDefinitions` | 获取 asmdef 列表 | 无 |
| 54 | `EditorGetDefineSymbols` | 获取定义符号 | `buildTargetGroup` (可选) |
| 55 | `EditorSetDefineSymbols` | 设置定义符号 | `symbols`, `buildTargetGroup` (可选) |

---

## 六、资源管理 (15 个)

| 序号 | 工具名 | 描述 | 参数 |
|------|--------|------|------|
| 56 | `EditorFindAssets` | 查找资源 | `filter`, `type` (可选), `searchInFolders` (可选) |
| 57 | `EditorGetAssetInfo` | 获取资源详情 | `assetPath` |
| 58 | `EditorGetAssetPath` | 获取资源路径 | `guid` |
| 59 | `EditorGetAssetDependencies` | 获取资源依赖 | `assetPath` |
| 60 | `EditorGetFolderContents` | 获取目录内容 | `folderPath`, `recursive` (可选) |
| 61 | `EditorCreateFolder` | 创建文件夹 | `parentPath`, `folderName` |
| 62 | `EditorDeleteAsset` | 删除资源 | `assetPath` |
| 63 | `EditorRenameAsset` | 重命名资源 | `assetPath`, `newName` |
| 64 | `EditorMoveAsset` | 移动资源 | `sourcePath`, `destPath` |
| 65 | `EditorCopyAsset` | 复制资源 | `sourcePath`, `destPath` |
| 66 | `EditorRefreshAssets` | 刷新资源库 | `importMode` (可选) |
| 67 | `EditorReimportAsset` | 重新导入资源 | `assetPath` |
| 68 | `EditorGetAssetImportSettings` | 获取导入设置 | `assetPath` |
| 69 | `EditorLoadAssetAtPath` | 加载资源 | `assetPath` |
| 70 | `EditorGetAllAssetPaths` | 获取所有资源路径 | `filter` (可选) |

---

## 七、Prefab 操作 (10 个)

| 序号 | 工具名 | 描述 | 参数 |
|------|--------|------|------|
| 71 | `EditorInstantiatePrefab` | 实例化预制体 | `prefabPath`, `position` (可选), `rotation` (可选), `parentPath` (可选) |
| 72 | `EditorCreatePrefab` | 创建预制体 | `gameObjectPath`, `prefabPath` |
| 73 | `EditorApplyPrefab` | 应用预制体修改 | `gameObjectPath` |
| 74 | `EditorRevertPrefab` | 还原预制体 | `gameObjectPath` |
| 75 | `EditorUnpackPrefab` | 解包预制体 | `gameObjectPath`, `unpackMode` (可选) |
| 76 | `EditorGetPrefabInfo` | 获取预制体信息 | `path` |
| 77 | `EditorGetPrefabType` | 获取预制体类型 | `path` |
| 78 | `EditorIsPrefabInstance` | 是否是预制体实例 | `path` |
| 79 | `EditorGetPrefabAssetPath` | 获取预制体资源路径 | `gameObjectPath` |
| 80 | `EditorGetAllPrefabs` | 获取所有预制体 | `searchFolder` (可选) |

---

## 分类统计

| 分类 | 数量 |
|------|------|
| 场景管理 | 10 |
| GameObject 操作 | 15 |
| Transform 操作 | 10 |
| 组件操作 | 10 |
| 编译与构建 | 10 |
| 资源管理 | 15 |
| Prefab 操作 | 10 |
| **总计** | **80** |

---

## 实现规范

### 1. 工具定义模板

```csharp
[McpServerTool("EditorXxx", Description = "工具描述")]
public static CallToolResult EditorXxx(
    [McpArgument(Description = "参数描述", Required = true)] string param,
    [McpArgument(Description = "可选参数")] int optionalParam = 0)
{
    // 参数验证
    if (string.IsNullOrEmpty(param))
    {
        return ErrorResult("Parameter 'param' is required");
    }

    // 执行逻辑
    try
    {
        // ...
        return SuccessResult(result);
    }
    catch (Exception ex)
    {
        return ErrorResult($"Error: {ex.Message}");
    }
}
```

### 2. 辅助方法

```csharp
private static CallToolResult SuccessResult(object data)
{
    return new CallToolResult
    {
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = data is string s ? s : JObject.FromObject(data).ToString() }
        }
    };
}

private static CallToolResult ErrorResult(string message)
{
    return new CallToolResult
    {
        IsError = true,
        Content = new List<ContentBlock>
        {
            new TextContentBlock { Text = message }
        }
    };
}
```

### 3. 特殊处理

- **GameObject 路径**: 使用 `GameObject.Find(path)` 查找
- **Vector3 参数**: 使用展开形式 `path_x`, `path_y`, `path_z`
- **枚举参数**: 使用字符串，内部转换为枚举
- **数组参数**: 支持字符串数组 `string[]`

---

## 文件位置

```
Assets/McpForUnity/Editor/Tools/EditorToolsList.cs
```

## 注册方式

```csharp
// 在 GlobalEditorMcpServer.cs 中
_server.RegisterToolsFromClass(typeof(EditorToolsList));
```
