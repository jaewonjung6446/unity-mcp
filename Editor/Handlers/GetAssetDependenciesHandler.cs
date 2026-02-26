using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class GetAssetDependenciesHandler : IToolHandler
    {
        public string Name => "get_asset_dependencies";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            var recursive = parameters["recursive"]?.ToObject<bool>() ?? true;

            var deps = AssetDatabase.GetDependencies(assetPath, recursive);
            var results = new JArray();

            foreach (var dep in deps.OrderBy(d => d))
            {
                if (dep == assetPath) continue; // Skip self
                var type = AssetDatabase.GetMainAssetTypeAtPath(dep);
                results.Add(new JObject
                {
                    ["path"] = dep,
                    ["type"] = type?.Name ?? "Unknown"
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} dependencies for '{assetPath}'",
                ["dependencies"] = results
            };
        }
    }
}
