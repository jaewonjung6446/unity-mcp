using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class GetAssetImporterHandler : IToolHandler
    {
        public string Name => "get_asset_importer";

        public JObject Execute(JObject parameters)
        {
            var path = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            var importer = AssetImporter.GetAtPath(path);
            if (importer == null)
                return McpServer.CreateError($"No importer found for '{path}'", "not_found_error");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Retrieved importer for '{path}'",
                ["importerType"] = importer.GetType().Name,
                ["settings"] = JObject.Parse(EditorJsonUtility.ToJson(importer, true))
            };
        }
    }
}
