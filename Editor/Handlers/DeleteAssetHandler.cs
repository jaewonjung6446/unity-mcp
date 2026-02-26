using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class DeleteAssetHandler : IToolHandler
    {
        public string Name => "delete_asset";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            if (!System.IO.File.Exists(assetPath) && !System.IO.Directory.Exists(assetPath))
                return McpServer.CreateError($"Asset not found at '{assetPath}'", "not_found_error");

            bool success = AssetDatabase.DeleteAsset(assetPath);

            if (!success)
                return McpServer.CreateError($"Failed to delete asset at '{assetPath}'", "execution_error");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Deleted asset at '{assetPath}'"
            };
        }
    }
}
