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

            if (!ShaderGraphHelper.PropertyTypeMap.ContainsKey(propertyType))
                return McpServer.CreateError($"Unknown property type: {propertyType}. Supported: Color, Float, Vector2, Vector3, Vector4, Texture2D, Boolean, Integer", "validation_error");

            var fullPath = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                assetPath);

            if (!File.Exists(fullPath))
                return McpServer.CreateError($"Shader graph not found: {assetPath}", "not_found_error");

            var docs = ShaderGraphHelper.LoadDocuments(fullPath);
            var graphData = ShaderGraphHelper.GetGraphData(docs);

            // Count existing properties for Y positioning
            var props = graphData["m_Properties"] as JArray ?? new JArray();
            float posY = props.Count * 150f;

            // Create property + node + slots
            var (propDoc, nodeDoc, slotDocs, propId, nodeId) =
                ShaderGraphHelper.CreateProperty(propertyName, propertyType, referenceName, defaultValue, -600f, posY);

            // Add property reference to GraphData
            if (graphData["m_Properties"] == null)
                graphData["m_Properties"] = new JArray();
            ((JArray)graphData["m_Properties"]).Add(new JObject { ["m_Id"] = propId });

            // Add node reference to GraphData
            if (graphData["m_Nodes"] == null)
                graphData["m_Nodes"] = new JArray();
            ((JArray)graphData["m_Nodes"]).Add(new JObject { ["m_Id"] = nodeId });

            // Add to CategoryData's child list (find first CategoryData)
            foreach (var doc in docs)
            {
                if (doc["m_Type"]?.ToString() == "UnityEditor.ShaderGraph.CategoryData")
                {
                    var children = doc["m_ChildObjectList"] as JArray;
                    if (children == null)
                    {
                        children = new JArray();
                        doc["m_ChildObjectList"] = children;
                    }
                    children.Add(new JObject { ["m_Id"] = propId });
                    break;
                }
            }

            // Append documents
            docs.Add(propDoc);
            docs.Add(nodeDoc);
            docs.AddRange(slotDocs);

            ShaderGraphHelper.SaveDocuments(fullPath, docs);
            AssetDatabase.ImportAsset(assetPath);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Added {propertyType} property '{propertyName}' to shader graph",
                ["propertyId"] = propId,
                ["nodeId"] = nodeId,
                ["referenceName"] = referenceName ?? ("_" + propertyName.Replace(" ", ""))
            };
        }
    }
}
