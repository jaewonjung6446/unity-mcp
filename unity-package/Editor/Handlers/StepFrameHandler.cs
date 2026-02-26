using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class StepFrameHandler : IToolHandler
    {
        public string Name => "step_frame";

        public JObject Execute(JObject parameters)
        {
            if (!EditorApplication.isPlaying)
                return McpServer.CreateError("Step frame requires Play Mode", "invalid_state");

            EditorApplication.isPaused = true;
            EditorApplication.Step();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Stepped one frame",
                ["frameCount"] = UnityEngine.Time.frameCount
            };
        }
    }
}
