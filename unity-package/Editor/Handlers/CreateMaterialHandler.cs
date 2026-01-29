using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class CreateMaterialHandler : IToolHandler
    {
        public string Name => "create_material";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            var shaderName = parameters["shaderName"]?.ToString();
            var shaderGraphPath = parameters["shaderGraphPath"]?.ToString();

            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            if (!assetPath.EndsWith(".mat"))
                assetPath += ".mat";

            Shader shader = null;

            if (!string.IsNullOrEmpty(shaderGraphPath))
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderGraphPath);
                if (shader == null)
                    return McpServer.CreateError($"Shader not found at path: {shaderGraphPath}", "not_found_error");
            }
            else if (!string.IsNullOrEmpty(shaderName))
            {
                shader = Shader.Find(shaderName);
                if (shader == null)
                    return McpServer.CreateError($"Shader not found: {shaderName}", "not_found_error");
            }
            else
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
            }

            if (shader == null)
                return McpServer.CreateError("No valid shader found", "not_found_error");

            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir))
            {
                var fullDir = Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    dir);
                if (!Directory.Exists(fullDir))
                    Directory.CreateDirectory(fullDir);
            }

            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.SaveAssets();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created material at '{assetPath}' with shader '{shader.name}'",
                ["assetPath"] = assetPath,
                ["shaderName"] = shader.name
            };
        }
    }
}
