using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class SetPlayModeHandler : IToolHandler
    {
        public string Name => "set_play_mode";

        public JObject Execute(JObject parameters)
        {
            var play = parameters["play"]?.ToObject<bool>();
            if (play == null)
                return McpServer.CreateError("Missing required parameter: play (boolean)", "validation_error");

            bool targetState = play.Value;
            if (EditorApplication.isPlaying == targetState)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = targetState
                        ? "Already in Play Mode"
                        : "Already in Edit Mode"
                };
            }

            EditorApplication.isPlaying = targetState;

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = targetState
                    ? "Entering Play Mode. Transition is async — use get_state to confirm."
                    : "Exiting Play Mode. Transition is async — use get_state to confirm."
            };
        }
    }
}
