using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Unity;

namespace ModelContextProtocol.Editor
{
    public class McpServerEditorWindow : EditorWindow
    {
        private McpServer _server;
        private int _port = 3000;
        private bool _isRunning;
        private CancellationTokenSource _cts;

        private Vector2 _scrollPosition;
        private int _currentPage = 0;
        private int _itemsPerPage = 10;

        private GUIStyle _headerStyle;
        private GUIStyle _toolNameStyle;
        private GUIStyle _disabledToolNameStyle;
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

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_isRunning)
            {
                Repaint();
            }
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
                normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
            };

            _disabledToolNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = Color.gray }
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

            if (_isRunning)
            {
                int connectedClients = _server?.ConnectedClients ?? 0;
                var clientColor = connectedClients > 0 ? Color.cyan : Color.gray;
                var clientText = connectedClients > 0 ? $"{connectedClients} client(s) connected" : "No clients connected";
                GUI.color = clientColor;
                EditorGUILayout.LabelField($"  ○ {clientText}", _statusStyle);
                GUI.color = originalColor;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToolList()
        {
            var tools = _server?.AllTools;
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
                    if (tool.IsDisabled)
                    {
                        EditorGUILayout.LabelField($"○ {tool.Name} [Disabled]", _disabledToolNameStyle);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"● {tool.Name}", _toolNameStyle);
                    }
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
            var tools = _server?.AllTools;
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
                _cts = new CancellationTokenSource();

                var options = new McpServerOptions
                {
                    Port = _port,
                    ServerInfo = new Implementation
                    {
                        Name = "UnityMCPEditor",
                        Version = "1.0.0"
                    },
                    Instructions = "Unity MCP Editor Server - Control Unity Editor from AI assistants"
                };

                _server = new McpServer(options, new UnityLoggerImpl());
                _server.RegisterToolsFromClass(typeof(EditorToolsList));

                await _server.StartAsync(_cts.Token);

                _isRunning = true;
                _currentPage = 0;
                Repaint();

                Debug.Log($"[MCP Editor] Server started at http://localhost:{_port}/mcp");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Editor] Failed to start server: {ex.Message}");
                CleanupServer();
            }
        }

        private async void StopServer()
        {
            if (!_isRunning || _server == null) return;

            try
            {
                _cts?.Cancel();
                await _server.DisposeAsync();
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
            _cts?.Dispose();
            _cts = null;
            _server = null;
            _isRunning = false;
            Repaint();
        }

        private void OnDestroy()
        {
            if (_isRunning && _server != null)
            {
                try
                {
                    _cts?.Cancel();
                    _server.DisposeAsync().AsTask().Wait(1000);
                }
                catch { }
                CleanupServer();
            }
        }
    }
}
