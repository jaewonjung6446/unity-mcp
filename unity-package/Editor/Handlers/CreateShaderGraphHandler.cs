using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class CreateShaderGraphHandler : IToolHandler
    {
        public string Name => "create_shader_graph";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            var templateType = parameters["templateType"]?.ToString() ?? "urp_lit";
            var shaderName = parameters["shaderName"]?.ToString();

            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");

            if (!assetPath.EndsWith(".shadergraph"))
                assetPath += ".shadergraph";

            var fullPath = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                assetPath);

            if (File.Exists(fullPath))
                return McpServer.CreateError($"File already exists: {assetPath}", "validation_error");

            if (shaderName == null)
                shaderName = Path.GetFileNameWithoutExtension(assetPath);

            var graph = ShaderGraphHelper.CreateEmptyGraph(templateType, shaderName);
            ShaderGraphHelper.SaveGraph(fullPath, graph);
            AssetDatabase.ImportAsset(assetPath);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created {templateType} shader graph at '{assetPath}'",
                ["assetPath"] = assetPath,
                ["templateType"] = templateType,
                ["shaderName"] = shaderName
            };
        }
    }
}
