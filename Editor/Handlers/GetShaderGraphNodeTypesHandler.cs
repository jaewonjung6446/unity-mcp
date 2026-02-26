using Newtonsoft.Json.Linq;

namespace McpUnity.Handlers
{
    public class GetShaderGraphNodeTypesHandler : IToolHandler
    {
        public string Name => "get_shader_graph_node_types";

        public JObject Execute(JObject parameters)
        {
            var filterNodeType = parameters?["nodeType"]?.ToString();

            var nodeTypes = new JObject();

            foreach (var kvp in ShaderGraphHelper.NodeTypeMap)
            {
                if (!string.IsNullOrEmpty(filterNodeType) &&
                    !string.Equals(kvp.Key, filterNodeType, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var slotDefs = ShaderGraphHelper.GetSlotDefs(kvp.Key);
                var slotsArray = new JArray();
                foreach (var sd in slotDefs)
                {
                    slotsArray.Add(new JObject
                    {
                        ["id"] = sd.Id,
                        ["name"] = sd.Name,
                        ["direction"] = sd.IsInput ? "input" : "output"
                    });
                }

                nodeTypes[kvp.Key] = new JObject
                {
                    ["internalType"] = kvp.Value,
                    ["slots"] = slotsArray
                };
            }

            var propertyTypes = new JArray();
            foreach (var key in ShaderGraphHelper.PropertyTypeMap.Keys)
                propertyTypes.Add(key);

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["nodeTypes"] = nodeTypes,
                ["propertyTypes"] = propertyTypes,
                ["templateTypes"] = new JArray("urp_lit", "urp_unlit", "urp_canvas")
            };

            if (!string.IsNullOrEmpty(filterNodeType))
                result["message"] = nodeTypes.Count > 0
                    ? $"Found node type '{filterNodeType}'"
                    : $"Unknown node type '{filterNodeType}'. Use this tool without nodeType parameter to see all available types.";
            else
                result["message"] = $"Available: {ShaderGraphHelper.NodeTypeMap.Count} node types, {ShaderGraphHelper.PropertyTypeMap.Count} property types, 3 template types";

            return result;
        }
    }
}
