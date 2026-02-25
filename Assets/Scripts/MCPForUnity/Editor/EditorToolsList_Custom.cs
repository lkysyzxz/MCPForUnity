#if UNITY_EDITOR
namespace ModelContextProtocol.Editor
{
    public static partial class EditorToolsList
    {
        // Add your custom MCP tools here
        // Example:
        // [McpServerTool("MyCustomTool", Description = "Description of my custom tool")]
        // public static CallToolResult MyCustomTool(
        //     [McpArgument(Description = "Parameter description", Required = true)] string param)
        // {
        //     return new CallToolResult
        //     {
        //         Content = new List<ContentBlock>
        //         {
        //             new TextContentBlock { Text = "Result" }
        //         }
        //     };
        // }
    }
}
#endif
