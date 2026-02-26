using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class CreateFolderHandler : IToolHandler
    {
        public string Name => "create_folder";

        public JObject Execute(JObject parameters)
        {
            var folderPath = parameters["folderPath"]?.ToString();
            if (string.IsNullOrEmpty(folderPath))
                return McpServer.CreateError("Missing required parameter: folderPath", "validation_error");

            // Split path and create folders recursively
            var parts = folderPath.Replace("\\", "/").Split('/');
            string currentPath = parts[0]; // "Assets"

            for (int i = 1; i < parts.Length; i++)
            {
                var nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    var guid = AssetDatabase.CreateFolder(currentPath, parts[i]);
                    if (string.IsNullOrEmpty(guid))
                        return McpServer.CreateError($"Failed to create folder at '{nextPath}'", "execution_error");
                }
                currentPath = nextPath;
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created folder '{folderPath}'"
            };
        }
    }
}
