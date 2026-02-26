using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetAssetPreviewHandler : IToolHandler
    {
        public string Name => "get_asset_preview";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return McpServer.CreateError($"Asset not found at '{assetPath}'", "not_found_error");

            var preview = AssetPreview.GetAssetPreview(asset);
            if (preview == null)
            {
                // Try mini thumbnail
                preview = AssetPreview.GetMiniThumbnail(asset);
            }

            if (preview == null)
                return McpServer.CreateError("No preview available for this asset", "execution_error");

            var pngData = preview.EncodeToPNG();
            var base64 = Convert.ToBase64String(pngData);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "image",
                ["message"] = $"Preview of '{asset.name}'",
                ["data"] = base64,
                ["mimeType"] = "image/png",
                ["width"] = preview.width,
                ["height"] = preview.height
            };
        }
    }
}
