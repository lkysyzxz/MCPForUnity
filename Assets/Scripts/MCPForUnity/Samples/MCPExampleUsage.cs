using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Unity;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModelContextProtocol.Samples
{
    public class MCPExampleUsage : MonoBehaviour
    {
        private MCPForUnityServer _mcpServer;

        private async void Start()
        {
            _mcpServer = gameObject.AddComponent<MCPForUnityServer>();
            
            await Task.Delay(1000);
            
            if (_mcpServer.IsRunning)
            {
                AddCustomTools();
            }
        }

        private void AddCustomTools()
        {
            _mcpServer.AddCustomTool("create_cube", "Create a cube at specified position", async (args, ct) =>
            {
                float x = args?["x"]?.Value<float>() ?? 0f;
                float y = args?["y"]?.Value<float>() ?? 0f;
                float z = args?["z"]?.Value<float>() ?? 0f;
                string name = args?["name"]?.ToString() ?? "Cube";

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = name;
                cube.transform.position = new Vector3(x, y, z);

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Created cube '{name}' at ({x}, {y}, {z})" }
                    }
                };
            }, JObject.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"", ""description"": ""Name of the cube"" },
                    ""x"": { ""type"": ""number"", ""description"": ""X position"", ""default"": 0 },
                    ""y"": { ""type"": ""number"", ""description"": ""Y position"", ""default"": 0 },
                    ""z"": { ""type"": ""number"", ""description"": ""Z position"", ""default"": 0 }
                },
                ""required"": []
            }"));

            _mcpServer.AddCustomTool("move_gameobject", "Move a GameObject to a new position", async (args, ct) =>
            {
                string path = args?["path"]?.ToString();
                float x = args?["x"]?.Value<float>() ?? 0f;
                float y = args?["y"]?.Value<float>() ?? 0f;
                float z = args?["z"]?.Value<float>() ?? 0f;

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
                            new TextContentBlock { Text = $"GameObject '{path}' not found" }
                        }
                    };
                }

                obj.transform.position = new Vector3(x, y, z);

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Moved '{path}' to ({x}, {y}, {z})" }
                    }
                };
            }, JObject.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""path"": { ""type"": ""string"", ""description"": ""Path to the GameObject"" },
                    ""x"": { ""type"": ""number"", ""description"": ""X position"" },
                    ""y"": { ""type"": ""number"", ""description"": ""Y position"" },
                    ""z"": { ""type"": ""number"", ""description"": ""Z position"" }
                },
                ""required"": [""path"", ""x"", ""y"", ""z""]
            }"));
        }

        private async void OnDestroy()
        {
            if (_mcpServer != null)
            {
                await _mcpServer.StopServerAsync();
            }
        }
    }

    public class MCPExampleWithAttributes : MonoBehaviour
    {
        private MCPForUnityServer _mcpServer;

        private async void Start()
        {
            _mcpServer = gameObject.AddComponent<MCPForUnityServer>();
            
            await Task.Delay(1000);
            
            if (_mcpServer.IsRunning && _mcpServer.Server != null)
            {
                _mcpServer.Server.RegisterToolsFromClass(typeof(CustomTools));
            }
        }
    }

    public static class CustomTools
    {
        [McpServerTool("get_application_info", Description = "Get Unity application information")]
        public static CallToolResult GetApplicationInfo()
        {
            var info = JObject.FromObject(new
            {
                productName = Application.productName,
                version = Application.version,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                isEditor = Application.isEditor,
                isPlaying = Application.isPlaying,
                dataPath = Application.dataPath
            });

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = info.ToString() }
                }
            };
        }

        [McpServerTool("destroy_gameobject", Description = "Destroy a GameObject by path")]
        public static CallToolResult DestroyGameObject(
            [McpArgument(Description = "Path to the GameObject in the scene hierarchy", Required = true)] string path)
        {
            var obj = GameObject.Find(path);
            if (obj == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"GameObject '{path}' not found" }
                    }
                };
            }

            Object.Destroy(obj);

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Destroyed '{path}'" }
                }
            };
        }

        [McpServerTool("create_primitive", Description = "Create a Unity primitive object")]
        public static CallToolResult CreatePrimitive(
            [McpArgument(Description = "Type of primitive to create", Required = true)] PrimitiveType type,
            [McpArgument(Description = "Name for the new GameObject", Required = false)] string name = "Primitive",
            [McpArgument(Description = "X position", Required = false)] float x = 0,
            [McpArgument(Description = "Y position", Required = false)] float y = 0,
            [McpArgument(Description = "Z position", Required = false)] float z = 0)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = new Vector3(x, y, z);

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Created {type} '{name}' at ({x}, {y}, {z})" }
                }
            };
        }

        [McpServerTool("set_time_scale", Description = "Set Unity time scale for controlling game speed")]
        public static CallToolResult SetTimeScale(
            [McpArgument(Description = "Time scale value (1 = normal, 0 = paused, 2 = double speed)", Required = true)] float scale)
        {
            if (scale < 0)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "Time scale cannot be negative" }
                    }
                };
            }

            Time.timeScale = scale;

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Time scale set to {scale}" }
                }
            };
        }

        [McpServerTool("find_gameobjects_by_tags", Description = "Find GameObjects by multiple tags")]
        public static CallToolResult FindGameObjectsByTags(
            [McpArgument(Description = "List of tags to search for", Required = true)] string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "At least one tag is required" }
                    }
                };
            }

            var results = new JArray();

            foreach (var tag in tags)
            {
                try
                {
                    var objects = GameObject.FindGameObjectsWithTag(tag);
                    foreach (var obj in objects)
                    {
                        results.Add(new JObject
                        {
                            ["name"] = obj.name,
                            ["tag"] = obj.tag,
                            ["active"] = obj.activeSelf,
                            ["position"] = JObject.FromObject(new
                            {
                                x = obj.transform.position.x,
                                y = obj.transform.position.y,
                                z = obj.transform.position.z
                            })
                        });
                    }
                }
                catch (UnityException)
                {
                    // Tag not defined
                }
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = results.ToString() }
                }
            };
        }

        [McpServerTool("set_gameobjects_active", Description = "Set active state for multiple GameObjects")]
        public static CallToolResult SetGameObjectsActive(
            [McpArgument(Description = "Paths to the GameObjects", Required = true)] string[] paths,
            [McpArgument(Description = "Whether to activate or deactivate", Required = true)] bool active)
        {
            var results = new JArray();

            foreach (var path in paths)
            {
                var obj = GameObject.Find(path);
                if (obj != null)
                {
                    obj.SetActive(active);
                    results.Add(new JObject { ["path"] = path, ["success"] = true, ["active"] = active });
                }
                else
                {
                    results.Add(new JObject { ["path"] = path, ["success"] = false, ["error"] = "Not found" });
                }
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = results.ToString() }
                }
            };
        }

        [McpServerTool("set_render_settings", Description = "Configure Unity render settings")]
        public static CallToolResult SetRenderSettings(
            [McpArgument(Description = "Ambient light mode: Skybox, Flat, Gradient, Trilight", Required = false)] AmbientMode? ambientMode,
            [McpArgument(Description = "Ambient intensity (0-8)", Required = false)] float? ambientIntensity = null,
            [McpArgument(Description = "Fog enabled", Required = false)] bool? fog = null,
            [McpArgument(Description = "Fog mode: Linear, Exponential, ExponentialSquared", Required = false)] FogMode? fogMode = null,
            [McpArgument(Description = "Fog density for exponential modes", Required = false)] float? fogDensity = null)
        {
            if (ambientMode.HasValue)
            {
                RenderSettings.ambientMode = ambientMode.Value;
            }

            if (ambientIntensity.HasValue)
            {
                RenderSettings.ambientIntensity = Mathf.Clamp(ambientIntensity.Value, 0f, 8f);
            }

            if (fog.HasValue)
            {
                RenderSettings.fog = fog.Value;
            }

            if (fogMode.HasValue)
            {
                RenderSettings.fogMode = fogMode.Value;
            }

            if (fogDensity.HasValue)
            {
                RenderSettings.fogDensity = Mathf.Max(0f, fogDensity.Value);
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = "Render settings updated" }
                }
            };
        }


        [McpServerTool("Echo", Description = "回声测试函数")]
        public static CallToolResult Echo(
            [McpArgument(Description = "回声测试输入信息", Required = true)]string message)
        {
            Debug.Log(message);

            return new CallToolResult()
            {
                Content = new List<ContentBlock>()
                {
                    new TextContentBlock { Text = "Successed" }
                }
            };
        }

        [McpServerTool(Name = "EchoArray", Description = "数组回声测试函数")]
        public static CallToolResult EchoArray(
            [McpArgument(Description = "多条回声测试信息", Required = true)] string[] messages)
        {
            foreach (var mesg in messages)
            {
                Debug.Log(mesg);
            }

            return new CallToolResult()
            {
                Content = new List<ContentBlock>()
                {
                    new TextContentBlock { Text = "Successed" }
                }
            };
        }

        [McpServerTool(Name = "log_position3d", Description = "Vector3 传参测试")]
        public static CallToolResult LogPosition3D(
            [McpArgument(Description = "三维空间坐标", Required = true)]
            Vector3 position)
        {
            Debug.Log($"Position: {position.x}, {position.y}, {position.z}");

            return new CallToolResult()
            {
                Content = new List<ContentBlock>()
                {
                    new TextContentBlock { Text = "Successed" }
                }
            };
        }

        [McpServerTool(Name = "log_position2d", Description = "Vector2 传参测试")]
        public static CallToolResult LogPosition2D(
            [McpArgument(Description = "二维空间坐标")]Vector2 position)
        {
            Debug.Log($"Position: {position.x}, {position.y}");
            
            return new CallToolResult()
            {
                Content = new List<ContentBlock>()
                {
                    new TextContentBlock { Text = "Successed" }
                }
            };
        }

        [McpServerTool(Name = "log_quaternion", Description = "Quaternion 传参测试")]
        public static CallToolResult LogRoataion(
            [McpArgument(Description = "三维空间Rotation", Required = true)]Quaternion quaternion)
        {
            Debug.Log($"Quaternion: {quaternion.x}, {quaternion.y}, {quaternion.z}, {quaternion.w}");
            
            return new CallToolResult()
            {
                Content = new List<ContentBlock>()
                {
                    new TextContentBlock { Text = "Successed" }
                }
            };
        }

        [McpServerTool("set_transform", Description = "Set position, rotation and scale of a GameObject")]
        public static CallToolResult SetTransform(
            [McpArgument(Description = "Path to the GameObject", Required = true)] string path,
            [McpArgument(Description = "World position")] UnityEngine.Vector3 position,
            [McpArgument(Description = "Rotation in Euler angles (degrees)")] UnityEngine.Vector3 rotation = default,
            [McpArgument(Description = "Local scale")] UnityEngine.Vector3 scale = default)
        {
            var obj = GameObject.Find(path);
            if (obj == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"GameObject '{path}' not found" }
                    }
                };
            }

            obj.transform.position = position;
            obj.transform.eulerAngles = rotation;
            obj.transform.localScale = scale;

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Set transform of '{path}':\nPosition: {position}\nRotation: {rotation}\nScale: {scale}" }
                }
            };
        }

        [McpServerTool("move_to_position", Description = "Move a GameObject to a target position")]
        public static CallToolResult MoveToPosition(
            [McpArgument(Description = "Path to the GameObject", Required = true)] string path,
            [McpArgument(Description = "Target position")] UnityEngine.Vector3 targetPosition,
            [McpArgument(Description = "Speed of movement")] float speed = 1.0f)
        {
            var obj = GameObject.Find(path);
            if (obj == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"GameObject '{path}' not found" }
                    }
                };
            }

            obj.transform.position = targetPosition;

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Moved '{path}' to {targetPosition}" }
                }
            };
        }

        [McpServerTool("create_at_position", Description = "Create a primitive at a specific position with rotation")]
        public static CallToolResult CreateAtPosition(
            [McpArgument(Description = "Primitive type")] PrimitiveType primitiveType,
            [McpArgument(Description = "Spawn position")] UnityEngine.Vector3 position,
            [McpArgument(Description = "Spawn rotation (quaternion)")] UnityEngine.Quaternion rotation = default,
            [McpArgument(Description = "Name for the GameObject")] string name = null)
        {
            var obj = GameObject.CreatePrimitive(primitiveType);
            obj.name = name ?? primitiveType.ToString();
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Created {primitiveType} '{obj.name}' at position {position} with rotation {rotation}" }
                }
            };
        }

        [McpServerTool("set_ui_position", Description = "Set UI element position (2D)")]
        public static CallToolResult SetUiPosition(
            [McpArgument(Description = "Path to the RectTransform", Required = true)] string path,
            [McpArgument(Description = "Anchored position")] UnityEngine.Vector2 anchoredPosition,
            [McpArgument(Description = "Size delta")] UnityEngine.Vector2 sizeDelta = default)
        {
            var obj = GameObject.Find(path);
            if (obj == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"GameObject '{path}' not found" }
                    }
                };
            }

            var rectTransform = obj.GetComponent<UnityEngine.RectTransform>();
            if (rectTransform == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"GameObject '{path}' has no RectTransform" }
                    }
                };
            }

            rectTransform.anchoredPosition = anchoredPosition;
            if (sizeDelta != UnityEngine.Vector2.zero)
            {
                rectTransform.sizeDelta = sizeDelta;
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Set UI '{path}': position {anchoredPosition}, size {sizeDelta}" }
                }
            };
        }

        [McpServerTool("look_at", Description = "Make a GameObject look at a target point")]
        public static CallToolResult LookAt(
            [McpArgument(Description = "Path to the GameObject", Required = true)] string path,
            [McpArgument(Description = "Target world position to look at")] UnityEngine.Vector3 target,
            [McpArgument(Description = "Up direction")] UnityEngine.Vector3 worldUp = default)
        {
            var obj = GameObject.Find(path);
            if (obj == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"GameObject '{path}' not found" }
                    }
                };
            }

            if (worldUp == UnityEngine.Vector3.zero)
            {
                worldUp = UnityEngine.Vector3.up;
            }

            obj.transform.LookAt(target, worldUp);

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"'{path}' now looks at {target}" }
                }
            };
        }
    }
}
