using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class RefreshAssetsHandler : IToolHandler
    {
        public string Name => "refresh_assets";

        public JObject Execute(JObject parameters)
        {
            AssetDatabase.Refresh();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "AssetDatabase refreshed"
            };
        }
    }
}
