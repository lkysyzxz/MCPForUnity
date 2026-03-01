#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModelContextProtocol.Editor
{
    public static partial class EditorToolsList
    {
        #region Scene Management (10 tools)

        [McpServerTool("EditorGetScenesInBuild", Description = "Get list of scenes in Build Settings")]
        public static CallToolResult GetScenesInBuild()
        {
            var scenes = new JArray();
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                var scene = EditorBuildSettings.scenes[i];
                scenes.Add(new JObject
                {
                    ["index"] = i,
                    ["path"] = scene.path,
                    ["enabled"] = scene.enabled,
                    ["name"] = System.IO.Path.GetFileNameWithoutExtension(scene.path)
                });
            }

            return SuccessResult(new JObject
            {
                ["count"] = scenes.Count,
                ["scenes"] = scenes
            });
        }

        [McpServerTool("EditorGetActiveScene", Description = "Get current active scene information")]
        public static CallToolResult GetActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            return SuccessResult(new JObject
            {
                ["name"] = scene.name,
                ["path"] = scene.path,
                ["buildIndex"] = scene.buildIndex,
                ["isLoaded"] = scene.isLoaded,
                ["isDirty"] = scene.isDirty,
                ["rootCount"] = scene.rootCount,
                ["gameObjectCount"] = scene.GetRootGameObjects().Sum(go => CountGameObjectsRecursive(go))
            });
        }

        [McpServerTool("EditorLoadScene", Description = "Load a scene by path")]
        public static CallToolResult LoadScene(
            [McpArgument(Description = "Scene path (e.g., Assets/Scenes/MyScene.unity)", Required = true)] string scenePath,
            [McpArgument(Description = "Load additively (keep current scene)", Required = false)] bool additive = false)
        {
            if (string.IsNullOrEmpty(scenePath))
                return ErrorResult("Parameter 'scenePath' is required");

            try
            {
                var mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
                var scene = EditorSceneManager.OpenScene(scenePath, mode);
                return SuccessResult(new JObject
                {
                    ["success"] = true,
                    ["sceneName"] = scene.name,
                    ["scenePath"] = scene.path
                });
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to load scene: {ex.Message}");
            }
        }

        [McpServerTool("EditorCreateScene", Description = "Create a new scene")]
        public static CallToolResult CreateScene(
            [McpArgument(Description = "Scene name", Required = true)] string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return ErrorResult("Parameter 'sceneName' is required");

            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                scene.name = sceneName;
                return SuccessResult(new JObject
                {
                    ["success"] = true,
                    ["sceneName"] = scene.name
                });
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to create scene: {ex.Message}");
            }
        }

        [McpServerTool("EditorSaveScene", Description = "Save current scene")]
        public static CallToolResult SaveScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(scene.path))
                {
                    return ErrorResult("Scene has no path. Use EditorSaveSceneAs to save as a new file.");
                }

                bool success = EditorSceneManager.SaveScene(scene);
                return SuccessResult(new JObject
                {
                    ["success"] = success,
                    ["scenePath"] = scene.path
                });
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to save scene: {ex.Message}");
            }
        }

        [McpServerTool("EditorSaveSceneAs", Description = "Save scene to a new path")]
        public static CallToolResult SaveSceneAs(
            [McpArgument(Description = "Destination path (e.g., Assets/Scenes/NewScene.unity)", Required = true)] string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return ErrorResult("Parameter 'scenePath' is required");

            try
            {
                var scene = SceneManager.GetActiveScene();
                bool success = EditorSceneManager.SaveScene(scene, scenePath);
                return SuccessResult(new JObject
                {
                    ["success"] = success,
                    ["scenePath"] = scene.path
                });
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to save scene: {ex.Message}");
            }
        }

        [McpServerTool("EditorCloseScene", Description = "Close a scene")]
        public static CallToolResult CloseScene(
            [McpArgument(Description = "Scene path to close", Required = true)] string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return ErrorResult("Parameter 'scenePath' is required");

            try
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.path == scenePath)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                        return SuccessResult(new JObject
                        {
                            ["success"] = true,
                            ["closedScene"] = scenePath
                        });
                    }
                }
                return ErrorResult($"Scene '{scenePath}' not found in loaded scenes");
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to close scene: {ex.Message}");
            }
        }

        [McpServerTool("EditorAddSceneToBuild", Description = "Add scene to Build Settings")]
        public static CallToolResult AddSceneToBuild(
            [McpArgument(Description = "Scene path to add", Required = true)] string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return ErrorResult("Parameter 'scenePath' is required");

            if (!File.Exists(scenePath))
                return ErrorResult($"Scene file not found: {scenePath}");

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath))
                return ErrorResult($"Scene already in Build Settings: {scenePath}");

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["scenePath"] = scenePath,
                ["totalScenes"] = scenes.Count
            });
        }

        [McpServerTool("EditorRemoveSceneFromBuild", Description = "Remove scene from Build Settings")]
        public static CallToolResult RemoveSceneFromBuild(
            [McpArgument(Description = "Scene path to remove", Required = true)] string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return ErrorResult("Parameter 'scenePath' is required");

            var scenes = EditorBuildSettings.scenes.ToList();
            var scene = scenes.FirstOrDefault(s => s.path == scenePath);
            if (scene == null)
                return ErrorResult($"Scene not found in Build Settings: {scenePath}");

            scenes.Remove(scene);
            EditorBuildSettings.scenes = scenes.ToArray();

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["removedScene"] = scenePath,
                ["totalScenes"] = scenes.Count
            });
        }

        [McpServerTool("EditorGetSceneHierarchy", Description = "Get scene hierarchy structure")]
        public static CallToolResult GetSceneHierarchy(
            [McpArgument(Description = "Root path (optional, defaults to scene root)", Required = false)] string rootPath = null,
            [McpArgument(Description = "Maximum depth to traverse (default 5)", Required = false)] int maxDepth = 5)
        {
            var scene = SceneManager.GetActiveScene();
            GameObject[] rootObjects;

            if (!string.IsNullOrEmpty(rootPath))
            {
                var rootObj = GameObject.Find(rootPath);
                if (rootObj == null)
                    return ErrorResult($"GameObject not found: {rootPath}");
                rootObjects = new[] { rootObj };
            }
            else
            {
                rootObjects = scene.GetRootGameObjects();
            }

            var result = new JObject
            {
                ["sceneName"] = scene.name,
                ["rootCount"] = rootObjects.Length,
                ["hierarchy"] = BuildHierarchyArray(rootObjects, 0, maxDepth)
            };

            return SuccessResult(result);
        }

        #endregion

        #region GameObject Operations (15 tools)

        [McpServerTool("EditorCreateGameObject", Description = "Create an empty GameObject")]
        public static CallToolResult CreateGameObject(
            [McpArgument(Description = "GameObject name", Required = true)] string name,
            [McpArgument(Description = "Parent path (optional)", Required = false)] string parentPath = null,
            [McpArgument(Description = "Position X", Required = false)] float position_x = 0,
            [McpArgument(Description = "Position Y", Required = false)] float position_y = 0,
            [McpArgument(Description = "Position Z", Required = false)] float position_z = 0)
        {
            if (string.IsNullOrEmpty(name))
                return ErrorResult("Parameter 'name' is required");

            var go = new GameObject(name);
            go.transform.position = new Vector3(position_x, position_y, position_z);

            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent == null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    return ErrorResult($"Parent GameObject not found: {parentPath}");
                }
                go.transform.SetParent(parent.transform);
            }

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["name"] = go.name,
                ["path"] = GetGameObjectPath(go)
            });
        }

        [McpServerTool("EditorCreatePrimitive", Description = "Create a primitive GameObject")]
        public static CallToolResult CreatePrimitive(
            [McpArgument(Description = "Primitive type: Cube, Sphere, Capsule, Cylinder, Plane, Quad", Required = true)] string type,
            [McpArgument(Description = "GameObject name (optional)", Required = false)] string name = null,
            [McpArgument(Description = "Position X", Required = false)] float position_x = 0,
            [McpArgument(Description = "Position Y", Required = false)] float position_y = 0,
            [McpArgument(Description = "Position Z", Required = false)] float position_z = 0)
        {
            if (string.IsNullOrEmpty(type))
                return ErrorResult("Parameter 'type' is required");

            if (!Enum.TryParse<PrimitiveType>(type, true, out var primitiveType))
                return ErrorResult($"Invalid primitive type: {type}. Valid types: Cube, Sphere, Capsule, Cylinder, Plane, Quad");

            var go = GameObject.CreatePrimitive(primitiveType);
            if (!string.IsNullOrEmpty(name))
                go.name = name;

            go.transform.position = new Vector3(position_x, position_y, position_z);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["name"] = go.name,
                ["type"] = primitiveType.ToString(),
                ["path"] = GetGameObjectPath(go)
            });
        }

        [McpServerTool("EditorDuplicateGameObject", Description = "Duplicate a GameObject")]
        public static CallToolResult DuplicateGameObject(
            [McpArgument(Description = "GameObject path", Required = true)] string path)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var duplicate = UnityEngine.Object.Instantiate(obj, obj.transform.parent);
            duplicate.name = obj.name;
            duplicate.transform.SetSiblingIndex(obj.transform.GetSiblingIndex() + 1);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["originalPath"] = path,
                ["duplicatePath"] = GetGameObjectPath(duplicate)
            });
        }

        [McpServerTool("EditorDeleteGameObject", Description = "Delete a GameObject")]
        public static CallToolResult DeleteGameObject(
            [McpArgument(Description = "GameObject path", Required = true)] string path)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            UnityEngine.Object.DestroyImmediate(obj);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["deletedPath"] = path
            });
        }

        [McpServerTool("EditorFindGameObject", Description = "Find a GameObject by path")]
        public static CallToolResult FindGameObject(
            [McpArgument(Description = "GameObject path", Required = true)] string path)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            return SuccessResult(GetGameObjectInfoJson(obj));
        }

        [McpServerTool("EditorFindGameObjectsByName", Description = "Find GameObjects by name")]
        public static CallToolResult FindGameObjectsByName(
            [McpArgument(Description = "GameObject name to search", Required = true)] string name,
            [McpArgument(Description = "Include inactive objects", Required = false)] bool includeInactive = true)
        {
            if (string.IsNullOrEmpty(name))
                return ErrorResult("Parameter 'name' is required");

            var results = new JArray();
            var allObjects = includeInactive
                ? Resources.FindObjectsOfTypeAll<GameObject>()
                : UnityEngine.Object.FindObjectsOfType<GameObject>();

            foreach (var obj in allObjects)
            {
                if (obj != null && obj.name == name && IsInScene(obj))
                {
                    results.Add(GetGameObjectSummaryJson(obj));
                }
            }

            return SuccessResult(new JObject
            {
                ["searchName"] = name,
                ["count"] = results.Count,
                ["gameObjects"] = results
            });
        }

        [McpServerTool("EditorFindGameObjectsByTag", Description = "Find GameObjects by tag")]
        public static CallToolResult FindGameObjectsByTag(
            [McpArgument(Description = "Tag to search", Required = true)] string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return ErrorResult("Parameter 'tag' is required");

            try
            {
                var objects = GameObject.FindGameObjectsWithTag(tag);
                var results = new JArray();
                foreach (var obj in objects)
                {
                    results.Add(GetGameObjectSummaryJson(obj));
                }

                return SuccessResult(new JObject
                {
                    ["tag"] = tag,
                    ["count"] = results.Count,
                    ["gameObjects"] = results
                });
            }
            catch (UnityException)
            {
                return ErrorResult($"Tag '{tag}' is not defined");
            }
        }

        [McpServerTool("EditorFindGameObjectsByLayer", Description = "Find GameObjects by layer")]
        public static CallToolResult FindGameObjectsByLayer(
            [McpArgument(Description = "Layer name or index", Required = true)] string layer)
        {
            int layerIndex;
            if (!int.TryParse(layer, out layerIndex))
            {
                layerIndex = LayerMask.NameToLayer(layer);
            }

            if (layerIndex < 0 || layerIndex > 31)
                return ErrorResult($"Invalid layer: {layer}");

            var results = new JArray();
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

            foreach (var obj in allObjects)
            {
                if (obj != null && obj.layer == layerIndex)
                {
                    results.Add(GetGameObjectSummaryJson(obj));
                }
            }

            return SuccessResult(new JObject
            {
                ["layer"] = LayerMask.LayerToName(layerIndex),
                ["layerIndex"] = layerIndex,
                ["count"] = results.Count,
                ["gameObjects"] = results
            });
        }

        [McpServerTool("EditorGetGameObjectInfo", Description = "Get detailed GameObject information")]
        public static CallToolResult GetGameObjectInfo(
            [McpArgument(Description = "GameObject path", Required = true)] string path)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            return SuccessResult(GetGameObjectInfoJson(obj));
        }

        [McpServerTool("EditorGetChildren", Description = "Get children of a GameObject")]
        public static CallToolResult GetChildren(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Get all descendants recursively", Required = false)] bool recursive = false)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var children = new JArray();
            CollectChildren(obj.transform, children, recursive);

            return SuccessResult(new JObject
            {
                ["parentPath"] = path,
                ["childCount"] = obj.transform.childCount,
                ["children"] = children
            });
        }

        [McpServerTool("EditorSetParent", Description = "Set parent of a GameObject")]
        public static CallToolResult SetParent(
            [McpArgument(Description = "Child GameObject path", Required = true)] string childPath,
            [McpArgument(Description = "New parent path (null for root)", Required = false)] string parentPath = null)
        {
            var child = FindGameObjectByPath(childPath);
            if (child == null)
                return ErrorResult($"Child GameObject not found: {childPath}");

            Transform newParent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentObj = FindGameObjectByPath(parentPath);
                if (parentObj == null)
                    return ErrorResult($"Parent GameObject not found: {parentPath}");
                newParent = parentObj.transform;
            }

            child.transform.SetParent(newParent);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["childPath"] = childPath,
                ["newParentPath"] = parentPath,
                ["newPath"] = GetGameObjectPath(child)
            });
        }

        [McpServerTool("EditorSetTag", Description = "Set tag of a GameObject")]
        public static CallToolResult SetTag(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Tag name", Required = true)] string tag)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            try
            {
                obj.tag = tag;
                return SuccessResult(new JObject
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["tag"] = tag
                });
            }
            catch (UnityException ex)
            {
                return ErrorResult($"Failed to set tag: {ex.Message}");
            }
        }

        [McpServerTool("EditorSetLayer", Description = "Set layer of a GameObject")]
        public static CallToolResult SetLayer(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Layer name or index", Required = true)] string layer)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            int layerIndex;
            if (!int.TryParse(layer, out layerIndex))
            {
                layerIndex = LayerMask.NameToLayer(layer);
            }

            if (layerIndex < 0 || layerIndex > 31)
                return ErrorResult($"Invalid layer: {layer}");

            obj.layer = layerIndex;

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["layer"] = LayerMask.LayerToName(layerIndex),
                ["layerIndex"] = layerIndex
            });
        }

        [McpServerTool("EditorSetStatic", Description = "Set static flag of a GameObject")]
        public static CallToolResult SetStatic(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Is static", Required = true)] bool isStatic)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            obj.isStatic = isStatic;

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["isStatic"] = isStatic
            });
        }

        [McpServerTool("EditorSetActive", Description = "Set active state of a GameObject")]
        public static CallToolResult SetActive(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Active state", Required = true)] bool active)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            obj.SetActive(active);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["active"] = active
            });
        }

        #endregion

        #region Transform Operations (10 tools)

        [McpServerTool("EditorSetPosition", Description = "Set world position of a GameObject")]
        public static CallToolResult SetPosition(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Position X", Required = false)] float x = 0,
            [McpArgument(Description = "Position Y", Required = false)] float y = 0,
            [McpArgument(Description = "Position Z", Required = false)] float z = 0)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            Undo.RecordObject(obj.transform, "Set Position");
            obj.transform.position = new Vector3(x, y, z);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["position"] = VectorToJson(obj.transform.position)
            });
        }

        [McpServerTool("EditorSetRotation", Description = "Set world rotation of a GameObject (Euler angles)")]
        public static CallToolResult SetRotation(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Rotation X (degrees)", Required = false)] float x = 0,
            [McpArgument(Description = "Rotation Y (degrees)", Required = false)] float y = 0,
            [McpArgument(Description = "Rotation Z (degrees)", Required = false)] float z = 0)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            Undo.RecordObject(obj.transform, "Set Rotation");
            obj.transform.rotation = Quaternion.Euler(x, y, z);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["rotation"] = VectorToJson(obj.transform.rotation.eulerAngles)
            });
        }

        [McpServerTool("EditorSetScale", Description = "Set scale of a GameObject")]
        public static CallToolResult SetScale(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Scale X", Required = false)] float x = 1,
            [McpArgument(Description = "Scale Y", Required = false)] float y = 1,
            [McpArgument(Description = "Scale Z", Required = false)] float z = 1)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            Undo.RecordObject(obj.transform, "Set Scale");
            obj.transform.localScale = new Vector3(x, y, z);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["scale"] = VectorToJson(obj.transform.localScale)
            });
        }

        [McpServerTool("EditorSetLocalPosition", Description = "Set local position of a GameObject")]
        public static CallToolResult SetLocalPosition(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Local position X", Required = false)] float x = 0,
            [McpArgument(Description = "Local position Y", Required = false)] float y = 0,
            [McpArgument(Description = "Local position Z", Required = false)] float z = 0)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            Undo.RecordObject(obj.transform, "Set Local Position");
            obj.transform.localPosition = new Vector3(x, y, z);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["localPosition"] = VectorToJson(obj.transform.localPosition)
            });
        }

        [McpServerTool("EditorSetLocalRotation", Description = "Set local rotation of a GameObject")]
        public static CallToolResult SetLocalRotation(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Local rotation X (degrees)", Required = false)] float x = 0,
            [McpArgument(Description = "Local rotation Y (degrees)", Required = false)] float y = 0,
            [McpArgument(Description = "Local rotation Z (degrees)", Required = false)] float z = 0)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            Undo.RecordObject(obj.transform, "Set Local Rotation");
            obj.transform.localRotation = Quaternion.Euler(x, y, z);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["localRotation"] = VectorToJson(obj.transform.localRotation.eulerAngles)
            });
        }

        [McpServerTool("EditorSetLocalScale", Description = "Set local scale of a GameObject")]
        public static CallToolResult SetLocalScale(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Local scale X", Required = false)] float x = 1,
            [McpArgument(Description = "Local scale Y", Required = false)] float y = 1,
            [McpArgument(Description = "Local scale Z", Required = false)] float z = 1)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            Undo.RecordObject(obj.transform, "Set Local Scale");
            obj.transform.localScale = new Vector3(x, y, z);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["localScale"] = VectorToJson(obj.transform.localScale)
            });
        }

        [McpServerTool("EditorSetPositionAndRotation", Description = "Set position and rotation at once")]
        public static CallToolResult SetPositionAndRotation(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Position X", Required = false)] float posX = 0,
            [McpArgument(Description = "Position Y", Required = false)] float posY = 0,
            [McpArgument(Description = "Position Z", Required = false)] float posZ = 0,
            [McpArgument(Description = "Rotation X (degrees)", Required = false)] float rotX = 0,
            [McpArgument(Description = "Rotation Y (degrees)", Required = false)] float rotY = 0,
            [McpArgument(Description = "Rotation Z (degrees)", Required = false)] float rotZ = 0)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            Undo.RecordObject(obj.transform, "Set Position and Rotation");
            obj.transform.SetPositionAndRotation(
                new Vector3(posX, posY, posZ),
                Quaternion.Euler(rotX, rotY, rotZ)
            );

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["position"] = VectorToJson(obj.transform.position),
                ["rotation"] = VectorToJson(obj.transform.rotation.eulerAngles)
            });
        }

        [McpServerTool("EditorTranslate", Description = "Move GameObject relative to current position")]
        public static CallToolResult Translate(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Translation X", Required = false)] float x = 0,
            [McpArgument(Description = "Translation Y", Required = false)] float y = 0,
            [McpArgument(Description = "Translation Z", Required = false)] float z = 0,
            [McpArgument(Description = "Relative to: World or Self", Required = false)] string relativeTo = "World")
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var space = relativeTo.ToLower() == "self" ? Space.Self : Space.World;

            Undo.RecordObject(obj.transform, "Translate");
            obj.transform.Translate(new Vector3(x, y, z), space);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["position"] = VectorToJson(obj.transform.position)
            });
        }

        [McpServerTool("EditorRotate", Description = "Rotate GameObject relative to current rotation")]
        public static CallToolResult Rotate(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Rotation X (degrees)", Required = false)] float x = 0,
            [McpArgument(Description = "Rotation Y (degrees)", Required = false)] float y = 0,
            [McpArgument(Description = "Rotation Z (degrees)", Required = false)] float z = 0,
            [McpArgument(Description = "Relative to: World or Self", Required = false)] string relativeTo = "World")
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var space = relativeTo.ToLower() == "self" ? Space.Self : Space.World;

            Undo.RecordObject(obj.transform, "Rotate");
            obj.transform.Rotate(new Vector3(x, y, z), space);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["rotation"] = VectorToJson(obj.transform.rotation.eulerAngles)
            });
        }

        [McpServerTool("EditorResetTransform", Description = "Reset transform to default values")]
        public static CallToolResult ResetTransform(
            [McpArgument(Description = "GameObject path", Required = true)] string path)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            Undo.RecordObject(obj.transform, "Reset Transform");
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["localPosition"] = VectorToJson(obj.transform.localPosition),
                ["localRotation"] = VectorToJson(obj.transform.localRotation.eulerAngles),
                ["localScale"] = VectorToJson(obj.transform.localScale)
            });
        }

        #endregion

        #region Component Operations (10 tools)

        [McpServerTool("EditorAddComponent", Description = "Add a component to a GameObject")]
        public static CallToolResult AddComponent(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Component type name (e.g., Rigidbody, BoxCollider)", Required = true)] string componentType)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var type = FindType(componentType);
            if (type == null)
                return ErrorResult($"Component type not found: {componentType}");

            try
            {
                var component = Undo.AddComponent(obj, type);
                return SuccessResult(new JObject
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["componentType"] = type.FullName
                });
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to add component: {ex.Message}");
            }
        }

        [McpServerTool("EditorRemoveComponent", Description = "Remove a component from a GameObject")]
        public static CallToolResult RemoveComponent(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Component type name", Required = true)] string componentType)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var type = FindType(componentType);
            if (type == null)
                return ErrorResult($"Component type not found: {componentType}");

            var component = obj.GetComponent(type);
            if (component == null)
                return ErrorResult($"Component not found on GameObject: {componentType}");

            if (component is Transform)
                return ErrorResult("Cannot remove Transform component");

            Undo.DestroyObjectImmediate(component);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = path,
                ["removedComponent"] = type.FullName
            });
        }

        [McpServerTool("EditorGetComponent", Description = "Get component information")]
        public static CallToolResult GetComponent(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Component type name", Required = true)] string componentType)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var type = FindType(componentType);
            if (type == null)
                return ErrorResult($"Component type not found: {componentType}");

            var component = obj.GetComponent(type);
            if (component == null)
                return ErrorResult($"Component not found: {componentType}");

            return SuccessResult(new JObject
            {
                ["path"] = path,
                ["componentType"] = type.FullName,
                ["properties"] = GetComponentProperties(component)
            });
        }

        [McpServerTool("EditorGetComponents", Description = "Get all components of a GameObject")]
        public static CallToolResult GetComponents(
            [McpArgument(Description = "GameObject path", Required = true)] string path)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var components = new JArray();
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp == null) continue;
                components.Add(new JObject
                {
                    ["type"] = comp.GetType().FullName,
                    ["typeName"] = comp.GetType().Name
                });
            }

            return SuccessResult(new JObject
            {
                ["path"] = path,
                ["count"] = components.Count,
                ["components"] = components
            });
        }

        [McpServerTool("EditorGetComponentsInChildren", Description = "Get components in children")]
        public static CallToolResult GetComponentsInChildren(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Component type name", Required = true)] string componentType,
            [McpArgument(Description = "Include inactive", Required = false)] bool includeInactive = false)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var type = FindType(componentType);
            if (type == null)
                return ErrorResult($"Component type not found: {componentType}");

            var components = obj.GetComponentsInChildren(type, includeInactive);
            var results = new JArray();

            foreach (var comp in components)
            {
                if (comp == null) continue;
                var go = comp.gameObject;
                results.Add(new JObject
                {
                    ["path"] = GetGameObjectPath(go),
                    ["type"] = comp.GetType().Name
                });
            }

            return SuccessResult(new JObject
            {
                ["path"] = path,
                ["componentType"] = type.Name,
                ["count"] = results.Count,
                ["components"] = results
            });
        }

        [McpServerTool("EditorSetComponentProperty", Description = "Set a property on a component")]
        public static CallToolResult SetComponentProperty(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Component type name", Required = true)] string componentType,
            [McpArgument(Description = "Property name", Required = true)] string propertyName,
            [McpArgument(Description = "Property value (JSON format for complex types)", Required = true)] string value)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var type = FindType(componentType);
            if (type == null)
                return ErrorResult($"Component type not found: {componentType}");

            var component = obj.GetComponent(type);
            if (component == null)
                return ErrorResult($"Component not found: {componentType}");

            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                    return ErrorResult($"Property or field not found: {propertyName}");

                try
                {
                    var convertedValue = ConvertValue(value, field.FieldType);
                    Undo.RecordObject(component, $"Set {propertyName}");
                    field.SetValue(component, convertedValue);
                    EditorUtility.SetDirty(component);

                    return SuccessResult(new JObject
                    {
                        ["success"] = true,
                        ["path"] = path,
                        ["component"] = type.Name,
                        ["property"] = propertyName,
                        ["value"] = value
                    });
                }
                catch (Exception ex)
                {
                    return ErrorResult($"Failed to set field: {ex.Message}");
                }
            }

            try
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                Undo.RecordObject(component, $"Set {propertyName}");
                property.SetValue(component, convertedValue);
                EditorUtility.SetDirty(component);

                return SuccessResult(new JObject
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["component"] = type.Name,
                    ["property"] = propertyName,
                    ["value"] = value
                });
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to set property: {ex.Message}");
            }
        }

        [McpServerTool("EditorGetComponentProperty", Description = "Get a property value from a component")]
        public static CallToolResult GetComponentProperty(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Component type name", Required = true)] string componentType,
            [McpArgument(Description = "Property name", Required = true)] string propertyName)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var type = FindType(componentType);
            if (type == null)
                return ErrorResult($"Component type not found: {componentType}");

            var component = obj.GetComponent(type);
            if (component == null)
                return ErrorResult($"Component not found: {componentType}");

            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                    return ErrorResult($"Property or field not found: {propertyName}");

                var value = field.GetValue(component);
                return SuccessResult(new JObject
                {
                    ["path"] = path,
                    ["component"] = type.Name,
                    ["property"] = propertyName,
                    ["value"] = value?.ToString() ?? "null",
                    ["type"] = field.FieldType.Name
                });
            }

            try
            {
                var value = property.GetValue(component);
                return SuccessResult(new JObject
                {
                    ["path"] = path,
                    ["component"] = type.Name,
                    ["property"] = propertyName,
                    ["value"] = value?.ToString() ?? "null",
                    ["type"] = property.PropertyType.Name
                });
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to get property: {ex.Message}");
            }
        }

        [McpServerTool("EditorHasComponent", Description = "Check if GameObject has a component")]
        public static CallToolResult HasComponent(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Component type name", Required = true)] string componentType)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var type = FindType(componentType);
            if (type == null)
                return ErrorResult($"Component type not found: {componentType}");

            var hasComponent = obj.GetComponent(type) != null;

            return SuccessResult(new JObject
            {
                ["path"] = path,
                ["componentType"] = type.Name,
                ["hasComponent"] = hasComponent
            });
        }

        [McpServerTool("EditorGetRequiredComponent", Description = "Get or add a component")]
        public static CallToolResult GetRequiredComponent(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Component type name", Required = true)] string componentType)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var type = FindType(componentType);
            if (type == null)
                return ErrorResult($"Component type not found: {componentType}");

            var component = obj.GetComponent(type);
            bool wasAdded = false;

            if (component == null)
            {
                component = Undo.AddComponent(obj, type);
                wasAdded = true;
            }

            return SuccessResult(new JObject
            {
                ["path"] = path,
                ["componentType"] = type.FullName,
                ["wasAdded"] = wasAdded,
                ["properties"] = GetComponentProperties(component)
            });
        }

        [McpServerTool("EditorSendComponentMessage", Description = "Send a message to a component")]
        public static CallToolResult SendComponentMessage(
            [McpArgument(Description = "GameObject path", Required = true)] string path,
            [McpArgument(Description = "Method name to call", Required = true)] string methodName,
            [McpArgument(Description = "Optional parameter value", Required = false)] string value = null)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    obj.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    obj.SendMessage(methodName, value, SendMessageOptions.DontRequireReceiver);
                }

                return SuccessResult(new JObject
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["methodName"] = methodName,
                    ["value"] = value
                });
            }
            catch (Exception ex)
            {
                return ErrorResult($"Failed to send message: {ex.Message}");
            }
        }

        #endregion

        #region Compilation & Build (10 tools)

        [McpServerTool("EditorGetCompilationStatus", Description = "Get compilation status")]
        public static CallToolResult GetCompilationStatus()
        {
            return SuccessResult(new JObject
            {
                ["isCompiling"] = EditorApplication.isCompiling,
                ["isPlaying"] = EditorApplication.isPlaying,
                ["isPaused"] = EditorApplication.isPaused
            });
        }

        [McpServerTool("EditorIsCompiling", Description = "Check if scripts are compiling")]
        public static CallToolResult IsCompiling()
        {
            return SuccessResult(new JObject
            {
                ["isCompiling"] = EditorApplication.isCompiling
            });
        }

        [McpServerTool("EditorGetCompileErrors", Description = "Get compilation errors from console")]
        public static CallToolResult GetCompileErrors()
        {
            var errors = new JArray();
            var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            if (logEntries != null)
            {
                var getCountMethod = logEntries.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                var getEntryMethod = logEntries.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);

                if (getCountMethod != null && getEntryMethod != null)
                {
                    int count = (int)getCountMethod.Invoke(null, null);
                    for (int i = 0; i < count && i < 100; i++)
                    {
                        var entry = getEntryMethod.Invoke(null, new object[] { i, null });
                        if (entry != null)
                        {
                            var mode = entry.GetType().GetField("mode", BindingFlags.Public | BindingFlags.Instance);
                            var condition = entry.GetType().GetField("condition", BindingFlags.Public | BindingFlags.Instance);
                            
                            if (mode != null && condition != null)
                            {
                                int modeValue = (int)mode.GetValue(entry);
                                if ((modeValue & (1 << 0)) != 0) // Error flag
                                {
                                    errors.Add(new JObject
                                    {
                                        ["message"] = condition.GetValue(entry)?.ToString()
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return SuccessResult(new JObject
            {
                ["count"] = errors.Count,
                ["errors"] = errors
            });
        }

        [McpServerTool("EditorGetCompileWarnings", Description = "Get compilation warnings from console")]
        public static CallToolResult GetCompileWarnings()
        {
            return SuccessResult(new JObject
            {
                ["message"] = "Warning extraction requires reflection into Unity internals",
                ["count"] = 0,
                ["warnings"] = new JArray()
            });
        }

        [McpServerTool("EditorGetBuildTarget", Description = "Get current build target")]
        public static CallToolResult GetBuildTarget()
        {
            return SuccessResult(new JObject
            {
                ["buildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["buildTargetGroup"] = EditorUserBuildSettings.selectedBuildTargetGroup.ToString()
            });
        }

        [McpServerTool("EditorSetBuildTarget", Description = "Set build target")]
        public static CallToolResult SetBuildTarget(
            [McpArgument(Description = "Build target (e.g., StandaloneWindows, Android, iOS)", Required = true)] string buildTarget)
        {
            if (!Enum.TryParse<BuildTarget>(buildTarget, out var target))
                return ErrorResult($"Invalid build target: {buildTarget}");

            var group = BuildTargetGroup.Standalone;
            
            if (target == BuildTarget.Android)
                group = BuildTargetGroup.Android;
            else if (target == BuildTarget.iOS)
                group = BuildTargetGroup.iOS;
            else if (target == BuildTarget.WebGL)
                group = BuildTargetGroup.WebGL;
            else if (target.ToString().Contains("Standalone"))
                group = BuildTargetGroup.Standalone;

            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["buildTarget"] = target.ToString(),
                ["buildTargetGroup"] = group.ToString()
            });
        }

        [McpServerTool("EditorGetScriptingBackend", Description = "Get scripting backend")]
        public static CallToolResult GetScriptingBackend()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var backend = PlayerSettings.GetScriptingBackend(group);

            return SuccessResult(new JObject
            {
                ["buildTargetGroup"] = group.ToString(),
                ["scriptingBackend"] = backend.ToString()
            });
        }

        [McpServerTool("EditorGetAssemblyDefinitions", Description = "Get all assembly definition files")]
        public static CallToolResult GetAssemblyDefinitions()
        {
            var asmdefs = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            var results = new JArray();

            foreach (var guid in asmdefs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                if (asset != null)
                {
                    results.Add(new JObject
                    {
                        ["name"] = asset.name,
                        ["path"] = path
                    });
                }
            }

            return SuccessResult(new JObject
            {
                ["count"] = results.Count,
                ["assemblyDefinitions"] = results
            });
        }

        [McpServerTool("EditorGetDefineSymbols", Description = "Get scripting define symbols")]
        public static CallToolResult GetDefineSymbols(
            [McpArgument(Description = "Build target group (optional)", Required = false)] string buildTargetGroup = null)
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            
            if (!string.IsNullOrEmpty(buildTargetGroup))
            {
                if (!Enum.TryParse<BuildTargetGroup>(buildTargetGroup, out group))
                    return ErrorResult($"Invalid build target group: {buildTargetGroup}");
            }

            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            return SuccessResult(new JObject
            {
                ["buildTargetGroup"] = group.ToString(),
                ["symbols"] = symbols,
                ["symbolList"] = JArray.FromObject(symbols.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToArray())
            });
        }

        [McpServerTool("EditorSetDefineSymbols", Description = "Set scripting define symbols")]
        public static CallToolResult SetDefineSymbols(
            [McpArgument(Description = "Semicolon-separated symbols", Required = true)] string symbols,
            [McpArgument(Description = "Build target group (optional)", Required = false)] string buildTargetGroup = null)
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            
            if (!string.IsNullOrEmpty(buildTargetGroup))
            {
                if (!Enum.TryParse<BuildTargetGroup>(buildTargetGroup, out group))
                    return ErrorResult($"Invalid build target group: {buildTargetGroup}");
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["buildTargetGroup"] = group.ToString(),
                ["symbols"] = symbols
            });
        }

        #endregion

        #region Asset Management (15 tools)

        [McpServerTool("EditorFindAssets", Description = "Find assets in project")]
        public static CallToolResult FindAssets(
            [McpArgument(Description = "Search filter", Required = true)] string filter,
            [McpArgument(Description = "Asset type (optional)", Required = false)] string type = null,
            [McpArgument(Description = "Search folders (optional, comma-separated)", Required = false)] string searchInFolders = null)
        {
            if (string.IsNullOrEmpty(filter))
                return ErrorResult("Parameter 'filter' is required");

            string searchFilter = filter;
            if (!string.IsNullOrEmpty(type))
            {
                searchFilter += $" t:{type}";
            }

            string[] folders = null;
            if (!string.IsNullOrEmpty(searchInFolders))
            {
                folders = searchInFolders.Split(',').Select(f => f.Trim()).ToArray();
            }

            var guids = AssetDatabase.FindAssets(searchFilter, folders);
            var results = new JArray();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                results.Add(new JObject
                {
                    ["guid"] = guid,
                    ["path"] = path,
                    ["name"] = System.IO.Path.GetFileNameWithoutExtension(path)
                });
            }

            return SuccessResult(new JObject
            {
                ["filter"] = filter,
                ["type"] = type,
                ["count"] = results.Count,
                ["assets"] = results
            });
        }

        [McpServerTool("EditorGetAssetInfo", Description = "Get asset information")]
        public static CallToolResult GetAssetInfo(
            [McpArgument(Description = "Asset path", Required = true)] string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return ErrorResult("Parameter 'assetPath' is required");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return ErrorResult($"Asset not found: {assetPath}");

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

            return SuccessResult(new JObject
            {
                ["path"] = assetPath,
                ["name"] = asset.name,
                ["guid"] = guid,
                ["type"] = type?.FullName,
                ["typeName"] = type?.Name,
                ["isMainAsset"] = AssetDatabase.IsMainAsset(asset),
                ["isSubAsset"] = AssetDatabase.IsSubAsset(asset)
            });
        }

        [McpServerTool("EditorGetAssetPath", Description = "Get asset path from GUID")]
        public static CallToolResult GetAssetPath(
            [McpArgument(Description = "Asset GUID", Required = true)] string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return ErrorResult("Parameter 'guid' is required");

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return ErrorResult($"Asset not found for GUID: {guid}");

            return SuccessResult(new JObject
            {
                ["guid"] = guid,
                ["path"] = path
            });
        }

        [McpServerTool("EditorGetAssetDependencies", Description = "Get asset dependencies")]
        public static CallToolResult GetAssetDependencies(
            [McpArgument(Description = "Asset path", Required = true)] string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return ErrorResult("Parameter 'assetPath' is required");

            var dependencies = AssetDatabase.GetDependencies(assetPath);
            var results = new JArray();

            foreach (var dep in dependencies)
            {
                if (dep != assetPath)
                {
                    results.Add(new JObject
                    {
                        ["path"] = dep,
                        ["name"] = System.IO.Path.GetFileNameWithoutExtension(dep)
                    });
                }
            }

            return SuccessResult(new JObject
            {
                ["assetPath"] = assetPath,
                ["dependencyCount"] = results.Count,
                ["dependencies"] = results
            });
        }

        [McpServerTool("EditorGetFolderContents", Description = "Get folder contents")]
        public static CallToolResult GetFolderContents(
            [McpArgument(Description = "Folder path", Required = true)] string folderPath,
            [McpArgument(Description = "Recursive search", Required = false)] bool recursive = false)
        {
            if (string.IsNullOrEmpty(folderPath))
                return ErrorResult("Parameter 'folderPath' is required");

            if (!AssetDatabase.IsValidFolder(folderPath))
                return ErrorResult($"Folder not found: {folderPath}");

            var contents = new JArray();
            var guids = AssetDatabase.FindAssets("", new[] { folderPath });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                
                if (!recursive && !path.StartsWith(folderPath + "/"))
                    continue;
                    
                if (path != folderPath)
                {
                    contents.Add(new JObject
                    {
                        ["path"] = path,
                        ["isFolder"] = AssetDatabase.IsValidFolder(path),
                        ["name"] = System.IO.Path.GetFileNameWithoutExtension(path)
                    });
                }
            }

            return SuccessResult(new JObject
            {
                ["folderPath"] = folderPath,
                ["count"] = contents.Count,
                ["contents"] = contents
            });
        }

        [McpServerTool("EditorCreateFolder", Description = "Create a folder")]
        public static CallToolResult CreateFolder(
            [McpArgument(Description = "Parent folder path", Required = true)] string parentPath,
            [McpArgument(Description = "New folder name", Required = true)] string folderName)
        {
            if (string.IsNullOrEmpty(parentPath) || string.IsNullOrEmpty(folderName))
                return ErrorResult("Parameters 'parentPath' and 'folderName' are required");

            if (!AssetDatabase.IsValidFolder(parentPath))
                return ErrorResult($"Parent folder not found: {parentPath}");

            var newFolderPath = parentPath + "/" + folderName;
            var guid = AssetDatabase.CreateFolder(parentPath, folderName);

            if (string.IsNullOrEmpty(guid))
                return ErrorResult("Failed to create folder");

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["path"] = newFolderPath,
                ["guid"] = guid
            });
        }

        [McpServerTool("EditorDeleteAsset", Description = "Delete an asset")]
        public static CallToolResult DeleteAsset(
            [McpArgument(Description = "Asset path to delete", Required = true)] string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return ErrorResult("Parameter 'assetPath' is required");

            if (!File.Exists(assetPath) && !AssetDatabase.IsValidFolder(assetPath))
                return ErrorResult($"Asset not found: {assetPath}");

            bool success = AssetDatabase.DeleteAsset(assetPath);

            return SuccessResult(new JObject
            {
                ["success"] = success,
                ["deletedPath"] = assetPath
            });
        }

        [McpServerTool("EditorRenameAsset", Description = "Rename an asset")]
        public static CallToolResult RenameAsset(
            [McpArgument(Description = "Asset path", Required = true)] string assetPath,
            [McpArgument(Description = "New name", Required = true)] string newName)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(newName))
                return ErrorResult("Parameters 'assetPath' and 'newName' are required");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null && !AssetDatabase.IsValidFolder(assetPath))
                return ErrorResult($"Asset not found: {assetPath}");

            string resultPath = AssetDatabase.RenameAsset(assetPath, newName);

            return SuccessResult(new JObject
            {
                ["success"] = !string.IsNullOrEmpty(resultPath) || assetPath.Contains(newName),
                ["oldPath"] = assetPath,
                ["newPath"] = string.IsNullOrEmpty(resultPath) ? assetPath : resultPath,
                ["newName"] = newName
            });
        }

        [McpServerTool("EditorMoveAsset", Description = "Move an asset")]
        public static CallToolResult MoveAsset(
            [McpArgument(Description = "Source path", Required = true)] string sourcePath,
            [McpArgument(Description = "Destination path", Required = true)] string destPath)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destPath))
                return ErrorResult("Parameters 'sourcePath' and 'destPath' are required");

            string result = AssetDatabase.MoveAsset(sourcePath, destPath);

            if (!string.IsNullOrEmpty(result))
                return ErrorResult($"Failed to move asset: {result}");

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["sourcePath"] = sourcePath,
                ["destPath"] = destPath
            });
        }

        [McpServerTool("EditorCopyAsset", Description = "Copy an asset")]
        public static CallToolResult CopyAsset(
            [McpArgument(Description = "Source path", Required = true)] string sourcePath,
            [McpArgument(Description = "Destination path", Required = true)] string destPath)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destPath))
                return ErrorResult("Parameters 'sourcePath' and 'destPath' are required");

            bool success = AssetDatabase.CopyAsset(sourcePath, destPath);

            if (!success)
                return ErrorResult("Failed to copy asset");

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["sourcePath"] = sourcePath,
                ["destPath"] = destPath
            });
        }

        [McpServerTool("EditorRefreshAssets", Description = "Refresh asset database")]
        public static CallToolResult RefreshAssets(
            [McpArgument(Description = "Import options: ForceUpdate, ForceSync, ForceUncompressedImport, or Default", Required = false)] string importOptions = "Default")
        {
            ImportAssetOptions options = ImportAssetOptions.Default;
            if (!string.IsNullOrEmpty(importOptions) && importOptions != "Default")
            {
                Enum.TryParse(importOptions, out options);
            }

            AssetDatabase.Refresh(options);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["importOptions"] = options.ToString()
            });
        }

        [McpServerTool("EditorReimportAsset", Description = "Reimport an asset")]
        public static CallToolResult ReimportAsset(
            [McpArgument(Description = "Asset path", Required = true)] string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return ErrorResult("Parameter 'assetPath' is required");

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["reimportedPath"] = assetPath
            });
        }

        [McpServerTool("EditorGetAssetImportSettings", Description = "Get asset import settings")]
        public static CallToolResult GetAssetImportSettings(
            [McpArgument(Description = "Asset path", Required = true)] string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return ErrorResult("Parameter 'assetPath' is required");

            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
                return ErrorResult($"No importer found for: {assetPath}");

            return SuccessResult(new JObject
            {
                ["path"] = assetPath,
                ["importerType"] = importer.GetType().FullName,
                ["assetBundleName"] = importer.assetBundleName,
                ["assetBundleVariant"] = importer.assetBundleVariant,
                ["userData"] = importer.userData
            });
        }

        [McpServerTool("EditorLoadAssetAtPath", Description = "Load asset at path")]
        public static CallToolResult LoadAssetAtPath(
            [McpArgument(Description = "Asset path", Required = true)] string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return ErrorResult("Parameter 'assetPath' is required");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return ErrorResult($"Asset not found: {assetPath}");

            return SuccessResult(new JObject
            {
                ["path"] = assetPath,
                ["name"] = asset.name,
                ["type"] = asset.GetType().FullName,
                ["typeName"] = asset.GetType().Name
            });
        }

        [McpServerTool("EditorGetAllAssetPaths", Description = "Get all asset paths")]
        public static CallToolResult GetAllAssetPaths(
            [McpArgument(Description = "Filter pattern (optional)", Required = false)] string filter = null)
        {
            var allPaths = AssetDatabase.GetAllAssetPaths();
            var results = new JArray();

            foreach (var path in allPaths)
            {
                if (!string.IsNullOrEmpty(filter) && !path.Contains(filter))
                    continue;

                results.Add(new JObject
                {
                    ["path"] = path,
                    ["isFolder"] = AssetDatabase.IsValidFolder(path)
                });
            }

            return SuccessResult(new JObject
            {
                ["filter"] = filter,
                ["count"] = results.Count,
                ["paths"] = results
            });
        }

        #endregion

        #region Prefab Operations (10 tools)

        [McpServerTool("EditorInstantiatePrefab", Description = "Instantiate a prefab in scene")]
        public static CallToolResult InstantiatePrefab(
            [McpArgument(Description = "Prefab asset path", Required = true)] string prefabPath,
            [McpArgument(Description = "Position X", Required = false)] float position_x = 0,
            [McpArgument(Description = "Position Y", Required = false)] float position_y = 0,
            [McpArgument(Description = "Position Z", Required = false)] float position_z = 0,
            [McpArgument(Description = "Rotation X (degrees)", Required = false)] float rotation_x = 0,
            [McpArgument(Description = "Rotation Y (degrees)", Required = false)] float rotation_y = 0,
            [McpArgument(Description = "Rotation Z (degrees)", Required = false)] float rotation_z = 0,
            [McpArgument(Description = "Parent path (optional)", Required = false)] string parentPath = null)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return ErrorResult("Parameter 'prefabPath' is required");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return ErrorResult($"Prefab not found: {prefabPath}");

            Transform parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentObj = FindGameObjectByPath(parentPath);
                if (parentObj == null)
                    return ErrorResult($"Parent GameObject not found: {parentPath}");
                parent = parentObj.transform;
            }

            var position = new Vector3(position_x, position_y, position_z);
            var rotation = Quaternion.Euler(rotation_x, rotation_y, rotation_z);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            
            if (parent != null)
                instance.transform.SetParent(parent);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["prefabPath"] = prefabPath,
                ["instancePath"] = GetGameObjectPath(instance),
                ["instanceName"] = instance.name
            });
        }

        [McpServerTool("EditorCreatePrefab", Description = "Create prefab from GameObject")]
        public static CallToolResult CreatePrefab(
            [McpArgument(Description = "GameObject path", Required = true)] string gameObjectPath,
            [McpArgument(Description = "Prefab save path", Required = true)] string prefabPath)
        {
            if (string.IsNullOrEmpty(gameObjectPath) || string.IsNullOrEmpty(prefabPath))
                return ErrorResult("Parameters 'gameObjectPath' and 'prefabPath' are required");

            var obj = FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ErrorResult($"GameObject not found: {gameObjectPath}");

            var prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out bool success);

            if (!success)
                return ErrorResult("Failed to create prefab");

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["prefabPath"] = prefabPath,
                ["prefabName"] = prefab?.name
            });
        }

        [McpServerTool("EditorApplyPrefab", Description = "Apply prefab changes")]
        public static CallToolResult ApplyPrefab(
            [McpArgument(Description = "GameObject instance path", Required = true)] string gameObjectPath)
        {
            if (string.IsNullOrEmpty(gameObjectPath))
                return ErrorResult("Parameter 'gameObjectPath' is required");

            var obj = FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ErrorResult($"GameObject not found: {gameObjectPath}");

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (prefabInstanceRoot == null)
                return ErrorResult($"GameObject is not a prefab instance: {gameObjectPath}");

            var prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstanceRoot);
            PrefabUtility.ApplyPrefabInstance(prefabInstanceRoot, InteractionMode.UserAction);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["prefabAssetPath"] = prefabAssetPath
            });
        }

        [McpServerTool("EditorRevertPrefab", Description = "Revert prefab to original")]
        public static CallToolResult RevertPrefab(
            [McpArgument(Description = "GameObject instance path", Required = true)] string gameObjectPath)
        {
            if (string.IsNullOrEmpty(gameObjectPath))
                return ErrorResult("Parameter 'gameObjectPath' is required");

            var obj = FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ErrorResult($"GameObject not found: {gameObjectPath}");

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (prefabInstanceRoot == null)
                return ErrorResult($"GameObject is not a prefab instance: {gameObjectPath}");

            PrefabUtility.RevertPrefabInstance(prefabInstanceRoot, InteractionMode.UserAction);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath
            });
        }

        [McpServerTool("EditorUnpackPrefab", Description = "Unpack prefab instance")]
        public static CallToolResult UnpackPrefab(
            [McpArgument(Description = "GameObject instance path", Required = true)] string gameObjectPath,
            [McpArgument(Description = "Unpack mode: OutermostRoot or Completely", Required = false)] string unpackMode = "OutermostRoot")
        {
            if (string.IsNullOrEmpty(gameObjectPath))
                return ErrorResult("Parameter 'gameObjectPath' is required");

            var obj = FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ErrorResult($"GameObject not found: {gameObjectPath}");

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (prefabInstanceRoot == null)
                return ErrorResult($"GameObject is not a prefab instance: {gameObjectPath}");

            var mode = unpackMode == "Completely" 
                ? PrefabUnpackMode.Completely 
                : PrefabUnpackMode.OutermostRoot;

            PrefabUtility.UnpackPrefabInstance(prefabInstanceRoot, mode, InteractionMode.UserAction);

            return SuccessResult(new JObject
            {
                ["success"] = true,
                ["gameObjectPath"] = gameObjectPath,
                ["unpackMode"] = mode.ToString()
            });
        }

        [McpServerTool("EditorGetPrefabInfo", Description = "Get prefab information")]
        public static CallToolResult GetPrefabInfo(
            [McpArgument(Description = "GameObject or prefab path", Required = true)] string path)
        {
            if (string.IsNullOrEmpty(path))
                return ErrorResult("Parameter 'path' is required");

            var obj = FindGameObjectByPath(path);
            if (obj != null)
            {
                var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                if (prefabInstanceRoot == null)
                    return ErrorResult($"GameObject is not a prefab instance: {path}");

                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstanceRoot);
                var prefabType = PrefabUtility.GetPrefabAssetType(obj);
                var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(obj);

                return SuccessResult(new JObject
                {
                    ["isPrefab"] = true,
                    ["instancePath"] = path,
                    ["assetPath"] = assetPath,
                    ["prefabType"] = prefabType.ToString(),
                    ["instanceStatus"] = instanceStatus.ToString()
                });
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return ErrorResult($"Prefab not found: {path}");

            return SuccessResult(new JObject
            {
                ["isPrefab"] = true,
                ["assetPath"] = path,
                ["prefabName"] = prefab.name,
                ["prefabType"] = PrefabUtility.GetPrefabAssetType(prefab).ToString()
            });
        }

        [McpServerTool("EditorGetPrefabType", Description = "Get prefab type")]
        public static CallToolResult GetPrefabType(
            [McpArgument(Description = "GameObject path", Required = true)] string path)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var prefabType = PrefabUtility.GetPrefabAssetType(obj);
            var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(obj);

            return SuccessResult(new JObject
            {
                ["path"] = path,
                ["prefabType"] = prefabType.ToString(),
                ["instanceStatus"] = instanceStatus.ToString(),
                ["isPrefabInstance"] = instanceStatus != PrefabInstanceStatus.NotAPrefab
            });
        }

        [McpServerTool("EditorIsPrefabInstance", Description = "Check if GameObject is a prefab instance")]
        public static CallToolResult IsPrefabInstance(
            [McpArgument(Description = "GameObject path", Required = true)] string path)
        {
            var obj = FindGameObjectByPath(path);
            if (obj == null)
                return ErrorResult($"GameObject not found: {path}");

            var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
            bool isInstance = instanceStatus != PrefabInstanceStatus.NotAPrefab;

            return SuccessResult(new JObject
            {
                ["path"] = path,
                ["isPrefabInstance"] = isInstance,
                ["instanceStatus"] = instanceStatus.ToString()
            });
        }

        [McpServerTool("EditorGetPrefabAssetPath", Description = "Get prefab asset path from instance")]
        public static CallToolResult GetPrefabAssetPath(
            [McpArgument(Description = "GameObject instance path", Required = true)] string gameObjectPath)
        {
            var obj = FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ErrorResult($"GameObject not found: {gameObjectPath}");

            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            if (string.IsNullOrEmpty(assetPath))
                return ErrorResult($"GameObject is not a prefab instance: {gameObjectPath}");

            return SuccessResult(new JObject
            {
                ["gameObjectPath"] = gameObjectPath,
                ["prefabAssetPath"] = assetPath
            });
        }

        [McpServerTool("EditorGetAllPrefabs", Description = "Get all prefab assets")]
        public static CallToolResult GetAllPrefabs(
            [McpArgument(Description = "Search folder (optional)", Required = false)] string searchFolder = null)
        {
            string[] folders = null;
            if (!string.IsNullOrEmpty(searchFolder))
            {
                folders = new[] { searchFolder };
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", folders);
            var results = new JArray();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    results.Add(new JObject
                    {
                        ["guid"] = guid,
                        ["path"] = path,
                        ["name"] = prefab.name
                    });
                }
            }

            return SuccessResult(new JObject
            {
                ["searchFolder"] = searchFolder,
                ["count"] = results.Count,
                ["prefabs"] = results
            });
        }

        #endregion

        #region Helper Methods

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

        private static JObject VectorToJson(Vector3 v)
        {
            return new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
        }

        private static GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            return GameObject.Find(path);
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";
            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private static JObject GetGameObjectInfoJson(GameObject obj)
        {
            var t = obj.transform;
            return new JObject
            {
                ["name"] = obj.name,
                ["path"] = GetGameObjectPath(obj),
                ["active"] = obj.activeSelf,
                ["activeInHierarchy"] = obj.activeInHierarchy,
                ["isStatic"] = obj.isStatic,
                ["tag"] = obj.tag,
                ["layer"] = LayerMask.LayerToName(obj.layer),
                ["layerIndex"] = obj.layer,
                ["position"] = VectorToJson(t.position),
                ["rotation"] = VectorToJson(t.rotation.eulerAngles),
                ["localPosition"] = VectorToJson(t.localPosition),
                ["localRotation"] = VectorToJson(t.localRotation.eulerAngles),
                ["localScale"] = VectorToJson(t.localScale),
                ["childCount"] = t.childCount,
                ["parent"] = t.parent?.name
            };
        }

        private static JObject GetGameObjectSummaryJson(GameObject obj)
        {
            return new JObject
            {
                ["name"] = obj.name,
                ["path"] = GetGameObjectPath(obj),
                ["active"] = obj.activeSelf,
                ["tag"] = obj.tag,
                ["layer"] = LayerMask.LayerToName(obj.layer)
            };
        }

        private static void CollectChildren(Transform parent, JArray array, bool recursive)
        {
            foreach (Transform child in parent)
            {
                var childInfo = GetGameObjectSummaryJson(child.gameObject);
                childInfo["childCount"] = child.childCount;

                if (recursive && child.childCount > 0)
                {
                    var subChildren = new JArray();
                    CollectChildren(child, subChildren, recursive);
                    childInfo["children"] = subChildren;
                }

                array.Add(childInfo);
            }
        }

        private static int CountGameObjectsRecursive(GameObject go)
        {
            int count = 1;
            foreach (Transform child in go.transform)
            {
                count += CountGameObjectsRecursive(child.gameObject);
            }
            return count;
        }

        private static bool IsInScene(GameObject obj)
        {
            return obj.hideFlags == HideFlags.None && 
                   AssetDatabase.GetAssetPath(obj) == "";
        }

        private static JArray BuildHierarchyArray(GameObject[] objects, int currentDepth, int maxDepth)
        {
            var array = new JArray();

            if (maxDepth >= 0 && currentDepth >= maxDepth)
                return array;

            foreach (var obj in objects)
            {
                var node = GetGameObjectSummaryJson(obj);
                node["childCount"] = obj.transform.childCount;

                var children = new List<GameObject>();
                foreach (Transform child in obj.transform)
                {
                    children.Add(child.gameObject);
                }

                if (children.Count > 0)
                {
                    node["children"] = BuildHierarchyArray(children.ToArray(), currentDepth + 1, maxDepth);
                }

                array.Add(node);
            }

            return array;
        }

        private static Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            type = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (type != null)
                return type;

            type = Type.GetType($"UnityEngine.{typeName}, UnityEngine.CoreModule");
            if (type != null)
                return type;

            type = Type.GetType($"UnityEditor.{typeName}, UnityEditor");
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;

                type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static JObject GetComponentProperties(Component component)
        {
            var props = new JObject();
            var type = component.GetType();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Take(20);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(component);
                    props[prop.Name] = value?.ToString() ?? "null";
                }
                catch
                {
                    props[prop.Name] = "<error>";
                }
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Take(20);

            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(component);
                    props[field.Name] = value?.ToString() ?? "null";
                }
                catch
                {
                    props[field.Name] = "<error>";
                }
            }

            return props;
        }

        private static object ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(bool))
                return bool.TryParse(value, out var b) ? b : value == "1" || value.ToLower() == "true";

            if (targetType == typeof(int))
                return int.TryParse(value, out var i) ? i : 0;

            if (targetType == typeof(float))
                return float.TryParse(value, out var f) ? f : 0f;

            if (targetType == typeof(double))
                return double.TryParse(value, out var d) ? d : 0d;

            if (targetType == typeof(long))
                return long.TryParse(value, out var l) ? l : 0L;

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, true);

            if (targetType == typeof(Vector2))
            {
                var parts = value.Split(',');
                if (parts.Length >= 2)
                    return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            }

            if (targetType == typeof(Vector3))
            {
                var parts = value.Split(',');
                if (parts.Length >= 3)
                    return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            }

            if (targetType == typeof(Color))
            {
                var parts = value.Split(',');
                if (parts.Length >= 4)
                    return new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                if (parts.Length >= 3)
                    return new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            }

            return Convert.ChangeType(value, targetType);
        }

        #endregion
    }
}
#endif
