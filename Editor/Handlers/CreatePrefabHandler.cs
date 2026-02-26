using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class CreatePrefabHandler : IToolHandler
    {
        public string Name => "create_prefab";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var savePath = parameters["savePath"]?.ToString();
            if (string.IsNullOrEmpty(savePath))
                return McpServer.CreateError("Missing required parameter: savePath", "validation_error");

            // Ensure directory exists
            var dir = System.IO.Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                // Create parent folders
                var parts = dir.Replace("\\", "/").Split('/');
                string currentPath = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var nextPath = currentPath + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    currentPath = nextPath;
                }
            }

            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, savePath, InteractionMode.UserAction);

            if (prefab == null)
                return McpServer.CreateError($"Failed to create prefab at '{savePath}'", "execution_error");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created prefab from '{go.name}' at '{savePath}'",
                ["prefabPath"] = savePath,
                ["instanceId"] = go.GetInstanceID()
            };
        }
    }
}
