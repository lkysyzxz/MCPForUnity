using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ModelContextProtocol.Unity;

namespace ModelContextProtocol.Editor
{
    public class McpServerEditorWindow : EditorWindow
    {
        private int _port = 3000;
        private bool _isRunning;
        private MCPForUnityServer _serverComponent;
        private GameObject _serverObject;

        private Vector2 _scrollPosition;
        private int _currentPage = 0;
        private int _itemsPerPage = 10;

        private GUIStyle _headerStyle;
        private GUIStyle _toolNameStyle;
        private GUIStyle _toolDescStyle;
        private GUIStyle _statusStyle;
        private bool _stylesInitialized;

        [MenuItem("Tools/McpServerEditorWindow")]
        public static void ShowWindow()
        {
            var window = GetWindow<McpServerEditorWindow>("MCP Server");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            if (!_stylesInitialized)
            {
                InitStyles();
                _stylesInitialized = true;
            }

            DrawHeader();
            DrawServerControls();
            EditorGUILayout.Space(10);
            DrawToolList();
            DrawPagination();
        }

        private void InitStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 10)
            };

            _toolNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.4f, 0.8f, 1f) }
            };

            _toolDescStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                normal = { textColor = Color.gray }
            };

            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("MCP Server Editor", _headerStyle);
            EditorGUILayout.Space(5);
        }

        private void DrawServerControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(_isRunning);
            _port = EditorGUILayout.IntField("Port", _port);
            _port = Mathf.Clamp(_port, 1, 65535);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isRunning;
            if (GUILayout.Button("▶ Start", GUILayout.Height(30)))
            {
                StartServer();
            }
            GUI.enabled = _isRunning;
            if (GUILayout.Button("■ Stop", GUILayout.Height(30)))
            {
                StopServer();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            var statusColor = _isRunning ? Color.green : Color.gray;
            var statusText = _isRunning ? $"Running (http://localhost:{_port}/mcp)" : "Stopped";

            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"● {statusText}", _statusStyle);
            GUI.color = originalColor;

            EditorGUILayout.EndVertical();
        }

        private void DrawToolList()
        {
            var tools = _serverComponent?.Server?.Tools;
            int toolCount = tools?.Count ?? 0;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Registered Tools ({toolCount})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));

            if (tools != null && toolCount > 0)
            {
                int totalPages = Mathf.CeilToInt((float)toolCount / _itemsPerPage);
                _currentPage = Mathf.Clamp(_currentPage, 0, Mathf.Max(0, totalPages - 1));

                int startIndex = _currentPage * _itemsPerPage;
                int endIndex = Mathf.Min(startIndex + _itemsPerPage, toolCount);

                for (int i = startIndex; i < endIndex; i++)
                {
                    var tool = tools[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"● {tool.Name}", _toolNameStyle);
                    EditorGUILayout.LabelField(tool.Description ?? "No description", _toolDescStyle);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No tools registered.\n\nStart the server to see registered tools.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPagination()
        {
            var tools = _serverComponent?.Server?.Tools;
            int toolCount = tools?.Count ?? 0;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)toolCount / _itemsPerPage));

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUI.enabled = _currentPage > 0;
            if (GUILayout.Button("◀ Prev", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
            {
                _currentPage--;
            }
            GUI.enabled = true;

            EditorGUILayout.LabelField($"Page {_currentPage + 1}/{totalPages}", GUILayout.Width(80));

            GUI.enabled = _currentPage < totalPages - 1;
            if (GUILayout.Button("Next ▶", EditorStyles.miniButtonRight, GUILayout.Width(60)))
            {
                _currentPage++;
            }
            GUI.enabled = true;

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Per Page:", GUILayout.Width(60));
            _itemsPerPage = EditorGUILayout.IntPopup(_itemsPerPage,
                new[] { "5", "10", "20", "50" },
                new[] { 5, 10, 20, 50 },
                GUILayout.Width(60));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private async void StartServer()
        {
            if (_isRunning) return;

            try
            {
                _serverObject = new GameObject("[MCP Server - Editor]");
                _serverObject.hideFlags = HideFlags.HideAndDontSave;

                _serverComponent = _serverObject.AddComponent<MCPForUnityServer>();

                var portField = typeof(MCPForUnityServer).GetField("_port", BindingFlags.NonPublic | BindingFlags.Instance);
                portField?.SetValue(_serverComponent, _port);

                await _serverComponent.StartServerAsync();

                _isRunning = true;
                _currentPage = 0;
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Editor] Failed to start server: {ex.Message}");
                CleanupServer();
            }
        }

        private async void StopServer()
        {
            if (!_isRunning || _serverComponent == null) return;

            try
            {
                await _serverComponent.StopServerAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP Editor] Error stopping server: {ex.Message}");
            }
            finally
            {
                CleanupServer();
            }
        }

        private void CleanupServer()
        {
            if (_serverObject != null)
            {
                DestroyImmediate(_serverObject);
                _serverObject = null;
            }
            _serverComponent = null;
            _isRunning = false;
            Repaint();
        }

        private void OnDestroy()
        {
            if (_isRunning)
            {
                StopServer();
            }
        }
    }
}
