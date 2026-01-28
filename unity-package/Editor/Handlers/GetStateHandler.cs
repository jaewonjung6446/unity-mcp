using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetStateHandler : IToolHandler
    {
        public string Name => "get_state";

        public JObject Execute(JObject parameters)
        {
            var scene = EditorSceneManager.GetActiveScene();
            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Editor state retrieved",
                ["state"] = new JObject
                {
                    ["isPlaying"] = EditorApplication.isPlaying,
                    ["isPaused"] = EditorApplication.isPaused,
                    ["isCompiling"] = EditorApplication.isCompiling,
                    ["activeScene"] = scene.name,
                    ["activeScenePath"] = scene.path,
                    ["activeSceneIsDirty"] = scene.isDirty,
                    ["platform"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                    ["unityVersion"] = Application.unityVersion
                }
            };
        }
    }
}
