using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class CreateScriptHandler : IToolHandler
    {
        public string Name => "create_script";

        public JObject Execute(JObject parameters)
        {
            var filePath = parameters["filePath"]?.ToString();
            var content = parameters["content"]?.ToString();

            if (string.IsNullOrEmpty(filePath))
                return McpServer.CreateError("Missing required parameter: filePath", "validation_error");
            if (content == null)
                return McpServer.CreateError("Missing required parameter: content", "validation_error");

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, content);
            AssetDatabase.ImportAsset(filePath);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created script at '{filePath}'"
            };
        }
    }
}
