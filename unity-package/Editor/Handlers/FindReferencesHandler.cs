using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class FindReferencesHandler : IToolHandler
    {
        public string Name => "find_references";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            var searchFilter = parameters["filter"]?.ToString();
            string[] searchInPaths = null;

            if (!string.IsNullOrEmpty(searchFilter))
                searchInPaths = new[] { searchFilter };

            // Find all assets that depend on this asset
            var allAssets = AssetDatabase.GetAllAssetPaths();
            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrEmpty(guid))
                return McpServer.CreateError($"Asset not found: {assetPath}", "not_found_error");

            var referencers = new List<string>();

            foreach (var path in allAssets)
            {
                if (path == assetPath) continue;
                if (searchInPaths != null && !searchInPaths.Any(s => path.StartsWith(s))) continue;

                var deps = AssetDatabase.GetDependencies(path, false);
                if (deps.Contains(assetPath))
                    referencers.Add(path);

                if (referencers.Count >= 200) break;
            }

            var results = new JArray();
            foreach (var refPath in referencers.OrderBy(r => r))
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(refPath);
                results.Add(new JObject
                {
                    ["path"] = refPath,
                    ["type"] = type?.Name ?? "Unknown"
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} assets referencing '{assetPath}'",
                ["references"] = results,
                ["capped"] = referencers.Count >= 200
            };
        }
    }
}
