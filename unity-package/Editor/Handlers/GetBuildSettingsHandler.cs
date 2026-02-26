using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class GetBuildSettingsHandler : IToolHandler
    {
        public string Name => "get_build_settings";

        public JObject Execute(JObject parameters)
        {
            var scenes = new JArray();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                scenes.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["enabled"] = scene.enabled,
                    ["guid"] = scene.guid.ToString()
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Build settings retrieved",
                ["activeBuildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["buildTargetGroup"] = EditorUserBuildSettings.selectedBuildTargetGroup.ToString(),
                ["scenes"] = scenes,
                ["development"] = EditorUserBuildSettings.development,
                ["buildAppBundle"] = EditorUserBuildSettings.buildAppBundle,
                ["allowDebugging"] = EditorUserBuildSettings.allowDebugging
            };
        }
    }
}
