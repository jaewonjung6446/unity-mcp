using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class OpenPrefabHandler : IToolHandler
    {
        public string Name => "open_prefab";

        public JObject Execute(JObject parameters)
        {
            var path = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return McpServer.CreateError($"Prefab not found at '{path}'", "not_found_error");

            AssetDatabase.OpenAsset(prefab);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Opened prefab '{prefab.name}'"
            };
        }
    }
}
