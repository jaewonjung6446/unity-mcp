using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetAssetContentsHandler : IToolHandler
    {
        public string Name => "get_asset_contents";

        public JObject Execute(JObject parameters)
        {
            var path = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null)
                return McpServer.CreateError($"Asset not found at '{path}'", "not_found_error");

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["assetType"] = asset.GetType().Name,
                ["name"] = asset.name,
                ["instanceId"] = asset.GetInstanceID()
            };

            // For text-based assets, include content
            if (asset is TextAsset textAsset)
            {
                result["content"] = textAsset.text;
                result["message"] = $"Retrieved text asset '{asset.name}'";
            }
            else if (asset is MonoScript script)
            {
                result["content"] = script.text;
                result["message"] = $"Retrieved script '{asset.name}'";
            }
            else if (File.Exists(path) && IsTextFile(path))
            {
                result["content"] = File.ReadAllText(path);
                result["message"] = $"Retrieved text content of '{asset.name}'";
            }
            else
            {
                // Return serialized YAML for binary assets
                result["content"] = EditorJsonUtility.ToJson(asset, true);
                result["message"] = $"Retrieved serialized content of '{asset.name}'";
            }

            return result;
        }

        private static bool IsTextFile(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext is ".cs" or ".txt" or ".json" or ".xml" or ".yaml" or ".yml" or ".shader"
                or ".cginc" or ".hlsl" or ".glsl" or ".asmdef" or ".asmref" or ".md";
        }
    }
}
