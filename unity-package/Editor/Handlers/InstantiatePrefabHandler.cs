using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class InstantiatePrefabHandler : IToolHandler
    {
        public string Name => "instantiate_prefab";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                return McpServer.CreateError($"Prefab not found at '{assetPath}'", "not_found_error");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
                return McpServer.CreateError("Failed to instantiate prefab", "execution_error");

            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {prefab.name}");

            // Set name
            var name = parameters["name"]?.ToString();
            if (!string.IsNullOrEmpty(name))
                instance.name = name;

            // Set parent
            var parentPath = parameters["parentPath"]?.ToString();
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                    Undo.SetTransformParent(instance.transform, parent.transform, $"Parent {instance.name}");
            }

            // Set position
            var position = parameters["position"] as JObject;
            if (position != null)
            {
                instance.transform.localPosition = new Vector3(
                    position["x"]?.ToObject<float>() ?? 0f,
                    position["y"]?.ToObject<float>() ?? 0f,
                    position["z"]?.ToObject<float>() ?? 0f
                );
            }

            // Set rotation
            var rotation = parameters["rotation"] as JObject;
            if (rotation != null)
            {
                instance.transform.localEulerAngles = new Vector3(
                    rotation["x"]?.ToObject<float>() ?? 0f,
                    rotation["y"]?.ToObject<float>() ?? 0f,
                    rotation["z"]?.ToObject<float>() ?? 0f
                );
            }

            EditorSceneManager.MarkSceneDirty(instance.scene);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Instantiated prefab '{prefab.name}' as '{instance.name}'",
                ["instanceId"] = instance.GetInstanceID(),
                ["path"] = GetGameObjectHandler.GetPath(instance),
                ["prefabPath"] = assetPath
            };
        }
    }
}
