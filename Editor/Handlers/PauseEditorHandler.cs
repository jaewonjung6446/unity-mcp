using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class PauseEditorHandler : IToolHandler
    {
        public string Name => "pause_editor";

        public JObject Execute(JObject parameters)
        {
            var pause = parameters["pause"]?.ToObject<bool>() ?? !EditorApplication.isPaused;

            EditorApplication.isPaused = pause;

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = pause ? "Editor paused" : "Editor unpaused",
                ["isPaused"] = EditorApplication.isPaused,
                ["isPlaying"] = EditorApplication.isPlaying
            };
        }
    }
}
