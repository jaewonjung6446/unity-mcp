using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class AddShaderGraphPropertyHandler : IToolHandler
    {
        public string Name => "add_shader_graph_property";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            var propertyName = parameters["propertyName"]?.ToString();
            var propertyType = parameters["propertyType"]?.ToString();
            var referenceName = parameters["referenceName"]?.ToString();
            var defaultValue = parameters["defaultValue"];

            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");
            if (string.IsNullOrEmpty(propertyName))
                return McpServer.CreateError("Missing required parameter: propertyName", "validation_error");
            if (string.IsNullOrEmpty(propertyType))
                return McpServer.CreateError("Missing required parameter: propertyType", "validation_error");

            if (!ShaderGraphHelper.PropertyTypeIds.ContainsKey(propertyType))
                return McpServer.CreateError($"Unknown property type: {propertyType}. Supported: Color, Float, Vector2, Vector3, Vector4, Texture2D, Boolean, Integer", "validation_error");

            var fullPath = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                assetPath);

            if (!File.Exists(fullPath))
                return McpServer.CreateError($"Shader graph not found: {assetPath}", "not_found_error");

            var graph = ShaderGraphHelper.LoadGraph(fullPath);

            // Create property
            var property = ShaderGraphHelper.CreatePropertyObject(propertyName, propertyType, referenceName, defaultValue);
            var propertyId = property["m_Id"].ToString();

            var props = graph["m_Properties"] as JArray;
            if (props == null)
            {
                props = new JArray();
                graph["m_Properties"] = props;
            }
            props.Add(property);

            // Create corresponding PropertyNode
            var propNode = ShaderGraphHelper.CreatePropertyNode(propertyId, propertyName, propertyType, -600f, props.Count * 150f);
            var nodeId = propNode["m_Id"].ToString();

            var nodes = graph["m_Nodes"] as JArray;
            if (nodes == null)
            {
                nodes = new JArray();
                graph["m_Nodes"] = nodes;
            }
            nodes.Add(propNode);

            ShaderGraphHelper.SaveGraph(fullPath, graph);
            AssetDatabase.ImportAsset(assetPath);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Added {propertyType} property '{propertyName}' to shader graph",
                ["propertyId"] = propertyId,
                ["nodeId"] = nodeId,
                ["referenceName"] = property["m_ReferenceName"]?.ToString()
            };
        }
    }
}
