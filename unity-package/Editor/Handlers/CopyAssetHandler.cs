using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class CopyAssetHandler : IToolHandler
    {
        public string Name => "copy_asset";

        public JObject Execute(JObject parameters)
        {
            var sourcePath = parameters["sourcePath"]?.ToString();
            var destPath = parameters["destPath"]?.ToString();

            if (string.IsNullOrEmpty(sourcePath))
                return McpServer.CreateError("Missing required parameter: sourcePath", "validation_error");
            if (string.IsNullOrEmpty(destPath))
                return McpServer.CreateError("Missing required parameter: destPath", "validation_error");

            bool success = AssetDatabase.CopyAsset(sourcePath, destPath);

            return new JObject
            {
                ["success"] = success,
                ["type"] = "text",
                ["message"] = success
                    ? $"Copied '{sourcePath}' to '{destPath}'"
                    : $"Failed to copy '{sourcePath}' to '{destPath}'"
            };
        }
    }
}
