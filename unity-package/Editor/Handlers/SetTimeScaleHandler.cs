using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetTimeScaleHandler : IToolHandler
    {
        public string Name => "set_time_scale";

        public JObject Execute(JObject parameters)
        {
            var timeScale = parameters["timeScale"]?.ToObject<float>();
            if (timeScale == null)
                return McpServer.CreateError("Missing required parameter: timeScale", "validation_error");

            var value = Mathf.Clamp(timeScale.Value, 0f, 100f);
            Time.timeScale = value;

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Set Time.timeScale to {value}",
                ["timeScale"] = value,
                ["fixedDeltaTime"] = Time.fixedDeltaTime
            };
        }
    }
}
