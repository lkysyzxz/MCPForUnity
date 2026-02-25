#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModelContextProtocol.Editor
{
    public static partial class EditorToolsList
    {
        [McpServerTool("GetSceneHierarchy", Description = "Get scene hierarchy with root GameObjects and their children. Returns a tree structure of all GameObjects in the current scene.")]
        public static CallToolResult GetSceneHierarchy(
            [McpArgument(Description = "Maximum depth to traverse. Default is 3. Use -1 for unlimited depth.", Required = false)] int maxDepth = 3)
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            var result = new JObject
            {
                ["sceneName"] = scene.name,
                ["scenePath"] = scene.path,
                ["rootCount"] = rootObjects.Length,
                ["hierarchy"] = BuildHierarchyArray(rootObjects, 0, maxDepth)
            };

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = result.ToString() }
                }
            };
        }

        [McpServerTool("FindGameObjects", Description = "Find GameObjects in the scene by name, tag, or layer. Returns a list of matching GameObjects with their paths and basic info.")]
        public static CallToolResult FindGameObjects(
            [McpArgument(Description = "Search criteria: 'name', 'tag', or 'layer'", Required = true)] string by,
            [McpArgument(Description = "Value to search for. For 'layer', you can use layer name or layer index.", Required = true)] string value)
        {
            GameObject[] results;
            string searchType = by?.ToLower()?.Trim();

            switch (searchType)
            {
                case "name":
                    results = FindByName(value);
                    break;
                case "tag":
                    results = FindByTag(value);
                    break;
                case "layer":
                    results = FindByLayer(value);
                    break;
                default:
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"Invalid search criteria '{by}'. Use 'name', 'tag', or 'layer'." }
                        }
                    };
            }

            if (results == null || results.Length == 0)
            {
                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"No GameObjects found with {by}='{value}'" }
                    }
                };
            }

            var foundObjects = new JArray();
            foreach (var obj in results)
            {
                if (obj != null)
                {
                    foundObjects.Add(new JObject
                    {
                        ["name"] = obj.name,
                        ["path"] = GetGameObjectPath(obj),
                        ["active"] = obj.activeSelf,
                        ["tag"] = obj.tag,
                        ["layer"] = LayerMask.LayerToName(obj.layer)
                    });
                }
            }

            var resultObj = new JObject
            {
                ["searchBy"] = by,
                ["searchValue"] = value,
                ["count"] = foundObjects.Count,
                ["gameObjects"] = foundObjects
            };

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = resultObj.ToString() }
                }
            };
        }

        [McpServerTool("GetGameObjectInfo", Description = "Get detailed information about a specific GameObject including position, rotation, scale, and component list.")]
        public static CallToolResult GetGameObjectInfo(
            [McpArgument(Description = "Path to the GameObject in the scene hierarchy (e.g., 'Parent/Child/GrandChild')", Required = true)] string path)
        {
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
                        new TextContentBlock { Text = $"GameObject at path '{path}' not found" }
                    }
                };
            }

            var transform = obj.transform;
            var info = new JObject
            {
                ["name"] = obj.name,
                ["path"] = path,
                ["active"] = obj.activeSelf,
                ["activeInHierarchy"] = obj.activeInHierarchy,
                ["isStatic"] = obj.isStatic,
                ["tag"] = obj.tag,
                ["layer"] = LayerMask.LayerToName(obj.layer),
                ["layerIndex"] = obj.layer,
                ["position"] = new JObject
                {
                    ["x"] = transform.position.x,
                    ["y"] = transform.position.y,
                    ["z"] = transform.position.z
                },
                ["rotation"] = new JObject
                {
                    ["x"] = transform.rotation.eulerAngles.x,
                    ["y"] = transform.rotation.eulerAngles.y,
                    ["z"] = transform.rotation.eulerAngles.z
                },
                ["localRotation"] = new JObject
                {
                    ["x"] = transform.localRotation.eulerAngles.x,
                    ["y"] = transform.localRotation.eulerAngles.y,
                    ["z"] = transform.localRotation.eulerAngles.z
                },
                ["localScale"] = new JObject
                {
                    ["x"] = transform.localScale.x,
                    ["y"] = transform.localScale.y,
                    ["z"] = transform.localScale.z
                },
                ["localPosition"] = new JObject
                {
                    ["x"] = transform.localPosition.x,
                    ["y"] = transform.localPosition.y,
                    ["z"] = transform.localPosition.z
                },
                ["childCount"] = transform.childCount,
                ["parent"] = transform.parent?.name
            };

            var componentNames = new JArray();
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp != null)
                {
                    componentNames.Add(comp.GetType().FullName);
                }
            }
            info["componentCount"] = componentNames.Count;
            info["components"] = componentNames;

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = info.ToString() }
                }
            };
        }

        [McpServerTool("GetGameObjectComponents", Description = "Get detailed component information for a specific GameObject.")]
        public static CallToolResult GetGameObjectComponents(
            [McpArgument(Description = "Path to the GameObject in the scene hierarchy", Required = true)] string path)
        {
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
                        new TextContentBlock { Text = $"GameObject at path '{path}' not found" }
                    }
                };
            }

            var components = new JArray();
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp == null) continue;

                var compInfo = new JObject
                {
                    ["type"] = comp.GetType().FullName,
                    ["typeName"] = comp.GetType().Name
                };

                if (comp is Transform t)
                {
                    compInfo["properties"] = new JObject
                    {
                        ["position"] = $"{t.position.x}, {t.position.y}, {t.position.z}",
                        ["rotation"] = $"{t.rotation.eulerAngles.x}, {t.rotation.eulerAngles.y}, {t.rotation.eulerAngles.z}",
                        ["scale"] = $"{t.localScale.x}, {t.localScale.y}, {t.localScale.z}"
                    };
                }
                else if (comp is Renderer r)
                {
                    compInfo["properties"] = new JObject
                    {
                        ["enabled"] = r.enabled,
                        ["bounds"] = $"center: {r.bounds.center}, size: {r.bounds.size}"
                    };
                }
                else if (comp is Collider c)
                {
                    compInfo["properties"] = new JObject
                    {
                        ["enabled"] = c.enabled,
                        ["isTrigger"] = c.isTrigger
                    };
                }
                else if (comp is Rigidbody rb)
                {
                    compInfo["properties"] = new JObject
                    {
                        ["mass"] = rb.mass,
                        ["useGravity"] = rb.useGravity,
                        ["isKinematic"] = rb.isKinematic
                    };
                }
                else if (comp is MonoBehaviour mb)
                {
                    compInfo["properties"] = new JObject
                    {
                        ["enabled"] = mb.enabled
                    };
                }

                components.Add(compInfo);
            }

            var result = new JObject
            {
                ["gameObjectPath"] = path,
                ["gameObjectName"] = obj.name,
                ["componentCount"] = components.Count,
                ["components"] = components
            };

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = result.ToString() }
                }
            };
        }

        private static JArray BuildHierarchyArray(GameObject[] objects, int currentDepth, int maxDepth)
        {
            var array = new JArray();

            if (maxDepth >= 0 && currentDepth >= maxDepth)
            {
                return array;
            }

            foreach (var obj in objects)
            {
                var node = new JObject
                {
                    ["name"] = obj.name,
                    ["active"] = obj.activeSelf,
                    ["tag"] = obj.tag,
                    ["layer"] = LayerMask.LayerToName(obj.layer)
                };

                var children = new List<GameObject>();
                foreach (Transform child in obj.transform)
                {
                    children.Add(child.gameObject);
                }

                if (children.Count > 0)
                {
                    node["children"] = BuildHierarchyArray(children.ToArray(), currentDepth + 1, maxDepth);
                    node["childCount"] = children.Count;
                }

                array.Add(node);
            }

            return array;
        }

        private static GameObject[] FindByName(string name)
        {
            var results = new List<GameObject>();
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj != null && obj.name == name)
                {
                    results.Add(obj);
                }
            }
            return results.ToArray();
        }

        private static GameObject[] FindByTag(string tag)
        {
            try
            {
                return GameObject.FindGameObjectsWithTag(tag);
            }
            catch
            {
                return new GameObject[0];
            }
        }

        private static GameObject[] FindByLayer(string value)
        {
            int layerIndex;
            if (int.TryParse(value, out layerIndex))
            {
            }
            else
            {
                layerIndex = LayerMask.NameToLayer(value);
            }

            if (layerIndex < 0 || layerIndex > 31)
            {
                return new GameObject[0];
            }

            var results = new List<GameObject>();
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj != null && obj.layer == layerIndex)
                {
                    results.Add(obj);
                }
            }
            return results.ToArray();
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
    }
}
#endif
