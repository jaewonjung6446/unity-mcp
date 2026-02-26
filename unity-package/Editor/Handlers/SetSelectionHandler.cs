using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetSelectionHandler : IToolHandler
    {
        public string Name => "set_selection";

        public JObject Execute(JObject parameters)
        {
            var instanceId = parameters["instanceId"];
            var gameObjectPath = parameters["gameObjectPath"]?.ToString();
            var instanceIds = parameters["instanceIds"] as JArray;
            var assetPath = parameters["assetPath"]?.ToString();

            if (instanceId == null && string.IsNullOrEmpty(gameObjectPath) &&
                instanceIds == null && string.IsNullOrEmpty(assetPath))
            {
                // Clear selection
                Selection.activeObject = null;
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "Cleared selection"
                };
            }

            // Select asset
            if (!string.IsNullOrEmpty(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset == null)
                    return McpServer.CreateError($"Asset not found at '{assetPath}'", "not_found_error");
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Selected asset '{assetPath}'"
                };
            }

            // Multiple selection
            if (instanceIds != null)
            {
                var objects = new List<Object>();
                foreach (var id in instanceIds)
                {
                    var obj = EditorUtility.InstanceIDToObject(id.ToObject<int>());
                    if (obj != null) objects.Add(obj);
                }
                Selection.objects = objects.ToArray();
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Selected {objects.Count} objects"
                };
            }

            // Single selection
            GameObject go = null;
            if (instanceId != null)
                go = EditorUtility.InstanceIDToObject(instanceId.ToObject<int>()) as GameObject;
            else if (!string.IsNullOrEmpty(gameObjectPath))
                go = GameObject.Find(gameObjectPath);

            if (go == null)
                return McpServer.CreateError("GameObject not found", "not_found_error");

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Selected '{go.name}'",
                ["instanceId"] = go.GetInstanceID()
            };
        }
    }
}
