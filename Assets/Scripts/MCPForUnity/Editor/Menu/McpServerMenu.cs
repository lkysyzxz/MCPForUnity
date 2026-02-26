#if UNITY_EDITOR
using UnityEditor;

namespace ModelContextProtocol.Editor
{
    public static class McpServerMenu
    {
        private const string MenuLaunch = "Tools/MCP For Unity/Launch Server";
        private const string MenuClose = "Tools/MCP For Unity/Close Server";

        [MenuItem(MenuLaunch)]
        public static void LaunchServer()
        {
            GlobalEditorMcpServer.StartServer();
        }

        [MenuItem(MenuLaunch, true)]
        public static bool ValidateLaunchServer()
        {
            return !GlobalEditorMcpServer.IsRunning;
        }

        [MenuItem(MenuClose)]
        public static void CloseServer()
        {
            GlobalEditorMcpServer.StopServer();
        }

        [MenuItem(MenuClose, true)]
        public static bool ValidateCloseServer()
        {
            return GlobalEditorMcpServer.IsRunning;
        }
    }
}
#endif
