using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetLightPropertyHandler : IToolHandler
    {
        public string Name => "set_light_property";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var light = go.GetComponent<Light>();
            if (light == null)
                return McpServer.CreateError($"No Light component found on '{go.name}'", "not_found_error");

            Undo.RecordObject(light, $"Set light properties on {go.name}");
            var changed = new JArray();

            // Light type
            var lightType = parameters["lightType"]?.ToString();
            if (!string.IsNullOrEmpty(lightType))
            {
                if (System.Enum.TryParse<LightType>(lightType, true, out var lt))
                {
                    light.type = lt;
                    changed.Add("lightType");
                }
            }

            // Color
            var color = parameters["color"] as JObject;
            if (color != null)
            {
                light.color = new Color(
                    color["r"]?.ToObject<float>() ?? 1f,
                    color["g"]?.ToObject<float>() ?? 1f,
                    color["b"]?.ToObject<float>() ?? 1f,
                    color["a"]?.ToObject<float>() ?? 1f
                );
                changed.Add("color");
            }

            // Intensity
            var intensity = parameters["intensity"];
            if (intensity != null)
            {
                light.intensity = intensity.ToObject<float>();
                changed.Add("intensity");
            }

            // Range
            var range = parameters["range"];
            if (range != null)
            {
                light.range = range.ToObject<float>();
                changed.Add("range");
            }

            // Spot angle
            var spotAngle = parameters["spotAngle"];
            if (spotAngle != null)
            {
                light.spotAngle = spotAngle.ToObject<float>();
                changed.Add("spotAngle");
            }

            // Shadows
            var shadows = parameters["shadows"]?.ToString();
            if (!string.IsNullOrEmpty(shadows))
            {
                if (System.Enum.TryParse<LightShadows>(shadows, true, out var s))
                {
                    light.shadows = s;
                    changed.Add("shadows");
                }
            }

            // Shadow strength
            var shadowStrength = parameters["shadowStrength"];
            if (shadowStrength != null)
            {
                light.shadowStrength = shadowStrength.ToObject<float>();
                changed.Add("shadowStrength");
            }

            // Color temperature
            var colorTemperature = parameters["colorTemperature"];
            if (colorTemperature != null)
            {
                light.colorTemperature = colorTemperature.ToObject<float>();
                changed.Add("colorTemperature");
            }

            // Enabled
            var enabled = parameters["enabled"];
            if (enabled != null)
            {
                light.enabled = enabled.ToObject<bool>();
                changed.Add("enabled");
            }

            EditorUtility.SetDirty(light);
            EditorSceneManager.MarkSceneDirty(go.scene);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Updated {changed.Count} properties on Light '{go.name}'",
                ["changed"] = changed
            };
        }
    }
}
