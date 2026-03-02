    这是一个基于 Model Context Protocol (MCP) 的 Unity 实现，使 AI 助手（Cursor、Claude Code、OpenCode）能直接控制 Unity 编辑器和运行时应用。该工具有如下特点：
纯 C# 实现
    我们完全在Unity中开发，兼容Unity C# 语法。
MCP协议
    我们几乎支持了MCP协议文档中定义的所有内容（除了Promts）。
工具开发与扩展
    我们提供了80种内置的编辑器工具。
我们定义了简洁的工具定义方法，方便用户自行扩展符合项目需求的工具。
我们的协议支持多种的数据类型
基本的数据类型Unity内置的几何类型
Unity内置的几何类型
数组类型
用户自定义类型（有约束条件）
AI自动编码支持
    我们提供该项目的AGENTS.md文件，可用AI进行二次开发和扩展
模式
    同时支持Editor和Runtime两种模式


