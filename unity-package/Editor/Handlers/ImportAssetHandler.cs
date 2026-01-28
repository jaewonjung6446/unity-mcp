using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class ImportAssetHandler : IToolHandler
    {
        public string Name => "import_asset";

        public JObject Execute(JObject parameters)
        {
            var path = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Imported asset at '{path}'"
            };
        }
    }
}
