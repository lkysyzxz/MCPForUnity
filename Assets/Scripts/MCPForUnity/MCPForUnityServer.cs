using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ModelContextProtocol.Unity
{
    public class MCPForUnityServer : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private int _port = 3000;
        [SerializeField] private string _serverName = "UnityMCP";
        [SerializeField] private string _serverVersion = "1.0.0";
        [SerializeField] private string _instructions = "Unity MCP Server - Control Unity from AI assistants";
        [SerializeField] private LogLevel _logLevel = LogLevel.Information;

        [Header("Features")]
        [SerializeField] private bool _enableSceneTools = true;
        [SerializeField] private bool _enableConsoleTools = true;
        [SerializeField] private bool _enableTimeTools = true;

        private McpServer _server;
        private CancellationTokenSource _cts;
        private bool _isRunning;

        public McpServer Server => _server;
        public bool IsRunning => _isRunning;
        public int Port => _port;
        public int ConnectedClients => _server?.ConnectedClients ?? 0;

        public event Action OnServerStarted;
        public event Action OnServerStopped;
        public event Action<string> OnServerError;

        private void Awake()
        {
            UnityLogger.MinimumLevel = _logLevel;
        }

        private void OnApplicationQuit()
        {
            _cts?.Cancel();
        }

        public async Task StartServerAsync()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[MCP] Server is already running");
                return;
            }

            try
            {
                _cts = new CancellationTokenSource();

                var options = new McpServerOptions
                {
                    Port = _port,
                    ServerInfo = new Implementation
                    {
                        Name = _serverName,
                        Version = _serverVersion
                    },
                    Instructions = _instructions
                };

                _server = new McpServer(options, new UnityLoggerImpl());

                RegisterDefaultTools();

                await _server.StartAsync(_cts.Token);
                _isRunning = true;

                Debug.Log($"[MCP] Server started at http://localhost:{_port}/mcp");
                OnServerStarted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to start server: {ex.Message}");
                OnServerError?.Invoke(ex.Message);
            }
        }

        public async Task StopServerAsync()
        {
            if (!_isRunning || _server == null) return;

            try
            {
                _cts?.Cancel();
                await _server.DisposeAsync();
                _server = null;
                _isRunning = false;

                Debug.Log("[MCP] Server stopped");
                OnServerStopped?.Invoke();
            }
            catch (Exception ex)
            {
                // Debug.LogError($"[MCP] Error stopping server: {ex.Message}");
                Debug.Log("[MCP] Server stopped");
                OnServerStopped?.Invoke();
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void RegisterDefaultTools()
        {
            if (_enableSceneTools)
            {
                RegisterSceneTools();
            }

            if (_enableConsoleTools)
            {
                RegisterConsoleTools();
            }

            if (_enableTimeTools)
            {
                RegisterTimeTools();
            }
        }

        private void RegisterSceneTools()
        {
            _server.AddTool("get_scene_info", "Get information about the current scene", async (args, ct) =>
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();

                var info = new JObject
                {
                    ["name"] = scene.name,
                    ["path"] = scene.path,
                    ["buildIndex"] = scene.buildIndex,
                    ["rootCount"] = rootObjects.Length,
                    ["isLoaded"] = scene.isLoaded,
                    ["isDirty"] = scene.isDirty
                };

                var gameObjects = new JArray();
                foreach (var obj in rootObjects)
                {
                    gameObjects.Add(new JObject
                    {
                        ["name"] = obj.name,
                        ["active"] = obj.activeSelf,
                        ["tag"] = obj.tag,
                        ["layer"] = LayerMask.LayerToName(obj.layer)
                    });
                }
                info["rootGameObjects"] = gameObjects;

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = info.ToString() }
                    }
                };
            });

            _server.AddTool("find_gameobject", "Find a GameObject by name", async (args, ct) =>
            {
                string name = args?["name"]?.ToString();
                if (string.IsNullOrEmpty(name))
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = "Parameter 'name' is required" }
                        }
                    };
                }

                var obj = GameObject.Find(name);
                if (obj == null)
                {
                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"GameObject '{name}' not found" }
                        }
                    };
                }

                var info = GetGameObjectInfo(obj);
                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = info.ToString() }
                    }
                };
            });

            _server.AddTool("get_gameobject_children", "Get children of a GameObject", async (args, ct) =>
            {
                string path = args?["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = "Parameter 'path' is required" }
                        }
                    };
                }

                var obj = GameObject.Find(path);
                if (obj == null)
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"GameObject at '{path}' not found" }
                        }
                    };
                }

                var children = new JArray();
                foreach (Transform child in obj.transform)
                {
                    children.Add(new JObject
                    {
                        ["name"] = child.name,
                        ["active"] = child.gameObject.activeSelf,
                        ["path"] = GetGameObjectPath(child.gameObject)
                    });
                }

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = children.ToString() }
                    }
                };
            });

            _server.AddTool("set_gameobject_active", "Set a GameObject active state", async (args, ct) =>
            {
                string path = args?["path"]?.ToString();
                bool active = args?["active"]?.Value<bool>() ?? true;

                if (string.IsNullOrEmpty(path))
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = "Parameter 'path' is required" }
                        }
                    };
                }

                var obj = GameObject.Find(path);
                if (obj == null)
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"GameObject at '{path}' not found" }
                        }
                    };
                }

                obj.SetActive(active);

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"GameObject '{path}' set active: {active}" }
                    }
                };
            });
        }

        private void RegisterConsoleTools()
        {
            _server.AddTool("log_message", "Log a message to Unity console", async (args, ct) =>
            {
                string message = args?["message"]?.ToString();
                string type = args?["type"]?.ToString()?.ToLower() ?? "info";

                if (string.IsNullOrEmpty(message))
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = "Parameter 'message' is required" }
                        }
                    };
                }

                switch (type)
                {
                    case "warning":
                        Debug.LogWarning(message);
                        break;
                    case "error":
                        Debug.LogError(message);
                        break;
                    default:
                        Debug.Log(message);
                        break;
                }

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Logged: [{type}] {message}" }
                    }
                };
            });
        }

        private void RegisterTimeTools()
        {
            _server.AddTool("get_time_info", "Get Unity time information", async (args, ct) =>
            {
                var info = new JObject
                {
                    ["time"] = Time.time,
                    ["deltaTime"] = Time.deltaTime,
                    ["fixedDeltaTime"] = Time.fixedDeltaTime,
                    ["timeScale"] = Time.timeScale,
                    ["frameCount"] = Time.frameCount,
                    ["realtimeSinceStartup"] = Time.realtimeSinceStartup,
                    ["unscaledTime"] = Time.unscaledTime
                };

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = info.ToString() }
                    }
                };
            });

            _server.AddTool("set_time_scale", "Set Unity time scale", async (args, ct) =>
            {
                float? scale = args?["scale"]?.Value<float>();

                if (!scale.HasValue)
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = "Parameter 'scale' is required" }
                        }
                    };
                }

                Time.timeScale = scale.Value;

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Time scale set to: {scale.Value}" }
                    }
                };
            });
        }

        private static JObject GetGameObjectInfo(GameObject obj)
        {
            var info = new JObject
            {
                ["name"] = obj.name,
                ["active"] = obj.activeSelf,
                ["activeInHierarchy"] = obj.activeInHierarchy,
                ["tag"] = obj.tag,
                ["layer"] = LayerMask.LayerToName(obj.layer),
                ["path"] = GetGameObjectPath(obj),
                ["position"] = JObject.FromObject(new { x = obj.transform.position.x, y = obj.transform.position.y, z = obj.transform.position.z }),
                ["rotation"] = JObject.FromObject(new { x = obj.transform.rotation.eulerAngles.x, y = obj.transform.rotation.eulerAngles.y, z = obj.transform.rotation.eulerAngles.z }),
                ["scale"] = JObject.FromObject(new { x = obj.transform.localScale.x, y = obj.transform.localScale.y, z = obj.transform.localScale.z })
            };

            var components = new JArray();
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp != null)
                {
                    components.Add(comp.GetType().Name);
                }
            }
            info["components"] = components;

            return info;
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        public void AddCustomTool(string name, string description, Func<JObject, CancellationToken, Task<CallToolResult>> handler, JObject inputSchema = null)
        {
            if (_server == null)
            {
                Debug.LogError("[MCP] Server not initialized");
                return;
            }

            _server.AddTool(name, description, handler, inputSchema);
        }

        public void AddCustomTool<T>(string name, string description, Func<T, CancellationToken, Task<CallToolResult>> handler)
        {
            if (_server == null)
            {
                Debug.LogError("[MCP] Server not initialized");
                return;
            }

            _server.AddTool(name, description, async (args, ct) =>
            {
                T typedArgs = args != null ? args.ToObject<T>() : default;
                return await handler(typedArgs, ct);
            });
        }
    }
}
