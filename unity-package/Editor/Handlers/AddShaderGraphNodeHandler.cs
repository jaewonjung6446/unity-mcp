using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class AddShaderGraphNodeHandler : IToolHandler
    {
        public string Name => "add_shader_graph_node";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            var nodeType = parameters["nodeType"]?.ToString();
            var posX = parameters["positionX"]?.ToObject<float>() ?? -400f;
            var posY = parameters["positionY"]?.ToObject<float>() ?? 0f;
            var properties = parameters["properties"] as JObject;

            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");
            if (string.IsNullOrEmpty(nodeType))
                return McpServer.CreateError("Missing required parameter: nodeType", "validation_error");

            if (!ShaderGraphHelper.NodeTypeMap.ContainsKey(nodeType) && !nodeType.Contains("."))
                return McpServer.CreateError($"Unknown node type: {nodeType}. Use a known type (SampleTexture2D, Color, Multiply, Add, Lerp, UV, Time, etc.) or a fully qualified type name.", "validation_error");

            var fullPath = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                assetPath);

            if (!File.Exists(fullPath))
                return McpServer.CreateError($"Shader graph not found: {assetPath}", "not_found_error");

            var graph = ShaderGraphHelper.LoadGraph(fullPath);
            var node = ShaderGraphHelper.CreateNodeObject(nodeType, posX, posY, properties);
            var nodeId = node["m_Id"].ToString();

            var nodes = graph["m_Nodes"] as JArray;
            if (nodes == null)
            {
                nodes = new JArray();
                graph["m_Nodes"] = nodes;
            }
            nodes.Add(node);

            ShaderGraphHelper.SaveGraph(fullPath, graph);
            AssetDatabase.ImportAsset(assetPath);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Added {nodeType} node to shader graph",
                ["nodeId"] = nodeId,
                ["nodeType"] = nodeType
            };
        }
    }
}
