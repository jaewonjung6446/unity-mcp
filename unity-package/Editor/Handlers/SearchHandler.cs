using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Handlers
{
    public class SearchHandler : IToolHandler
    {
        public string Name => "search";

        public JObject Execute(JObject parameters)
        {
            var query = parameters["query"]?.ToString();
            var searchType = parameters["type"]?.ToString() ?? "asset";

            if (string.IsNullOrEmpty(query))
                return McpServer.CreateError("Missing required parameter: query", "validation_error");

            if (searchType == "scene")
                return SearchScene(query);

            return SearchAssets(query);
        }

        private JObject SearchAssets(string query)
        {
            var guids = AssetDatabase.FindAssets(query);
            var results = new JArray();

            foreach (var guid in guids.Take(50))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                results.Add(new JObject
                {
                    ["guid"] = guid,
                    ["path"] = path,
                    ["type"] = type?.Name ?? "Unknown"
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} assets",
                ["results"] = results
            };
        }

        private JObject SearchScene(string query)
        {
            var results = new JArray();
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var go in allObjects.Where(g => g.name.Contains(query)).Take(50))
            {
                results.Add(new JObject
                {
                    ["name"] = go.name,
                    ["instanceId"] = go.GetInstanceID(),
                    ["path"] = GetGameObjectHandler.GetPath(go)
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} scene objects",
                ["results"] = results
            };
        }
    }
}
