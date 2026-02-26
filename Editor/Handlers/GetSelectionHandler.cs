using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class GetSelectionHandler : IToolHandler
    {
        public string Name => "get_selection";

        public JObject Execute(JObject parameters)
        {
            var selected = Selection.gameObjects;
            var arr = new JArray();
            foreach (var go in selected)
            {
                arr.Add(new JObject
                {
                    ["name"] = go.name,
                    ["instanceId"] = go.GetInstanceID(),
                    ["path"] = GetGameObjectHandler.GetPath(go)
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Selected {selected.Length} objects",
                ["selection"] = arr
            };
        }
    }
}
