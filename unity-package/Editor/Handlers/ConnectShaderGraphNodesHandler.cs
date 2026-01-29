using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class ConnectShaderGraphNodesHandler : IToolHandler
    {
        public string Name => "connect_shader_graph_nodes";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            var sourceNodeId = parameters["sourceNodeId"]?.ToString();
            var sourceSlotId = parameters["sourceSlotId"]?.ToObject<int>() ?? -1;
            var targetNodeId = parameters["targetNodeId"]?.ToString();
            var targetSlotId = parameters["targetSlotId"]?.ToObject<int>() ?? -1;

            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");
            if (string.IsNullOrEmpty(sourceNodeId))
                return McpServer.CreateError("Missing required parameter: sourceNodeId", "validation_error");
            if (sourceSlotId < 0)
                return McpServer.CreateError("Missing required parameter: sourceSlotId", "validation_error");
            if (string.IsNullOrEmpty(targetNodeId))
                return McpServer.CreateError("Missing required parameter: targetNodeId", "validation_error");
            if (targetSlotId < 0)
                return McpServer.CreateError("Missing required parameter: targetSlotId", "validation_error");

            var fullPath = Path.Combine(
                Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                assetPath);

            if (!File.Exists(fullPath))
                return McpServer.CreateError($"Shader graph not found: {assetPath}", "not_found_error");

            var docs = ShaderGraphHelper.LoadDocuments(fullPath);
            var graphData = ShaderGraphHelper.GetGraphData(docs);

            var edge = ShaderGraphHelper.CreateEdge(sourceNodeId, sourceSlotId, targetNodeId, targetSlotId);

            var edges = graphData["m_Edges"] as JArray;
            if (edges == null)
            {
                edges = new JArray();
                graphData["m_Edges"] = edges;
            }
            edges.Add(edge);

            ShaderGraphHelper.SaveDocuments(fullPath, docs);
            AssetDatabase.ImportAsset(assetPath);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Connected node {sourceNodeId}:{sourceSlotId} → {targetNodeId}:{targetSlotId}"
            };
        }
    }
}
