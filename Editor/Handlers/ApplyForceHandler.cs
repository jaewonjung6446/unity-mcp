using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class ApplyForceHandler : IToolHandler
    {
        public string Name => "apply_force";

        public JObject Execute(JObject parameters)
        {
            if (!EditorApplication.isPlaying)
                return McpServer.CreateError("Apply force requires Play Mode", "invalid_state");

            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var force = parameters["force"] as JObject;
            if (force == null)
                return McpServer.CreateError("Missing required parameter: force {x, y, z}", "validation_error");

            var forceVec = new Vector3(
                force["x"]?.ToObject<float>() ?? 0f,
                force["y"]?.ToObject<float>() ?? 0f,
                force["z"]?.ToObject<float>() ?? 0f
            );

            var modeStr = parameters["mode"]?.ToString() ?? "Force";
            if (!System.Enum.TryParse<ForceMode>(modeStr, true, out var forceMode))
                forceMode = ForceMode.Force;

            var rb = go.GetComponent<Rigidbody>();
            var rb2d = go.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                var atPosition = parameters["atPosition"] as JObject;
                if (atPosition != null)
                {
                    var pos = new Vector3(
                        atPosition["x"]?.ToObject<float>() ?? 0,
                        atPosition["y"]?.ToObject<float>() ?? 0,
                        atPosition["z"]?.ToObject<float>() ?? 0
                    );
                    rb.AddForceAtPosition(forceVec, pos, forceMode);
                }
                else
                {
                    rb.AddForce(forceVec, forceMode);
                }

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Applied force ({forceVec.x:F1}, {forceVec.y:F1}, {forceVec.z:F1}) mode={forceMode} to '{go.name}'"
                };
            }
            else if (rb2d != null)
            {
                var force2d = new Vector2(forceVec.x, forceVec.y);
                var mode2d = modeStr.ToLower() == "impulse" ? ForceMode2D.Impulse : ForceMode2D.Force;
                rb2d.AddForce(force2d, mode2d);

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Applied 2D force ({force2d.x:F1}, {force2d.y:F1}) to '{go.name}'"
                };
            }

            return McpServer.CreateError($"No Rigidbody/Rigidbody2D found on '{go.name}'", "not_found_error");
        }
    }
}
