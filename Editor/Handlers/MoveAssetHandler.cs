using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class MoveAssetHandler : IToolHandler
    {
        public string Name => "move_asset";

        public JObject Execute(JObject parameters)
        {
            var sourcePath = parameters["sourcePath"]?.ToString();
            var destPath = parameters["destPath"]?.ToString();

            if (string.IsNullOrEmpty(sourcePath))
                return McpServer.CreateError("Missing required parameter: sourcePath", "validation_error");
            if (string.IsNullOrEmpty(destPath))
                return McpServer.CreateError("Missing required parameter: destPath", "validation_error");

            var result = AssetDatabase.MoveAsset(sourcePath, destPath);

            if (!string.IsNullOrEmpty(result))
                return McpServer.CreateError($"Failed to move asset: {result}", "execution_error");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Moved asset from '{sourcePath}' to '{destPath}'"
            };
        }
    }
}
