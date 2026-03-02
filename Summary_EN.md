# Unity MCP Integration

This is a Unity implementation based on the Model Context Protocol (MCP), enabling AI assistants (Cursor, Claude Code, OpenCode) to directly control the Unity Editor and runtime applications.

## Features

### Pure C# Implementation
Fully developed in Unity, compatible with Unity C# syntax.

### MCP Protocol
Supports almost all features defined in the MCP protocol documentation (except Prompts).

### Tool Development and Extension
- Provides 80 built-in editor tools
- Offers a simple tool definition method for users to extend tools according to project requirements
- Protocol supports multiple data types:
  - Basic data types
  - Unity built-in geometry types
  - Array types
  - User-defined custom types (with constraints)

### AI-Assisted Coding Support
Provides AGENTS.md file for secondary development and extension with AI.

### Modes
Supports both Editor and Runtime modes.
